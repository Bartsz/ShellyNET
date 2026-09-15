using ShellyNET.Client.Model;
using ShellyNET.Client.Model.Messages;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Threading.Channels;

namespace ShellyNET.Client;


/// <summary>Provides a persistent JSON-RPC connection to one Shelly Gen2+ device.</summary>
public partial class ShellyClient
{
    private const int WebSocketPort = 80;
    private const int SecureWebSocketPort = 443;
    private const int InitialReceiveBufferSize = 256;
    private const int RequestIdStartIndex = 6;
    private const int RequestIdFieldLength = 5;
    private const int AuthenticationResponseFieldLength = 64;
    private const int ModernNonceCountFieldLength = 8;
    private const string RpcPath = "rpc";
    private const string AuthenticationUser = "admin";
    private const string Sha256Algorithm = "SHA-256";
    private const string WebSocketHa2 = "6370ec69915103833b5222b368555393393f098bfbfbb59f47e0590af135f062";
    private static readonly TimeSpan DefaultRpcTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan WebSocketKeepAliveInterval = TimeSpan.FromSeconds(55);
    private static readonly TimeSpan WebSocketKeepAliveTimeout = TimeSpan.FromSeconds(10);
    private static readonly byte[] GetDeviceInfoRequestTemplate = """{"id":     ,"src":"_","method":"Shelly.GetDeviceInfo"}"""u8.ToArray();
    private static readonly byte[] GetConfigRequestTemplate = """{"id":0    ,"src":"_","method":"Shelly.GetConfig"}"""u8.ToArray();
    private static readonly byte[] GetStatusRequestTemplate = """{"id":     ,"src":"_","method":"Shelly.GetStatus"}"""u8.ToArray();

