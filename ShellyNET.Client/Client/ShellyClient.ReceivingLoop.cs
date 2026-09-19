using ShellyNET.Client.Model;
using ShellyNET.Client.Model.Enums;
using ShellyNET.Client.Parsers;
using System.Net.WebSockets;
using System.Threading.Channels;

namespace ShellyNET.Client;

public partial class ShellyClient
{
    private readonly ShellyNotifyStatusParser _statusParser = new();
    private readonly ShellyNotifyEventParser _eventsParser = new();
    public async ValueTask<ShellyNotifyStatusMessage> GetShellyStatusMessageAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            return await _stateChangesChannel.Reader.ReadAsync(ct).ConfigureAwait(false);
        }

        return default;
    }

    public async ValueTask<ShellyComponentEvent> GetShellyEventAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            return await _shellyEventsChannel.Reader.ReadAsync(ct).ConfigureAwait(false);
        }

        return default;
    }

    private readonly Channel<ShellyComponentEvent> _shellyEventsChannel = Channel.CreateBounded<ShellyComponentEvent>(new BoundedChannelOptions(128)
    {
        AllowSynchronousContinuations = false,
        SingleReader = true,
        SingleWriter = true,
        FullMode = BoundedChannelFullMode.Wait
    });


    private readonly Channel<ShellyNotifyStatusMessage> _stateChangesChannel = Channel.CreateBounded<ShellyNotifyStatusMessage>(new BoundedChannelOptions(128)
    {
        AllowSynchronousContinuations = false,
        SingleReader = true,
        SingleWriter = true,
        FullMode = BoundedChannelFullMode.Wait
    });

    private readonly Channel<ShellyNotifyStatusStringMessage> _stringStateChangesChannel = Channel.CreateBounded<ShellyNotifyStatusStringMessage>(new BoundedChannelOptions(128)
    {
        AllowSynchronousContinuations = false,
        SingleReader = true,
        SingleWriter = true,
        FullMode = BoundedChannelFullMode.Wait
    });

    /// <summary>Receives complete WebSocket messages into one pooled buffer that grows only when required.</summary>
    private async Task ReceiveLoopAsync()
    {
        var webSocket = _webSocket!;
        var outputQueue = _webSocketOutputQueue!;
        var cancellationSource = _connectionCancellationSource!;
        var receiveBuffer = _pool.Rent(InitialReceiveBufferSize);
        ShellyUnexpectedDisconnectionEventArgs? unexpectedDisconnection = null;
        int counter = 0;
        try
        {
            while (!cancellationSource.IsCancellationRequested)
            {
                var messageLength = 0;
                WebSocketMessageType? messageType = null;

                while (true)
                {
                    EnsureSufficientBufferSize(ref receiveBuffer, messageLength);

                    var destination = receiveBuffer.AsMemory(messageLength);
                    var receiveResult = await webSocket.ReceiveAsync(destination, cancellationSource.Token).ConfigureAwait(false);

                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        if (!_disconnectRequested)
                        {
                            unexpectedDisconnection = new ShellyUnexpectedDisconnectionEventArgs(ShellyDisconnectionReason.RemoteClose, webSocket.CloseStatus, webSocket.CloseStatusDescription, _connectionError?.Message);
                        }

                        return;
                    }

                    messageType ??= receiveResult.MessageType;
                    messageLength += receiveResult.Count;
                    if (!receiveResult.EndOfMessage) continue;

                    Console.WriteLine("Incoming Msg: " + Encoding.UTF8.GetString(receiveBuffer.AsSpan(0, messageLength)));
                    Console.WriteLine();

                    if (messageType == WebSocketMessageType.Text) await ProcessTextMessage(receiveBuffer.AsMemory(0, messageLength), cancellationSource.Token).ConfigureAwait(false);
                    else RaiseUnsupportedMessageTypeReceived(messageType.Value, messageLength);

                    break;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationSource.IsCancellationRequested)
        {
            var connectionError = _connectionError;
            if (!_disconnectRequested && connectionError is not null)
            {
                var reason = IsTransportException(connectionError) ? ShellyDisconnectionReason.TransportError : ShellyDisconnectionReason.ClientError;
                unexpectedDisconnection = new ShellyUnexpectedDisconnectionEventArgs(reason, webSocket.CloseStatus, webSocket.CloseStatusDescription, connectionError.Message);
            }
        }
        catch (Exception exception)
        {
            Interlocked.CompareExchange(ref _connectionError, exception, null);
            if (!_disconnectRequested)
            {
                var reason = IsTransportException(exception) ? ShellyDisconnectionReason.TransportError : ShellyDisconnectionReason.ClientError;
                unexpectedDisconnection = new ShellyUnexpectedDisconnectionEventArgs(reason, webSocket.CloseStatus, webSocket.CloseStatusDescription, exception.Message);
            }
        }
        finally
        {
            outputQueue.Writer.TryComplete();
            CancelSafely(cancellationSource);

            var sendTask = _sendTask;
            if (sendTask is not null)
            {
                try
                {
                    await sendTask.ConfigureAwait(false);
                }
                catch
                {
                }
            }

            CompletePendingRequestsAsConnectionLost();
            Volatile.Write(ref _deviceInfo, null);
            Interlocked.Exchange(ref _authenticationState, null);
            _pool.Return(receiveBuffer);
            webSocket.Dispose();
            cancellationSource.Dispose();

            _webSocketOutputQueue = null;
            _connectionCancellationSource = null;
            _connectedEndpoint = null;
            _sendTask = null;
            _receiveTask = null;
            _webSocket = null;

            if (unexpectedDisconnection is not null) RaiseDisconnectedUnexpectedly(unexpectedDisconnection);
        }
    }


    /// <summary>Routes a complete text frame by response id or Shelly notification method without creating a JSON document.</summary>
    internal async ValueTask ProcessTextMessage(Memory<byte> message, CancellationToken ct)
    {
        if (!TryReadFrameHeader(message, out var responseId, out var notificationType))
        {
            RaiseUnknownMessageReceived(message, "Invalid or unsupported JSON-RPC frame.");
            return;
        }

        if (responseId is null)
        {
            switch (notificationType)
            {
                case ShellyNotificationType.NotifyStatus:
                case ShellyNotificationType.NotifyFullStatus:
                    {
                        using var result = _statusParser.ProcessNotifyStatusMessage(message, StatesContainingObject.Params, ct);

                        for (var i = 0; i < result.MessageCount; i++)
                            await _stateChangesChannel.Writer.WriteAsync(result.MessageSpan[i], ct).ConfigureAwait(false);

                        for (var i = 0; i < result.StringMessageCount; i++)
                            await _stringStateChangesChannel.Writer.WriteAsync(result.StringMessageSpan[i], ct).ConfigureAwait(false);
                        return;
                    }
                case ShellyNotificationType.NotifyEvent:
                    {
                        using var results = _eventsParser.ProcessNotifyEventMessage(message, ct);
                        for (var i = 0; i < results.EventsCount; i++)
                        {
                            var shellyEvent = results.EventsSpan[i];
                            await _shellyEventsChannel.Writer.WriteAsync(shellyEvent, ct).ConfigureAwait(false);
                        }
                        return;
                    }
                default:
                    RaiseUnknownMessageReceived(message, "Unsupported Shelly notification method.");
                    return;
            }
        }

        if (notificationType != ShellyNotificationType.None)
        {
            RaiseUnknownMessageReceived(message, "Device-initiated RPC calls are not supported by this client.");
            return;
        }

        if (TryCompletePendingRequest(responseId.Value, message)) return;

        RaiseUnknownMessageReceived(message, $"Response id {responseId.Value} does not match an active request.");
    }


    /// <summary>Raises the unsupported-message notification while leaving the receive loop active.</summary>
    private void RaiseUnsupportedMessageTypeReceived(WebSocketMessageType messageType, int messageLength)
    {
        var eventArgs = new ShellyUnsupportedMessageTypeEventArgs(messageType, messageLength);

        try
        {
            UnsupportedMessageTypeReceived?.Invoke(this, eventArgs);
        }
        catch
        {
        }
    }

    /// <summary>Ensures ReceiveAsync always has writable pooled memory while retaining an enlarged buffer for later messages.</summary>
    private void EnsureSufficientBufferSize(ref byte[] receiveBuffer, int messageLength)
    {
        if (messageLength == 0 && receiveBuffer.Length != InitialReceiveBufferSize)
        {
            _pool.Return(receiveBuffer);
            receiveBuffer = _pool.Rent(InitialReceiveBufferSize);
            return;
        }

        if (messageLength < receiveBuffer.Length) return;

        var expandedBuffer = _pool.Rent(checked(receiveBuffer.Length * 2));
        receiveBuffer.AsSpan(0, messageLength).CopyTo(expandedBuffer);
        _pool.Return(receiveBuffer);
        receiveBuffer = expandedBuffer;
    }


    /// <summary>Completes every operation owned by a disappearing connection so callers do not remain blocked after transport cleanup.</summary>
    private void CompletePendingRequestsAsConnectionLost()
    {
        foreach (var requestEntry in _pendingRequests)
        {
            if (!_pendingRequests.TryRemove(requestEntry)) continue;
            requestEntry.Value.CompleteFailure(ShellyRpcResultStatus.ConnectionLost, null);
        }
    }
}
