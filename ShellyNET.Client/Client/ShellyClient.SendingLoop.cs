using System.Buffers.Text;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Threading.Channels;

namespace ShellyNET.Client;

public partial class ShellyClient
{
    private Channel<OutgoingMessage>? _webSocketOutputQueue;

    /// <summary>Serializes authentication finalization and WebSocket sends while returning every pooled buffer exactly once.</summary>
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
                        var messageLength = FinalizeOutgoingMessage(message);
                        Console.WriteLine("Outgoing Msg: " + Encoding.UTF8.GetString(message.Buffer.AsSpan(0, messageLength)));
                        Console.WriteLine();
                        await webSocket.SendAsync(message.Buffer.AsMemory(0, messageLength), WebSocketMessageType.Text, true, cancellationSource.Token).ConfigureAwait(false);
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

    /// <summary>Appends the authentication selected for one send attempt and assigns reusable nonce counters in physical send order.</summary>
    private static int FinalizeOutgoingMessage(OutgoingMessage message)
    {
        var authenticationState = message.Authentication;
        if (authenticationState is null) return message.Length;
        if (message.Length == 0) throw new InvalidOperationException("Cannot append authentication to an empty RPC request.");

        var authenticationOffset = message.Length - 1;
        var authenticationDestination = message.Buffer.AsSpan(authenticationOffset, authenticationState.Template.Length);
        authenticationState.Template.CopyTo(authenticationDestination);

        var digestInput = authenticationState.DigestInput;
        if (digestInput is null) return checked(authenticationOffset + authenticationState.Template.Length);
        if (authenticationState.NonceCount == int.MaxValue) throw new InvalidOperationException("The authentication nonce counter exceeded the supported range.");

        var nonceCount = ++authenticationState.NonceCount;
        Span<byte> nonceCountBytes = stackalloc byte[ModernNonceCountFieldLength];
        var nonceCountFormatted = Utf8Formatter.TryFormat(nonceCount, nonceCountBytes, out var nonceCountBytesWritten, ModernNonceCountFormat) && nonceCountBytesWritten == ModernNonceCountFieldLength;
        if (!nonceCountFormatted) throw new InvalidOperationException("The authentication nonce counter did not fit the reserved field.");

        nonceCountBytes.CopyTo(digestInput.AsSpan(authenticationState.DigestNonceCountOffset, ModernNonceCountFieldLength));
        nonceCountBytes.CopyTo(authenticationDestination.Slice(authenticationState.NonceCountOffset, ModernNonceCountFieldLength));

        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(digestInput, hash);
        WriteLowerHex(hash, authenticationDestination.Slice(authenticationState.ResponseOffset, AuthenticationResponseFieldLength));
        return checked(authenticationOffset + authenticationState.Template.Length);
    }
}