    private readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;
    private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);
    private readonly SemaphoreSlim _authenticatedSendSemaphore = new(1, 1);
    private readonly ConcurrentDictionary<ushort, PendingRequest> _pendingRequests = new();

    private ClientWebSocket? _webSocket;
    private Uri? _connectedEndpoint;
    private Channel<OutgoingMessage>? _webSocketOutputQueue;
    private CancellationTokenSource? _connectionCancellationSource;
    private Task? _receiveTask;
    private Task? _sendTask;
    private ShellyDeviceInfo? _deviceInfo;
    private ShellyAuthenticationTemplate? _authenticationTemplate;
    private Exception? _connectionError;
    private volatile bool _disconnectRequested;
    private int _nextRequestId;

    /// <summary>Occurs when an established connection ends without an explicit DisconnectAsync request.</summary>
    public event EventHandler<ShellyUnexpectedDisconnectionEventArgs>? DisconnectedUnexpectedly;

    /// <summary>Occurs when Shelly sends a WebSocket message type that this JSON-RPC client does not consume.</summary>
    public event EventHandler<ShellyUnsupportedMessageTypeEventArgs>? UnsupportedMessageTypeReceived;

    /// <summary>Occurs when a complete Shelly text frame cannot be matched to a supported response or notification.</summary>
    public event EventHandler<ShellyUnknownMessageEventArgs>? UnknownMessageReceived;

    /// <summary>Gets whether transport initialization completed and the client has an open WebSocket connection.</summary>
    public bool IsConnected => !_disconnectRequested && Volatile.Read(ref _deviceInfo) is not null && _webSocket?.State == WebSocketState.Open;

    /// <summary>Gets identity and firmware information loaded during connection initialization.</summary>
    public ShellyDeviceInfo? DeviceInfo => Volatile.Read(ref _deviceInfo);

    /// <summary>Gets the endpoint of the active connection.</summary>
    public Uri? ConnectedEndpoint => IsConnected ? _connectedEndpoint : null;

    /// <summary>Connects to the Shelly device, starts transport loops, and loads the device information required by later RPC calls.</summary>
    public async Task<ConnectionResult> ConnectAsync(IPAddress address, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(address);

        try
        {
            await _connectionSemaphore.WaitAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return ConnectionResult.Canceled;
        }

        try
        {
            if (IsConnected) return ConnectionResult.AlreadyConnected;
            if (_webSocket is not null) await AbortConnectionAsync().ConfigureAwait(false);

            var endpoint = CreateRpcEndpoint("ws", address, WebSocketPort);
            var webSocket = await TryConnectWebSocketAsync(endpoint, acceptUntrustedServerCertificate: false, ct).ConfigureAwait(false);

            if (webSocket is null && !ct.IsCancellationRequested)
            {
                endpoint = CreateRpcEndpoint("wss", address, SecureWebSocketPort);
                webSocket = await TryConnectWebSocketAsync(endpoint, acceptUntrustedServerCertificate: true, ct).ConfigureAwait(false);
            }

            if (webSocket is null) return ct.IsCancellationRequested ? ConnectionResult.Canceled : ConnectionResult.ConnectionFailed;

            StartConnection(webSocket, endpoint);

            var deviceInfoResult = await GetDeviceInfoAsync(ct).ConfigureAwait(false);
            if (deviceInfoResult.IsSuccess && deviceInfoResult.Value is not null) return ConnectionResult.Connected;

            await AbortConnectionAsync().ConfigureAwait(false);
            return deviceInfoResult.Status == ShellyRpcResultStatus.Canceled ? ConnectionResult.Canceled : ConnectionResult.InitializationFailed;
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <summary>Closes the active connection without reporting the requested shutdown as an unexpected disconnection.</summary>
    public async Task<DisconnectionResult> DisconnectAsync(CancellationToken ct = default)
    {
        try
        {
            await _connectionSemaphore.WaitAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return DisconnectionResult.Canceled;
        }

        try
        {
            var webSocket = _webSocket;
            if (webSocket is null) return DisconnectionResult.NotConnected;

            _disconnectRequested = true;
            _webSocketOutputQueue?.Writer.TryComplete();

            try
            {
                var sendTask = _sendTask;
                if (sendTask is not null) await sendTask.WaitAsync(ct).ConfigureAwait(false);

                if (_connectionError is { } sendError)
                {
                    await AbortConnectionAsync().ConfigureAwait(false);
                    return IsTransportException(sendError) ? DisconnectionResult.TransportError : DisconnectionResult.ClientError;
                }

                if (webSocket.State == WebSocketState.Open)
                {
                    await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, ct).ConfigureAwait(false);
                }

                var receiveTask = _receiveTask;
                if (receiveTask is not null) await receiveTask.WaitAsync(ct).ConfigureAwait(false);

                var connectionError = _connectionError;
                if (connectionError is null) return DisconnectionResult.Disconnected;
                return IsTransportException(connectionError) ? DisconnectionResult.TransportError : DisconnectionResult.ClientError;
            }
            catch (OperationCanceledException)
            {
                await AbortConnectionAsync().ConfigureAwait(false);
                return DisconnectionResult.Canceled;
            }
            catch (Exception exception)
            {
                Interlocked.CompareExchange(ref _connectionError, exception, null);
                await AbortConnectionAsync().ConfigureAwait(false);
                return IsTransportException(exception) ? DisconnectionResult.TransportError : DisconnectionResult.ClientError;
            }
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <summary>Requests Shelly.GetDeviceInfo without authentication and refreshes the cached device information.</summary>
    public async Task<ShellyRpcResult<ShellyDeviceInfo>> GetDeviceInfoAsync(CancellationToken ct = default)
    {
        var result = await SendTemplateMessageRpcCoreAsync<ShellyDeviceInfo>(GetDeviceInfoRequestTemplate, null, static (_, resultPayload) => JsonSerializer.Deserialize(resultPayload, ShellyJsonSerializerContext.Default.ShellyDeviceInfo), ct).ConfigureAwait(false);
        if (result.Status != ShellyRpcResultStatus.Success || result.Value is null)
        {
            if (result.Status != ShellyRpcResultStatus.Success) return result;
            return new ShellyRpcResult<ShellyDeviceInfo>(ShellyRpcResultStatus.ClientError, null, null, "Shelly.GetDeviceInfo returned an empty result.");
        }

        Volatile.Write(ref _deviceInfo, result.Value);
        if (!result.Value.AuthenticationEnabled) Interlocked.Exchange(ref _authenticationTemplate, null);
        return result;
    }

    /// <summary>Requests Shelly.GetConfig and parses the owned complete JSON-RPC response into component configuration models.</summary>
    public async Task<ShellyRpcResult<List<ShellyComponentConfigBase>>> GetDeviceConfigAsync(CancellationToken ct = default)
    {
        var response = await SendTemplateMessageRpcAsync<byte[]>(GetConfigRequestTemplate, static (message, _) => message.ToArray(), ct).ConfigureAwait(false);
        if (!response.IsSuccess) return new ShellyRpcResult<List<ShellyComponentConfigBase>>(response.Status, null, response.RpcError, response.ErrorMessage);

        try
        {
            var config = _configParser.ParseConfig(response.Value!);
            return new ShellyRpcResult<List<ShellyComponentConfigBase>>(ShellyRpcResultStatus.Success, config, null, null);
        }
        catch (JsonException exception)
        {
            return new ShellyRpcResult<List<ShellyComponentConfigBase>>(ShellyRpcResultStatus.ClientError, null, null, exception.Message);
        }
    }

    /// <summary>Requests Shelly.GetConfig and parses the owned complete JSON-RPC response into component configuration models.</summary>
    public async Task<ShellyRpcResult<string>> GetDeviceStatus(CancellationToken ct = default)
    {
        var response = await SendTemplateMessageRpcAsync<byte[]>(GetStatusRequestTemplate, static (message, _) => message.ToArray(), ct).ConfigureAwait(false);
        if (!response.IsSuccess) return new ShellyRpcResult<string>(response.Status, null, response.RpcError, response.ErrorMessage);
        return new ShellyRpcResult<string>(ShellyRpcResultStatus.ClientError, null, null, "");
    }

    /// <summary>Validates credentials against a protected RPC and publishes an authentication template for later protected calls.</summary>
    public async Task<ShellyLoginResult> LoginToDeviceAsync(string password, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(password);

        var deviceInfo = DeviceInfo;
        if (!IsConnected || deviceInfo is null) return new ShellyLoginResult(ShellyLoginStatus.NotConnected, null, null);

        if (!deviceInfo.AuthenticationEnabled)
        {
            Interlocked.Exchange(ref _authenticationTemplate, null);
            return new ShellyLoginResult(ShellyLoginStatus.AuthenticationNotRequired, null, null);
        }

        Interlocked.Exchange(ref _authenticationTemplate, null);

        var challengeResult = await SendTemplateMessageRpcCoreAsync<IgnoredRpcResult>(GetStatusRequestTemplate, null, null, cancellationToken).ConfigureAwait(false);
        var challengeFailure = CreateLoginTransportFailure(challengeResult);
        if (challengeFailure is { } failure) return failure;

        if (challengeResult.Status == ShellyRpcResultStatus.Success) return new ShellyLoginResult(ShellyLoginStatus.AuthenticationNotRequired, null, null);

        var challengeError = challengeResult.RpcError;
        if (challengeError is not { Code: 401 })
        {
            return new ShellyLoginResult(ShellyLoginStatus.RpcError, challengeError, challengeError is null ? "Protected RPC returned neither a result nor an authentication challenge." : null);
        }

        var authenticationTemplateResult = TryCreateAuthenticationTemplate(deviceInfo, password, challengeError.Message);
        if (authenticationTemplateResult.Status != ShellyLoginStatus.Success)
        {
            return new ShellyLoginResult(authenticationTemplateResult.Status, challengeError, authenticationTemplateResult.ErrorMessage);
        }

        var authenticationTemplate = authenticationTemplateResult.AuthenticationTemplate!;
        var authenticatedResult = await SendTemplateMessageRpcCoreAsync<IgnoredRpcResult>(GetStatusRequestTemplate, authenticationTemplate, null, cancellationToken).ConfigureAwait(false);
        var authenticatedFailure = CreateLoginTransportFailure(authenticatedResult);
        if (authenticatedFailure is { } authenticatedFailureResult) return authenticatedFailureResult;

        if (authenticatedResult.RpcError is { Code: 401 } invalidCredentialsError)
        {
            return new ShellyLoginResult(ShellyLoginStatus.InvalidCredentials, invalidCredentialsError, null);
        }

        if (authenticatedResult.RpcError is not null) return new ShellyLoginResult(ShellyLoginStatus.RpcError, authenticatedResult.RpcError, null);

        Interlocked.Exchange(ref _authenticationTemplate, authenticationTemplate);
        return new ShellyLoginResult(ShellyLoginStatus.Success, null, null);
    }

    /// <summary>Publishes a newly established transport only after all connection-owned resources are ready.</summary>
    private void StartConnection(ClientWebSocket webSocket, Uri endpoint)
    {
        _connectedEndpoint = endpoint;
        _webSocketOutputQueue = Channel.CreateBounded<OutgoingMessage>(new BoundedChannelOptions(64)
        {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });
        _connectionCancellationSource = new CancellationTokenSource();
        _connectionError = null;
        _disconnectRequested = false;
        Volatile.Write(ref _deviceInfo, null);
        Interlocked.Exchange(ref _authenticationTemplate, null);
        _webSocket = webSocket;
        _sendTask = SendLoopAsync();
        _sendTask.ContinueWith((x) => Console.WriteLine("Send task closed"));
        _receiveTask = ReceiveLoopAsync();
        _receiveTask.ContinueWith((x) => Console.WriteLine("Recieve task closed"));
    }

    /// <summary>Receives complete WebSocket messages into one pooled buffer that grows only when required.</summary>
    private async Task ReceiveLoopAsync()
    {
        var webSocket = _webSocket!;
        var outputQueue = _webSocketOutputQueue!;
        var cancellationSource = _connectionCancellationSource!;
        var receiveBuffer = _pool.Rent(InitialReceiveBufferSize);
        ShellyUnexpectedDisconnectionEventArgs? unexpectedDisconnection = null;

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
            Console.WriteLine("Disconnected");
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
            Interlocked.Exchange(ref _authenticationTemplate, null);
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

    /// <summary>Serializes all WebSocket sends through the outgoing channel and returns every pooled buffer exactly once.</summary>
    private async Task SendLoopAsync()
    {
        var webSocket = _webSocket!;
        var outputQueue = _webSocketOutputQueue!;
        var cancellationSource = _connectionCancellationSource!;
        var reader = outputQueue.Reader;

        try
        {
            while (await reader.WaitToReadAsync(cancellationSource.Token).ConfigureAwait(false))
            {
                while (reader.TryRead(out var message))
                {
                    Console.WriteLine("Outgoing Msg: " + Encoding.UTF8.GetString(message.Buffer.AsSpan(0, message.Length)));
                    Console.WriteLine();
                    try
                    {
                        await webSocket.SendAsync(message.Buffer.AsMemory(0, message.Length), WebSocketMessageType.Text, true, cancellationSource.Token).ConfigureAwait(false);
                    }
                    finally
                    {
                        _pool.Return(message.Buffer);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationSource.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Interlocked.CompareExchange(ref _connectionError, exception, null);
            outputQueue.Writer.TryComplete();
            webSocket.Abort();
            CancelSafely(cancellationSource);
        }
        finally
        {
            outputQueue.Writer.TryComplete();
            while (reader.TryRead(out var message)) _pool.Return(message.Buffer);
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
                    ProcessNotifyStatusMessage(message, ct);
                    for (int i = 0; i < _pendingMessagesCount; i++)
                    {
                        await _stateChangesChannel.Writer.WriteAsync(_pendingStateUpdates[i], ct).ConfigureAwait(false);
                    }
                    _pendingMessagesCount = 0;
                    return;
                case ShellyNotificationType.NotifyEvent:
                    ProcessNotifyEventMessage(message, ct);
                    for (int i = 0; i < _pendingEventsCount; i++)
                    {
                        await _shellyEventsChannel.Writer.WriteAsync(_pendingEventUpdates[i], ct).ConfigureAwait(false);
                    }
                    _pendingEventsCount = 0;
                    return;

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

        if (TryCompletePendingRequest(responseId.Value, message.Span)) return;

        RaiseUnknownMessageReceived(message, $"Response id {responseId.Value} does not match an active request.");
    }


    /// <summary>Sends one template RPC with the active authentication state and retries once when firmware 2.0+ reports a stale nonce.</summary>
    private async Task<ShellyRpcResult<T>> SendTemplateMessageRpcAsync<T>(byte[] template, RpcResultParser<T>? resultParser, CancellationToken ct)
    {
        var authenticationTemplate = Volatile.Read(ref _authenticationTemplate);
        var result = await SendTemplateMessageRpcCoreAsync(template, authenticationTemplate, resultParser, ct).ConfigureAwait(false);

        if (authenticationTemplate is null || !authenticationTemplate.UsesReusableNonce) return result;
        if (result.RpcError is not { Code: 401 } rpcError) return result;

        if (!TryReadAuthenticationChallenge(rpcError.Message, out var challenge, out var challengeError)) return result;
        if (challenge!.Stale != true) return result;

        if (!TryRefreshAuthenticationTemplate(authenticationTemplate, challenge, out var refreshedTemplate, out var refreshError))
        {
            var errorMessage = refreshError ?? challengeError ?? "Shelly returned an invalid stale authentication challenge.";
            return new ShellyRpcResult<T>(ShellyRpcResultStatus.ClientError, default, null, errorMessage);
        }

        Interlocked.CompareExchange(ref _authenticationTemplate, refreshedTemplate, authenticationTemplate);
        var currentTemplate = Volatile.Read(ref _authenticationTemplate);
        if (currentTemplate is null) return new ShellyRpcResult<T>(ShellyRpcResultStatus.ConnectionLost, default, null, null);

        return await SendTemplateMessageRpcCoreAsync(template, currentTemplate, resultParser, ct).ConfigureAwait(false);
    }

    /// <summary>Sends one fixed-layout RPC frame with an explicitly selected authentication template.</summary>
    private async Task<ShellyRpcResult<T>> SendTemplateMessageRpcCoreAsync<T>(byte[] template, ShellyAuthenticationTemplate? authenticationTemplate, RpcResultParser<T>? resultParser, CancellationToken ct)
    {
        var outputWriter = GetOutputWriter();
        if (outputWriter is null) return new ShellyRpcResult<T>(ShellyRpcResultStatus.NotConnected, default, null, null);

        var pendingRequest = new PendingRequest<T>(resultParser);
        if (!TryReservePendingRequest(pendingRequest, out var requestId))
        {
            return new ShellyRpcResult<T>(ShellyRpcResultStatus.ClientError, default, null, "All 65536 numeric request ids are currently in use.");
        }

        var authenticatedSendLockTaken = false;
        if (authenticationTemplate?.UsesReusableNonce == true)
        {
            try
            {
                await _authenticatedSendSemaphore.WaitAsync(ct).ConfigureAwait(false);
                authenticatedSendLockTaken = true;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                TryRemovePendingRequest(requestId, pendingRequest);
                return new ShellyRpcResult<T>(ShellyRpcResultStatus.Canceled, default, null, null);
            }
        }

        EnqueueStatus enqueueStatus;
        try
        {
            var authenticationLength = authenticationTemplate?.Payload.Length ?? 0;
            var outgoingLength = authenticationTemplate is null ? template.Length : checked(template.Length - 1 + authenticationLength);
            var outgoingBuffer = _pool.Rent(outgoingLength);

            if (!TryWriteOutgoingMessage(template, authenticationTemplate, requestId, outgoingBuffer, out var writeError))
            {
                _pool.Return(outgoingBuffer);
                TryRemovePendingRequest(requestId, pendingRequest);
                return new ShellyRpcResult<T>(ShellyRpcResultStatus.ClientError, default, null, writeError);
            }

            enqueueStatus = await EnqueueOutgoingMessageAsync(outputWriter, outgoingBuffer, outgoingLength, ct).ConfigureAwait(false);
        }
        finally
        {
            if (authenticatedSendLockTaken) _authenticatedSendSemaphore.Release();
        }

        if (enqueueStatus != EnqueueStatus.Queued)
        {
            TryRemovePendingRequest(requestId, pendingRequest);

            var status = enqueueStatus == EnqueueStatus.Canceled
                ? ShellyRpcResultStatus.Canceled
                : ShellyRpcResultStatus.ConnectionLost;

            return new ShellyRpcResult<T>(status, default, null, null);
        }

        return await WaitForPendingResponseAsync(requestId, pendingRequest, ct).ConfigureAwait(false);
    }

    /// <summary>Copies a request template into its owned send buffer and patches request-specific authentication values.</summary>
    private static bool TryWriteOutgoingMessage(byte[] messageTemplate, ShellyAuthenticationTemplate? authenticationTemplate, ushort requestId, byte[] destination, out string? errorMessage)
    {
        errorMessage = null;

        if (authenticationTemplate is not null && (messageTemplate.Length == 0 || messageTemplate[^1] != (byte)'}'))
        {
            errorMessage = "Authenticated RPC templates must end with a JSON object brace.";
            return false;
        }

        if (authenticationTemplate is null)
        {
            messageTemplate.CopyTo(destination, 0);
        }
        else
        {
            var messageWithoutClosingBrace = messageTemplate.AsSpan(0, messageTemplate.Length - 1);
            messageWithoutClosingBrace.CopyTo(destination);
            authenticationTemplate.Payload.CopyTo(destination, messageWithoutClosingBrace.Length);
        }

        var requestIdDestination = destination.AsSpan(RequestIdStartIndex, RequestIdFieldLength);
        if (!requestId.TryFormat(requestIdDestination, out _))
        {
            errorMessage = "The request id did not fit the reserved JSON field.";
            return false;
        }

        if (authenticationTemplate is null) return true;

        if (!authenticationTemplate.UsesReusableNonce) return true;

        var authenticationOffset = messageTemplate.Length - 1;
        var authenticationValues = authenticationTemplate.CreateRequestValues();
        var nonceCountDestination = destination.AsSpan(authenticationOffset + authenticationTemplate.NonceCountOffset, ModernNonceCountFieldLength);
        Encoding.ASCII.GetBytes(authenticationValues.NonceCount.AsSpan(), nonceCountDestination);

        var responseDestination = destination.AsSpan(authenticationOffset + authenticationTemplate.ResponseOffset, AuthenticationResponseFieldLength);
        if (Encoding.ASCII.GetBytes(authenticationValues.Response.AsSpan(), responseDestination) != AuthenticationResponseFieldLength)
        {
            errorMessage = "The authentication response did not fit the reserved SHA-256 field.";
            return false;
        }

        return true;
    }

    /// <summary>Waits at most ten seconds for one correlated response and converts timeout or cancellation into explicit result states.</summary>
    private async Task<ShellyRpcResult<T>> WaitForPendingResponseAsync<T>(ushort requestId, PendingRequest<T> pendingRequest, CancellationToken cancellationToken)
    {
        try
        {
            return await pendingRequest.Task.WaitAsync(DefaultRpcTimeout, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            if (TryRemovePendingRequest(requestId, pendingRequest))
            {
                return new ShellyRpcResult<T>(ShellyRpcResultStatus.TimedOut, default, null, null);
            }

            return await pendingRequest.Task.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (TryRemovePendingRequest(requestId, pendingRequest))
            {
                return new ShellyRpcResult<T>(ShellyRpcResultStatus.Canceled, default, null, null);
            }

            return await pendingRequest.Task.ConfigureAwait(false);
        }
    }


    /// <summary>Queues a pooled WebSocket payload while preserving a no-allocation fast path when capacity is available.</summary>
    private ValueTask<EnqueueStatus> EnqueueOutgoingMessageAsync(ChannelWriter<OutgoingMessage> outputWriter, byte[] buffer, int length, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            _pool.Return(buffer);
            return ValueTask.FromResult(EnqueueStatus.Canceled);
        }

        var message = new OutgoingMessage(buffer, length);
        if (outputWriter.TryWrite(message)) return ValueTask.FromResult(EnqueueStatus.Queued);

        return EnqueueSlowAsync(outputWriter, message, ct);
    }

    /// <summary>Waits for output capacity only on the uncommon backpressure path and releases buffer ownership on failure.</summary>
    private async ValueTask<EnqueueStatus> EnqueueSlowAsync(ChannelWriter<OutgoingMessage> outputWriter, OutgoingMessage message, CancellationToken ct)
    {
        try
        {
            await outputWriter.WriteAsync(message, ct).ConfigureAwait(false);
            return EnqueueStatus.Queued;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _pool.Return(message.Buffer);
            return EnqueueStatus.Canceled;
        }
        catch (ChannelClosedException)
        {
            _pool.Return(message.Buffer);
            return EnqueueStatus.ConnectionLost;
        }
    }

    /// <summary>Reserves an unused request id for the specified pending RPC operation.</summary>
    private bool TryReservePendingRequest(PendingRequest pendingRequest, out ushort requestId)
    {
        for (var attempt = 0; attempt <= ushort.MaxValue; attempt++)
        {
            requestId = unchecked((ushort)Interlocked.Increment(ref _nextRequestId));

            if (_pendingRequests.TryAdd(requestId, pendingRequest)) return true;
        }

        requestId = default;
        return false;
    }

    /// <summary>Completes one pending request while the reusable receive buffer still contains the full response.</summary>
    private bool TryCompletePendingRequest(ushort requestId, ReadOnlySpan<byte> message)
    {
        if (!_pendingRequests.TryGetValue(requestId, out var pendingRequest)) return false;

        var requestEntry = new KeyValuePair<ushort, PendingRequest>(requestId, pendingRequest);
        if (!_pendingRequests.TryRemove(requestEntry)) return true;

        pendingRequest.CompleteResponse(message);
        return true;
    }

    /// <summary>Removes a pending request only when the id still belongs to the same request instance.</summary>
    private bool TryRemovePendingRequest(ushort requestId, PendingRequest pendingRequest)
    {
        var item = new KeyValuePair<ushort, PendingRequest>(requestId, pendingRequest);
        return ((ICollection<KeyValuePair<ushort, PendingRequest>>)_pendingRequests).Remove(item);
    }

    /// <summary>Completes every request owned by the disappearing connection with an explicit connection-lost result.</summary>
    private void CompletePendingRequestsAsConnectionLost()
    {
        foreach (var requestEntry in _pendingRequests)
        {
            if (!_pendingRequests.TryRemove(requestEntry)) continue;
            requestEntry.Value.CompleteFailure(ShellyRpcResultStatus.ConnectionLost, null);
        }
    }

    /// <summary>Validates one JSON-RPC response and materializes its successful result while the receive buffer is still valid.</summary>
    private static ShellyRpcResult<T> ParseRpcResponse<T>(ReadOnlySpan<byte> message, RpcResultParser<T>? resultParser)
    {
        var reader = new Utf8JsonReader(message);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            return new ShellyRpcResult<T>(ShellyRpcResultStatus.ClientError, default, null, "Shelly returned an invalid JSON-RPC response object.");
        }

        var hasResult = false;
        var hasError = false;
        var reachedEndObject = false;
        var resultOffset = 0;
        var resultLength = 0;
        ShellyRpcError? rpcError = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                reachedEndObject = true;
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName || reader.CurrentDepth != 1)
            {
                return new ShellyRpcResult<T>(ShellyRpcResultStatus.ClientError, default, null, "Shelly returned an invalid JSON-RPC response shape.");
            }

            if (reader.ValueTextEquals("result"u8))
            {
                if (hasResult) return new ShellyRpcResult<T>(ShellyRpcResultStatus.ClientError, default, null, "Shelly returned more than one result field.");
                if (!reader.Read()) return new ShellyRpcResult<T>(ShellyRpcResultStatus.ClientError, default, null, "Shelly returned an incomplete result field.");

                hasResult = true;
                resultOffset = checked((int)reader.TokenStartIndex);
                if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray) reader.Skip();
                resultLength = checked((int)reader.BytesConsumed) - resultOffset;
                continue;
            }

            if (reader.ValueTextEquals("error"u8))
            {
                if (hasError) return new ShellyRpcResult<T>(ShellyRpcResultStatus.ClientError, default, null, "Shelly returned more than one error field.");
                if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return new ShellyRpcResult<T>(ShellyRpcResultStatus.ClientError, default, null, "Shelly returned an invalid RPC error object.");

                hasError = true;
                if (!TryReadRpcError(ref reader, out rpcError, out var errorMessage))
                {
                    return new ShellyRpcResult<T>(ShellyRpcResultStatus.ClientError, default, null, errorMessage);
                }

                continue;
            }

            if (!reader.Read()) return new ShellyRpcResult<T>(ShellyRpcResultStatus.ClientError, default, null, "Shelly returned an incomplete JSON-RPC response.");
            if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray) reader.Skip();
        }

        if (!reachedEndObject || reader.Read()) return new ShellyRpcResult<T>(ShellyRpcResultStatus.ClientError, default, null, "Shelly returned malformed JSON after the response object.");
        if (hasResult && hasError) return new ShellyRpcResult<T>(ShellyRpcResultStatus.ClientError, default, null, "Shelly returned both result and error fields.");
        if (hasError) return rpcError is null
            ? new ShellyRpcResult<T>(ShellyRpcResultStatus.ClientError, default, null, "Shelly returned an empty RPC error.")
            : new ShellyRpcResult<T>(ShellyRpcResultStatus.RpcError, default, rpcError, null);
        if (!hasResult) return new ShellyRpcResult<T>(ShellyRpcResultStatus.ClientError, default, null, "Shelly returned neither a result nor an error.");
        if (resultParser is null) return new ShellyRpcResult<T>(ShellyRpcResultStatus.Success, default, null, null);

        var resultPayload = message.Slice(resultOffset, resultLength);
        var value = resultParser(message, resultPayload);
        return new ShellyRpcResult<T>(ShellyRpcResultStatus.Success, value, null, null);
    }

    /// <summary>Reads the stable JSON-RPC error fields while ignoring optional protocol-specific error data.</summary>
    private static bool TryReadRpcError(ref Utf8JsonReader reader, out ShellyRpcError? rpcError, out string? errorMessage)
    {
        rpcError = null;
        errorMessage = null;
        int? code = null;
        var message = string.Empty;
        var reachedEndObject = false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                reachedEndObject = true;
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName || reader.CurrentDepth != 2)
            {
                errorMessage = "Shelly returned an invalid RPC error shape.";
                return false;
            }

            if (reader.ValueTextEquals("code"u8))
            {
                if (!reader.Read() || reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out var parsedCode))
                {
                    errorMessage = "Shelly returned an invalid RPC error code.";
                    return false;
                }

                code = parsedCode;
                continue;
            }

            if (reader.ValueTextEquals("message"u8))
            {
                if (!reader.Read() || reader.TokenType != JsonTokenType.String)
                {
                    errorMessage = "Shelly returned an invalid RPC error message.";
                    return false;
                }

                message = reader.GetString() ?? string.Empty;
                continue;
            }

            if (!reader.Read())
            {
                errorMessage = "Shelly returned an incomplete RPC error object.";
                return false;
            }

            if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray) reader.Skip();
        }

        if (!reachedEndObject || code is null)
        {
            errorMessage = "Shelly returned an incomplete RPC error object.";
            return false;
        }

        rpcError = new ShellyRpcError { Code = code.Value, Message = message };
        return true;
    }

    /// <summary>Reads only top-level routing fields and skips payload objects that do not affect classification.</summary>
    private static bool TryReadFrameHeader(Memory<byte> message, out ushort? responseId, out ShellyNotificationType notificationType)
    {
        responseId = null;
        notificationType = ShellyNotificationType.None;

        try
        {
            var reader = new Utf8JsonReader(message.Span);
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject) break;
                if (reader.TokenType != JsonTokenType.PropertyName || reader.CurrentDepth != 1) return false;

                if (reader.ValueTextEquals("id"u8))
                {
                    if (!reader.Read() || reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out var numericId)) return false;
                    if ((uint)numericId > ushort.MaxValue) return false;

                    responseId = (ushort)numericId;
                    continue;
                }

                if (reader.ValueTextEquals("method"u8))
                {
                    if (!reader.Read() || reader.TokenType != JsonTokenType.String) return false;

                    if (reader.ValueTextEquals("NotifyStatus"u8)) notificationType = ShellyNotificationType.NotifyStatus;
                    else if (reader.ValueTextEquals("NotifyEvent"u8)) notificationType = ShellyNotificationType.NotifyEvent;
                    else if (reader.ValueTextEquals("NotifyFullStatus"u8)) notificationType = ShellyNotificationType.NotifyFullStatus;
                    else notificationType = ShellyNotificationType.Other;

                    continue;
                }

                if (!reader.Read()) return false;
                if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray) reader.Skip();
            }
        }
        catch (JsonException)
        {
            return false;
        }

        return responseId.HasValue || notificationType != ShellyNotificationType.None;
    }

    /// <summary>Creates a version-specific authentication template from the first 401 challenge without retaining the plain-text password.</summary>
    private static AuthenticationTemplateResult TryCreateAuthenticationTemplate(ShellyDeviceInfo deviceInfo, string password, string challengeJson)
    {
        if (!TryReadAuthenticationChallenge(challengeJson, out var challenge, out var challengeError))
        {
            return new AuthenticationTemplateResult(ShellyLoginStatus.ClientError, null, challengeError);
        }

        if (!string.Equals(challenge!.AuthenticationType, "digest", StringComparison.OrdinalIgnoreCase))
        {
            return new AuthenticationTemplateResult(ShellyLoginStatus.UnsupportedAuthentication, null, $"Unsupported authentication type '{challenge.AuthenticationType}'.");
        }

        if (!string.Equals(challenge.Algorithm, Sha256Algorithm, StringComparison.OrdinalIgnoreCase))
        {
            return new AuthenticationTemplateResult(ShellyLoginStatus.UnsupportedAuthentication, null, $"Unsupported authentication algorithm '{challenge.Algorithm}'.");
        }

        if (string.IsNullOrWhiteSpace(challenge.Nonce)) return new AuthenticationTemplateResult(ShellyLoginStatus.ClientError, null, "Authentication challenge does not contain a nonce.");
        if (!TryReadFirmwareMajorVersion(deviceInfo.FirmwareVersion, out var firmwareMajorVersion)) return new AuthenticationTemplateResult(ShellyLoginStatus.ClientError, null, $"Unsupported firmware version format '{deviceInfo.FirmwareVersion}'.");

        var realm = challenge.Realm ?? deviceInfo.AuthenticationDomain ?? deviceInfo.Id;
        var ha1 = ComputeSha256Hex($"{AuthenticationUser}:{realm}:{password}");
        var usesReusableNonce = firmwareMajorVersion >= 2;
        var legacyNonceCount = challenge.NonceCount.GetValueOrDefault(1);
        if (!usesReusableNonce && legacyNonceCount <= 0) return new AuthenticationTemplateResult(ShellyLoginStatus.ClientError, null, "Authentication challenge contains an invalid nonce counter.");

        if (!TryCreateAuthenticationTemplate(realm, ha1, challenge.Nonce, legacyNonceCount, usesReusableNonce, out var authenticationTemplate, out var templateError))
        {
            return new AuthenticationTemplateResult(ShellyLoginStatus.ClientError, null, templateError);
        }

        return new AuthenticationTemplateResult(ShellyLoginStatus.Success, authenticationTemplate, null);
    }

    /// <summary>Creates a fresh firmware 2.0+ authentication template from a stale challenge while reusing the already derived HA1.</summary>
    private static bool TryRefreshAuthenticationTemplate(ShellyAuthenticationTemplate currentTemplate, ShellyAuthChallenge challenge, out ShellyAuthenticationTemplate? refreshedTemplate, out string? errorMessage)
    {
        refreshedTemplate = null;
        errorMessage = null;

        if (!currentTemplate.UsesReusableNonce || challenge.Stale != true) return false;
        if (!string.Equals(challenge.AuthenticationType, "digest", StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = $"Unsupported authentication type '{challenge.AuthenticationType}'.";
            return false;
        }

        if (!string.Equals(challenge.Algorithm, Sha256Algorithm, StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = $"Unsupported authentication algorithm '{challenge.Algorithm}'.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(challenge.Nonce))
        {
            errorMessage = "Stale authentication challenge does not contain a replacement nonce.";
            return false;
        }

        if (challenge.Realm is { } realm && !string.Equals(realm, currentTemplate.Realm, StringComparison.Ordinal))
        {
            errorMessage = "Stale authentication challenge changed the realm and cannot reuse the existing credential hash.";
            return false;
        }

        return TryCreateAuthenticationTemplate(currentTemplate.Realm, currentTemplate.Ha1, challenge.Nonce, 1, usesReusableNonce: true, out refreshedTemplate, out errorMessage);
    }

    /// <summary>Builds the immutable auth suffix that is copied into each protected request before request-specific fields are patched.</summary>
    private static bool TryCreateAuthenticationTemplate(string realm, string ha1, string nonce, int legacyNonceCount, bool usesReusableNonce, out ShellyAuthenticationTemplate? authenticationTemplate, out string? errorMessage)
    {
        authenticationTemplate = null;
        errorMessage = null;

        string nonceJson;
        if (usesReusableNonce)
        {
            nonceJson = JsonSerializer.Serialize(nonce);
        }
        else
        {
            if (!long.TryParse(nonce, NumberStyles.None, CultureInfo.InvariantCulture, out var legacyNonce) || legacyNonce < 0)
            {
                errorMessage = "Legacy authentication challenge contains a non-numeric nonce.";
                return false;
            }

            nonceJson = legacyNonce.ToString(CultureInfo.InvariantCulture);
        }

        var realmJson = JsonSerializer.Serialize(realm);
        var clientNonce = RandomNumberGenerator.GetInt32(1, int.MaxValue);
        var nonceCountText = usesReusableNonce ? "00000000" : legacyNonceCount.ToString(CultureInfo.InvariantCulture);
        var nonceCountJson = usesReusableNonce ? $"\"nc\":\"{nonceCountText}\"," : string.Empty;
        var initialResponse = usesReusableNonce
            ? new string(' ', AuthenticationResponseFieldLength)
            : ComputeSha256Hex($"{ha1}:{nonce}:{nonceCountText}:{clientNonce}:auth:{WebSocketHa2}");
        var authJson = $",\"auth\":{{\"realm\":{realmJson},\"username\":\"{AuthenticationUser}\",\"nonce\":{nonceJson},{nonceCountJson}\"cnonce\":{clientNonce},\"response\":\"{initialResponse}\",\"algorithm\":\"{Sha256Algorithm}\"}}}}";
        var payload = Encoding.UTF8.GetBytes(authJson);

        var nonceCountOffset = usesReusableNonce ? FindJsonValueOffset(payload, "\"nc\":\""u8) : -1;
        var responseOffset = usesReusableNonce ? FindJsonValueOffset(payload, "\"response\":\""u8) : -1;
        if (usesReusableNonce && (nonceCountOffset < 0 || responseOffset < 0))
        {
            errorMessage = "Failed to build authentication template offsets.";
            return false;
        }

        authenticationTemplate = new ShellyAuthenticationTemplate(realm, ha1, nonce, clientNonce, usesReusableNonce, payload, nonceCountOffset, responseOffset);
        return true;
    }

    /// <summary>Finds the first byte after a JSON property prefix inside a prebuilt ASCII-compatible template.</summary>
    private static int FindJsonValueOffset(byte[] template, ReadOnlySpan<byte> prefix)
    {
        var index = template.AsSpan().IndexOf(prefix);
        return index < 0 ? -1 : index + prefix.Length;
    }

    /// <summary>Parses a digest challenge returned inside a Shelly 401 error message.</summary>
    private static bool TryReadAuthenticationChallenge(string challengeJson, out ShellyAuthChallenge? challenge, out string? errorMessage)
    {
        challenge = null;
        errorMessage = null;

        try
        {
            challenge = JsonSerializer.Deserialize(challengeJson, ShellyJsonSerializerContext.Default.ShellyAuthChallenge);
        }
        catch (JsonException exception)
        {
            errorMessage = exception.Message;
            return false;
        }

        if (challenge is not null) return true;

        errorMessage = "Shelly returned an empty authentication challenge.";
        return false;
    }

    /// <summary>Reads only the major firmware component so prerelease and vendor suffixes do not affect authentication protocol selection.</summary>
    private static bool TryReadFirmwareMajorVersion(string firmwareVersion, out int majorVersion)
    {
        var separatorIndex = firmwareVersion.IndexOf('.');
        var majorVersionText = separatorIndex < 0 ? firmwareVersion.AsSpan() : firmwareVersion.AsSpan(0, separatorIndex);
        return int.TryParse(majorVersionText, NumberStyles.None, CultureInfo.InvariantCulture, out majorVersion) && majorVersion >= 0;
    }

    /// <summary>Maps transport-level login failures while leaving successful and RPC-error responses to the authentication flow.</summary>
    private static ShellyLoginResult? CreateLoginTransportFailure<T>(ShellyRpcResult<T> result)
    {
        return result.Status switch
        {
            ShellyRpcResultStatus.Success or ShellyRpcResultStatus.RpcError => null,
            ShellyRpcResultStatus.NotConnected => new ShellyLoginResult(ShellyLoginStatus.NotConnected, null, null),
            ShellyRpcResultStatus.Canceled => new ShellyLoginResult(ShellyLoginStatus.Canceled, null, null),
            ShellyRpcResultStatus.TimedOut => new ShellyLoginResult(ShellyLoginStatus.TimedOut, null, null),
            ShellyRpcResultStatus.ConnectionLost => new ShellyLoginResult(ShellyLoginStatus.ConnectionLost, null, null),
            _ => new ShellyLoginResult(ShellyLoginStatus.ClientError, null, result.ErrorMessage)
        };
    }

    /// <summary>Calculates a lower-case SHA-256 digest compatible with Shelly digest authentication.</summary>
    private static string ComputeSha256Hex(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>Gets the writer belonging to the currently active WebSocket generation.</summary>
    private ChannelWriter<OutgoingMessage>? GetOutputWriter()
    {
        var webSocket = _webSocket;
        if (_disconnectRequested || webSocket?.State != WebSocketState.Open) return null;

        return _webSocketOutputQueue?.Writer;
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

    /// <summary>Aborts the single active transport and waits for its loops to release resources.</summary>
    private async Task AbortConnectionAsync()
    {
        _disconnectRequested = true;
        _webSocketOutputQueue?.Writer.TryComplete();
        _webSocket?.Abort();

        var cancellationSource = _connectionCancellationSource;
        if (cancellationSource is not null) CancelSafely(cancellationSource);

        var receiveTask = _receiveTask;
        if (receiveTask is null) return;

        try
        {
            await receiveTask.ConfigureAwait(false);
        }
        catch
        {
        }
    }

    /// <summary>Attempts one Shelly WebSocket transport and reports expected connection failure through a null result.</summary>
    private static async Task<ClientWebSocket?> TryConnectWebSocketAsync(Uri endpoint, bool acceptUntrustedServerCertificate, CancellationToken cancellationToken)
    {
        var webSocket = CreateWebSocket(acceptUntrustedServerCertificate);

        try
        {
            await webSocket.ConnectAsync(endpoint, cancellationToken).ConfigureAwait(false);
            return webSocket;
        }
        catch (OperationCanceledException)
        {
            webSocket.Dispose();
            return null;
        }
        catch (Exception exception) when (IsTransportException(exception))
        {
            webSocket.Dispose();
            return null;
        }
    }

    /// <summary>Creates the WebSocket configured for either the plain or TLS Shelly RPC transport.</summary>
    private static ClientWebSocket CreateWebSocket(bool acceptUntrustedServerCertificate)
    {
        var webSocket = new ClientWebSocket();
        webSocket.Options.KeepAliveInterval = WebSocketKeepAliveInterval;
        webSocket.Options.KeepAliveTimeout = WebSocketKeepAliveTimeout;

        if (acceptUntrustedServerCertificate) webSocket.Options.RemoteCertificateValidationCallback = static (_, _, _, _) => true;

        return webSocket;
    }

    /// <summary>Builds one inbound Shelly WebSocket endpoint.</summary>
    private static Uri CreateRpcEndpoint(string scheme, IPAddress address, int port)
    {
        var builder = new UriBuilder(scheme, address.ToString(), port, RpcPath);
        return builder.Uri;
    }

    /// <summary>Cancels the connection token while tolerating concurrent transport cleanup.</summary>
    private static void CancelSafely(CancellationTokenSource cancellationSource)
    {
        try
        {
            cancellationSource.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    /// <summary>Raises the unexpected-disconnection notification without allowing consumer code to fault transport cleanup.</summary>
    private void RaiseDisconnectedUnexpectedly(ShellyUnexpectedDisconnectionEventArgs eventArgs)
    {
        try
        {
            DisconnectedUnexpectedly?.Invoke(this, eventArgs);
        }
        catch
        {
        }
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

    /// <summary>Raises diagnostics for an unclassified text frame without retaining the reusable receive buffer.</summary>
    private void RaiseUnknownMessageReceived(Memory<byte> message, string reason)
    {
        var eventArgs = new ShellyUnknownMessageEventArgs(reason, Encoding.UTF8.GetString(message.Span));

        try
        {
            UnknownMessageReceived?.Invoke(this, eventArgs);
        }
        catch
        {
        }
    }

    /// <summary>Identifies transport failures expected during the normal WebSocket lifecycle.</summary>
    private static bool IsTransportException(Exception exception)
    {
        return exception is WebSocketException or HttpRequestException or IOException or ObjectDisposedException;
    }

    /// <summary>Owns one pooled outbound payload until the send loop returns it to the shared pool.</summary>
    private readonly struct OutgoingMessage
    {
        /// <summary>Creates one outbound payload owned by the output queue.</summary>
        public OutgoingMessage(byte[] buffer, int length)
        {
            Buffer = buffer;
            Length = length;
        }

        /// <summary>Gets the pooled payload buffer.</summary>
        public byte[] Buffer { get; }

        /// <summary>Gets the number of payload bytes to send.</summary>
        public int Length { get; }
    }

    /// <summary>Stores one immutable authentication suffix and the firmware 2.0+ counter used to patch reusable nonce requests.</summary>
    private sealed class ShellyAuthenticationTemplate
    {
        private int _nonceCount;

        /// <summary>Creates one authentication template bound to a realm, server nonce, and client nonce.</summary>
        public ShellyAuthenticationTemplate(string realm, string ha1, string nonce, int clientNonce, bool usesReusableNonce, byte[] payload, int nonceCountOffset, int responseOffset)
        {
            Realm = realm;
            Ha1 = ha1;
            Nonce = nonce;
            ClientNonce = clientNonce;
            UsesReusableNonce = usesReusableNonce;
            Payload = payload;
            NonceCountOffset = nonceCountOffset;
            ResponseOffset = responseOffset;
        }

        /// <summary>Gets the realm used when HA1 was derived.</summary>
        public string Realm { get; }

        /// <summary>Gets the precomputed credential hash so stale nonce renewal does not require retaining the password.</summary>
        public string Ha1 { get; }

        /// <summary>Gets the normalized server nonce text used in the digest calculation.</summary>
        public string Nonce { get; }

        /// <summary>Gets the client nonce generated once for this server nonce.</summary>
        public int ClientNonce { get; }

        /// <summary>Gets whether firmware 2.0+ nonce reuse rules apply to this template.</summary>
        public bool UsesReusableNonce { get; }

        /// <summary>Gets the immutable JSON suffix beginning with the auth property and ending with the outer object brace.</summary>
        public byte[] Payload { get; }

        /// <summary>Gets the offset of the eight-character firmware 2.0+ nonce counter, or -1 for legacy firmware.</summary>
        public int NonceCountOffset { get; }

        /// <summary>Gets the offset of the firmware 2.0+ SHA-256 response field, or -1 for legacy firmware.</summary>
        public int ResponseOffset { get; }

        /// <summary>Advances the firmware 2.0+ nonce counter and calculates the matching digest response.</summary>
        public AuthenticationRequestValues CreateRequestValues()
        {
            var nonceCount = Interlocked.Increment(ref _nonceCount);
            var nonceCountText = nonceCount.ToString("x8", CultureInfo.InvariantCulture);
            var response = ComputeSha256Hex($"{Ha1}:{Nonce}:{nonceCountText}:{ClientNonce}:auth:{WebSocketHa2}");
            return new AuthenticationRequestValues(nonceCountText, response);
        }
    }

    /// <summary>Contains the firmware 2.0+ values patched into one owned authenticated RPC buffer.</summary>
    private readonly struct AuthenticationRequestValues
    {
        /// <summary>Creates request-specific authentication values.</summary>
        public AuthenticationRequestValues(string nonceCount, string response)
        {
            NonceCount = nonceCount;
            Response = response;
        }

        /// <summary>Gets the eight-character hexadecimal nonce count.</summary>
        public string NonceCount { get; }

        /// <summary>Gets the SHA-256 response matching the nonce count.</summary>
        public string Response { get; }
    }

    /// <summary>Materializes a successful JSON-RPC result from the complete response and the exact result payload.</summary>
    private delegate T? RpcResultParser<T>(ReadOnlySpan<byte> response, ReadOnlySpan<byte> resultPayload);

    /// <summary>Provides type erasure for pending requests stored together while preserving typed completion in each concrete request.</summary>
    private abstract class PendingRequest
    {
        /// <summary>Completes this request from a response while the receive buffer is still valid.</summary>
        public abstract void CompleteResponse(ReadOnlySpan<byte> message);

        /// <summary>Completes this request with an explicit client or transport failure.</summary>
        public abstract void CompleteFailure(ShellyRpcResultStatus status, string? errorMessage);
    }

    /// <summary>Stores one request-specific result parser and the completion source for an in-flight RPC request.</summary>
    private sealed class PendingRequest<T> : PendingRequest
    {
        private readonly TaskCompletionSource<ShellyRpcResult<T>> _completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly RpcResultParser<T>? _resultParser;

        /// <summary>Creates a pending request that either materializes or intentionally ignores a successful RPC result.</summary>
        public PendingRequest(RpcResultParser<T>? resultParser)
        {
            _resultParser = resultParser;
        }

        /// <summary>Gets the task completed by the receive loop, timeout path, cancellation path, or connection cleanup.</summary>
        public Task<ShellyRpcResult<T>> Task => _completionSource.Task;

        /// <summary>Parses one response into an explicit request result without allowing request-specific parsing failures to terminate the receive loop.</summary>
        public override void CompleteResponse(ReadOnlySpan<byte> message)
        {
            ShellyRpcResult<T> result;

            try
            {
                result = ParseRpcResponse(message, _resultParser);
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                result = new ShellyRpcResult<T>(ShellyRpcResultStatus.ClientError, default, null, exception.Message);
            }

            _completionSource.TrySetResult(result);
        }

        /// <summary>Completes the request without a response value when transport or client processing fails.</summary>
        public override void CompleteFailure(ShellyRpcResultStatus status, string? errorMessage)
        {
            var result = new ShellyRpcResult<T>(status, default, null, errorMessage);
            _completionSource.TrySetResult(result);
        }
    }

    /// <summary>Represents a successful RPC payload that the caller intentionally does not need to materialize.</summary>
    private readonly struct IgnoredRpcResult
    {
    }

    /// <summary>Represents the result of creating one version-specific Shelly authentication template.</summary>
    private readonly struct AuthenticationTemplateResult
    {
        /// <summary>Creates an explicit authentication-template creation result.</summary>
        public AuthenticationTemplateResult(ShellyLoginStatus status, ShellyAuthenticationTemplate? authenticationTemplate, string? errorMessage)
        {
            Status = status;
            AuthenticationTemplate = authenticationTemplate;
            ErrorMessage = errorMessage;
        }

        /// <summary>Gets the authentication outcome.</summary>
        public ShellyLoginStatus Status { get; }

        /// <summary>Gets the authentication template when creation succeeded.</summary>
        public ShellyAuthenticationTemplate? AuthenticationTemplate { get; }

        /// <summary>Gets client-side diagnostic information when creation failed.</summary>
        public string? ErrorMessage { get; }
    }

    private enum EnqueueStatus : byte
    {
        Queued,
        Canceled,
        ConnectionLost
    }

    private enum ShellyNotificationType : byte
    {
        None,
        NotifyStatus,
        NotifyFullStatus,
        NotifyEvent,
        Other
    }
}

/// <summary>Represents a non-hot-path Shelly message delivered to the configuration message channel.</summary>
public interface IShellyMessage
{
    /// <summary>Gets the discriminant used for exhaustive message handling.</summary>
    ShellyMessageType MessageType { get; }
}


/// <summary>Describes how an attempt to establish the Shelly WebSocket transport completed.</summary>
public enum ConnectionResult
{
    /// <summary>The client connected to the documented ws:// device RPC endpoint.</summary>
    Connected,

    /// <summary>The client was already connected and no new connection was created.</summary>
    AlreadyConnected,

    /// <summary>The connection attempt was canceled before a transport was established.</summary>
    Canceled,

    /// <summary>Neither the plain nor TLS WebSocket endpoint could establish a usable connection.</summary>
    ConnectionFailed,

    /// <summary>The transport connected but Shelly.GetDeviceInfo could not initialize the client.</summary>
    InitializationFailed
}

/// <summary>Describes how an explicit disconnection request completed.</summary>
public enum DisconnectionResult
{
    /// <summary>The active connection was closed.</summary>
    Disconnected,

    /// <summary>No active connection existed.</summary>
    NotConnected,

    /// <summary>The graceful shutdown was canceled and the transport was aborted.</summary>
    Canceled,

    /// <summary>The graceful shutdown failed because of a transport error and the transport was aborted.</summary>
    TransportError,

    /// <summary>The client failed while completing the requested shutdown.</summary>
    ClientError
}

/// <summary>Describes why an established connection ended without an explicit DisconnectAsync request.</summary>
public enum ShellyDisconnectionReason
{
    /// <summary>The remote Shelly endpoint initiated a WebSocket close.</summary>
    RemoteClose,

    /// <summary>The transport failed while a connection loop was active.</summary>
    TransportError,

    /// <summary>The client failed while processing an established connection.</summary>
    ClientError
}

/// <summary>Provides explicit information about an unexpected connection termination.</summary>
public sealed class ShellyUnexpectedDisconnectionEventArgs : EventArgs
{
    /// <summary>Creates information describing an unexpected connection termination.</summary>
    public ShellyUnexpectedDisconnectionEventArgs(ShellyDisconnectionReason reason, WebSocketCloseStatus? closeStatus, string? closeStatusDescription, string? errorMessage)
    {
        Reason = reason;
        CloseStatus = closeStatus;
        CloseStatusDescription = closeStatusDescription;
        ErrorMessage = errorMessage;
    }

    /// <summary>Gets the category that caused the connection to end.</summary>
    public ShellyDisconnectionReason Reason { get; }

    /// <summary>Gets the WebSocket close status when the peer supplied one.</summary>
    public WebSocketCloseStatus? CloseStatus { get; }

    /// <summary>Gets the WebSocket close description when the peer supplied one.</summary>
    public string? CloseStatusDescription { get; }

    /// <summary>Gets diagnostic transport information when the connection ended because of a local transport failure.</summary>
    public string? ErrorMessage { get; }
}

/// <summary>Provides metadata about a complete WebSocket message whose type is not consumed by the JSON-RPC client.</summary>
public sealed class ShellyUnsupportedMessageTypeEventArgs : EventArgs
{
    /// <summary>Creates metadata for an unsupported complete WebSocket message.</summary>
    public ShellyUnsupportedMessageTypeEventArgs(WebSocketMessageType messageType, int messageLength)
    {
        MessageType = messageType;
        MessageLength = messageLength;
    }

    /// <summary>Gets the unsupported WebSocket message type.</summary>
    public WebSocketMessageType MessageType { get; }

    /// <summary>Gets the total payload length after all fragments were received.</summary>
    public int MessageLength { get; }
}

/// <summary>Provides diagnostics for a rare text frame that cannot be routed by the Shelly client.</summary>
public sealed class ShellyUnknownMessageEventArgs : EventArgs
{
    /// <summary>Creates diagnostics for one unclassified text frame.</summary>
    public ShellyUnknownMessageEventArgs(string reason, string payload)
    {
        Reason = reason;
        Payload = payload;
    }

    /// <summary>Gets the reason why the frame could not be routed.</summary>
    public string Reason { get; }

    /// <summary>Gets a detached UTF-8 representation of the original text payload.</summary>
    public string Payload { get; }
}

/// <summary>Describes how an RPC call completed across Shelly protocol and client transport concerns.</summary>
public enum ShellyRpcResultStatus
{
    /// <summary>Shelly returned a successful RPC result.</summary>
    Success,

    /// <summary>Shelly returned an explicit JSON-RPC error response.</summary>
    RpcError,

    /// <summary>The operation requires an active connection but no connection exists.</summary>
    NotConnected,

    /// <summary>The operation was canceled by the caller.</summary>
    Canceled,

    /// <summary>The response did not arrive before the default RPC timeout elapsed.</summary>
    TimedOut,

    /// <summary>The connection ended before the RPC response could be received.</summary>
    ConnectionLost,

    /// <summary>The client could not process the operation or response.</summary>
    ClientError
}

/// <summary>Represents an RPC outcome without requiring exceptions for expected protocol or connection failures.</summary>
public readonly record struct ShellyRpcResult<T>
{
    /// <summary>Creates an explicit RPC result containing either a value, a Shelly protocol error, or a client-side failure.</summary>
    public ShellyRpcResult(ShellyRpcResultStatus status, T? value, ShellyRpcError? rpcError, string? errorMessage)
    {
        Status = status;
        Value = value;
        RpcError = rpcError;
        ErrorMessage = errorMessage;
    }

    /// <summary>Gets the outcome category.</summary>
    public ShellyRpcResultStatus Status { get; }

    /// <summary>Gets the deserialized RPC value when the call succeeded.</summary>
    public T? Value { get; }

    /// <summary>Gets the error returned by Shelly when the protocol response is an RPC error.</summary>
    public ShellyRpcError? RpcError { get; }

    /// <summary>Gets diagnostic client-side information when the operation could not be completed locally.</summary>
    public string? ErrorMessage { get; }

    /// <summary>Gets whether the call completed with a successful Shelly result.</summary>
    public bool IsSuccess => Status == ShellyRpcResultStatus.Success;
}

/// <summary>Represents an error returned by the Shelly JSON-RPC protocol.</summary>
public sealed class ShellyRpcError
{
    /// <summary>Gets the Shelly RPC error code.</summary>
    public int Code { get; init; }

    /// <summary>Gets the Shelly RPC error message.</summary>
    public string Message { get; init; } = string.Empty;
}

/// <summary>Describes the outcome of explicit Shelly digest-authentication validation.</summary>
public enum ShellyLoginStatus
{
    /// <summary>The credentials were accepted and digest state is ready for protected RPC calls.</summary>
    Success,

    /// <summary>The device does not currently require authentication.</summary>
    AuthenticationNotRequired,

    /// <summary>No active WebSocket connection exists.</summary>
    NotConnected,

    /// <summary>The caller canceled authentication.</summary>
    Canceled,

    /// <summary>A challenge or authenticated response exceeded the default RPC timeout.</summary>
    TimedOut,

    /// <summary>The connection ended before authentication completed.</summary>
    ConnectionLost,

    /// <summary>The authenticated retry was rejected with another 401 response.</summary>
    InvalidCredentials,

    /// <summary>The device requested an authentication mechanism or algorithm this client does not implement.</summary>
    UnsupportedAuthentication,

    /// <summary>Shelly returned a non-authentication RPC error while validating credentials.</summary>
    RpcError,

    /// <summary>The client could not parse or validate the authentication exchange.</summary>
    ClientError
}

/// <summary>Represents an authentication outcome without using exceptions as a consumer-facing transport contract.</summary>
public readonly record struct ShellyLoginResult
{
    /// <summary>Creates one explicit authentication result.</summary>
    public ShellyLoginResult(ShellyLoginStatus status, ShellyRpcError? rpcError, string? errorMessage)
    {
        Status = status;
        RpcError = rpcError;
        ErrorMessage = errorMessage;
    }

    /// <summary>Gets the authentication outcome.</summary>
    public ShellyLoginStatus Status { get; }

    /// <summary>Gets a Shelly RPC error when authentication failed through an explicit protocol error.</summary>
    public ShellyRpcError? RpcError { get; }

    /// <summary>Gets diagnostic client-side information when the authentication exchange could not be processed.</summary>
    public string? ErrorMessage { get; }

    /// <summary>Gets whether authentication completed successfully or was not required by the device.</summary>
    public bool IsSuccess => Status is ShellyLoginStatus.Success or ShellyLoginStatus.AuthenticationNotRequired;
}