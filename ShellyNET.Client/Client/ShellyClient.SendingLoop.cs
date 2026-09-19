using System.Net.WebSockets;
using System.Threading.Channels;

namespace ShellyNET.Client;

public partial class ShellyClient
{
    private Channel<OutgoingMessage>? _webSocketOutputQueue;

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
                    try
                    {
                        Console.WriteLine("Outgoing Msg: " + Encoding.UTF8.GetString(message.Buffer.AsSpan(0, message.Length)));
                        Console.WriteLine();
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

}
