using ShellyNET.Client.Model;
using ShellyNET.Client.Model.Enums;
using ShellyNET.Client.Model.Messages;
using System.Buffers;
using System.Buffers.Text;
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
    private const int InitialRpcRequestBufferSize = 1024;
    private const int AuthenticationResponseFieldLength = 64;
    private const int ModernNonceCountFieldLength = 8;
    private const string ModernNonceCountPlaceholder = "00000000";
    private const string AuthenticationResponsePlaceholder = "0000000000000000000000000000000000000000000000000000000000000000";
    private const string RpcPath = "rpc";
    private const string AuthenticationUser = "admin";
    private const string Sha256Algorithm = "SHA-256";
    private const string WebSocketHa2 = "6370ec69915103833b5222b368555393393f098bfbfbb59f47e0590af135f062";
    private static readonly TimeSpan DefaultRpcTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan WebSocketKeepAliveInterval = TimeSpan.FromSeconds(55);
    private static readonly TimeSpan WebSocketKeepAliveTimeout = TimeSpan.FromSeconds(10);
    private static readonly StandardFormat ModernNonceCountFormat = new('x', ModernNonceCountFieldLength);

    private readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;
    private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);
    private readonly SemaphoreSlim _authenticatedSendSemaphore = new(1, 1);
    private readonly ConcurrentDictionary<ushort, PendingRequest> _pendingRequests = new();

    private ClientWebSocket? _webSocket;
    private Uri? _connectedEndpoint;
    private CancellationTokenSource? _connectionCancellationSource;
    private Task? _receiveTask;
    private Task? _sendTask;
    private ShellyDeviceInfo? _deviceInfo;
    private AuthenticationState? _authenticationState;
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

            var deviceInfoResult = await Call_Shelly_GetDeviceInfoAsync(ct).ConfigureAwait(false);
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

    /// <summary>Validates credentials against a protected RPC and publishes authentication state for later protected calls.</summary>
    public async Task<ShellyLoginResult> LoginToDeviceAsync(string password, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(password);

        var deviceInfo = DeviceInfo;
        if (!IsConnected || deviceInfo is null) return new ShellyLoginResult(ShellyLoginStatus.NotConnected, null, null);

        if (!deviceInfo.AuthenticationEnabled)
        {
            Interlocked.Exchange(ref _authenticationState, null);
            return new ShellyLoginResult(ShellyLoginStatus.AuthenticationNotRequired, null, null);
        }

        Interlocked.Exchange(ref _authenticationState, null);

        var challengeResult = await SendRpcCoreAsync<EmptyRpcParameters, IgnoredRpcResult>(RpcMethods.Shelly_GetStatus, null, null, null, cancellationToken).ConfigureAwait(false);
        var challengeFailure = CreateLoginTransportFailure(challengeResult);
        if (challengeFailure is { } failure) return failure;

        if (challengeResult.Status == ShellyRpcResultStatus.Success) return new ShellyLoginResult(ShellyLoginStatus.AuthenticationNotRequired, null, null);

        var challengeError = challengeResult.RpcError;
        if (challengeError is not { Code: 401 })
        {
            return new ShellyLoginResult(ShellyLoginStatus.RpcError, challengeError, challengeError is null ? "Protected RPC returned neither a result nor an authentication challenge." : null);
        }

        var authenticationStatus = TryCreateAuthenticationState(deviceInfo, password, challengeError.Message, out var authenticationState, out var authenticationError);
        if (authenticationStatus != ShellyLoginStatus.Success)
        {
            return new ShellyLoginResult(authenticationStatus, challengeError, authenticationError);
        }

        var authenticatedResult = await SendRpcCoreAsync<EmptyRpcParameters, IgnoredRpcResult>(RpcMethods.Shelly_GetStatus, null, authenticationState, null, cancellationToken).ConfigureAwait(false);
        var authenticatedFailure = CreateLoginTransportFailure(authenticatedResult);
        if (authenticatedFailure is { } authenticatedFailureResult) return authenticatedFailureResult;

        if (authenticatedResult.RpcError is { Code: 401 } invalidCredentialsError)
        {
            return new ShellyLoginResult(ShellyLoginStatus.InvalidCredentials, invalidCredentialsError, null);
        }

        if (authenticatedResult.RpcError is not null) return new ShellyLoginResult(ShellyLoginStatus.RpcError, authenticatedResult.RpcError, null);

        Interlocked.Exchange(ref _authenticationState, authenticationState);
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
        Interlocked.Exchange(ref _authenticationState, null);
        _webSocket = webSocket;
        _sendTask = SendLoopAsync();
        _receiveTask = ReceiveLoopAsync();
    }

    /// <summary>Appends the authentication object to a serialized RPC request and patches values that change for each reusable-nonce request.</summary>
    private static void AppendAuthentication(PooledByteBufferWriter bufferWriter, AuthenticationState authenticationState)
    {
        if (bufferWriter.WrittenCount == 0) throw new InvalidOperationException("Cannot append authentication to an empty RPC request.");

        bufferWriter.Rewind(1);
        var authenticationDestination = bufferWriter.GetSpan(authenticationState.Template.Length).Slice(0, authenticationState.Template.Length);
        authenticationState.Template.CopyTo(authenticationDestination);

        if (authenticationState.DigestInput is not null)
        {
            if (authenticationState.NonceCount == int.MaxValue) throw new InvalidOperationException("The authentication nonce counter exceeded the supported range.");

            var nonceCount = ++authenticationState.NonceCount;
            Span<byte> nonceCountBytes = stackalloc byte[ModernNonceCountFieldLength];
            if (!Utf8Formatter.TryFormat(nonceCount, nonceCountBytes, out var nonceCountBytesWritten, ModernNonceCountFormat) || nonceCountBytesWritten != ModernNonceCountFieldLength)
            {
                throw new InvalidOperationException("The authentication nonce counter did not fit the reserved field.");
            }

            nonceCountBytes.CopyTo(authenticationState.DigestInput.AsSpan(authenticationState.DigestNonceCountOffset, ModernNonceCountFieldLength));
            nonceCountBytes.CopyTo(authenticationDestination.Slice(authenticationState.NonceCountOffset, ModernNonceCountFieldLength));

            Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
            SHA256.HashData(authenticationState.DigestInput, hash);
            WriteLowerHex(hash, authenticationDestination.Slice(authenticationState.ResponseOffset, AuthenticationResponseFieldLength));
        }

        bufferWriter.Advance(authenticationState.Template.Length);
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


    /// <summary>Transfers one pooled payload to the active session queue and returns ownership immediately when cancellation prevents enqueueing.</summary>
    private ValueTask<EnqueueStatus> EnqueueOutgoingMessageAsync(ChannelWriter<OutgoingMessage> outputWriter, OutgoingMessage message, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            _pool.Return(message.Buffer);
            return ValueTask.FromResult(EnqueueStatus.Canceled);
        }

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
        catch
        {
            _pool.Return(message.Buffer);
            throw;
        }
    }

    /// <summary>Validates one JSON-RPC response and materializes its successful result while the receive buffer is still valid.</summary>
    private static ShellyRpcResult<T> ParseRpcResponse<T>(Memory<byte> message, RpcResultParser<T>? resultParser)
    {
        var reader = new Utf8JsonReader(message.Span);
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

            while (true)
            {
                if (reader.TokenType == JsonTokenType.EndObject) break;
                if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }
                bool propertyRead = false;

                if (reader.ValueTextEquals("id"u8))
                {
                    reader.Read();
                    responseId = reader.GetUInt16();
                    reader.Read();
                    propertyRead = true;
                    return true;
                }

                if (reader.ValueTextEquals("src"u8))
                {
                    reader.Read();
                    reader.Read();
                    propertyRead = true;
                }

                if (reader.ValueTextEquals("dst"u8))
                {
                    reader.Read();
                    reader.Read();
                    propertyRead = true;
                }

                if (reader.ValueTextEquals("method"u8))
                {
                    reader.Read();

                    if (reader.ValueTextEquals("NotifyStatus"u8)) notificationType = ShellyNotificationType.NotifyStatus;
                    else if (reader.ValueTextEquals("NotifyEvent"u8)) notificationType = ShellyNotificationType.NotifyEvent;
                    else if (reader.ValueTextEquals("NotifyFullStatus"u8)) notificationType = ShellyNotificationType.NotifyFullStatus;
                    else notificationType = ShellyNotificationType.Other;
                    reader.Read();
                    propertyRead = true;
                    return true;
                }

                if (reader.ValueTextEquals("params"u8) || reader.ValueTextEquals("result"u8))
                {
                    reader.Read();
                    reader.Skip();
                    reader.Read();
                    propertyRead = true;
                }
 
            }
        }
        catch (JsonException)
        {
            return false;
        }

        return responseId.HasValue || notificationType != ShellyNotificationType.None;
    }

    /// <summary>Creates version-specific authentication state from the first 401 challenge without retaining the plain-text password.</summary>
    private static ShellyLoginStatus TryCreateAuthenticationState(ShellyDeviceInfo deviceInfo, string password, string challengeJson, out AuthenticationState? authenticationState, out string? errorMessage)
    {
        authenticationState = null;
        errorMessage = null;

        if (!TryReadAuthenticationChallenge(challengeJson, out var challenge, out errorMessage)) return ShellyLoginStatus.ClientError;

        var challengeStatus = ValidateAuthenticationChallenge(challenge!, requireStale: false, out errorMessage);
        if (challengeStatus != ShellyLoginStatus.Success) return challengeStatus;
        if (!TryReadFirmwareMajorVersion(deviceInfo.FirmwareVersion, out var firmwareMajorVersion))
        {
            errorMessage = $"Unsupported firmware version format '{deviceInfo.FirmwareVersion}'.";
            return ShellyLoginStatus.ClientError;
        }

        var realm = deviceInfo.AuthenticationDomain ?? deviceInfo.Id;
        if (challenge!.Realm is { } challengeRealm && !string.Equals(challengeRealm, realm, StringComparison.Ordinal))
        {
            errorMessage = "Authentication challenge realm does not match the connected device.";
            return ShellyLoginStatus.ClientError;
        }

        var ha1Prefix = CreateHa1Prefix(realm, password);

        var usesReusableNonce = firmwareMajorVersion >= 2;
        var legacyNonceCount = challenge.NonceCount.GetValueOrDefault(1);
        if (!usesReusableNonce && legacyNonceCount <= 0)
        {
            errorMessage = "Authentication challenge contains an invalid nonce counter.";
            return ShellyLoginStatus.ClientError;
        }

        if (!TryCreateAuthenticationState(realm, ha1Prefix, challenge.Nonce!, legacyNonceCount, usesReusableNonce, out authenticationState, out errorMessage))
        {
            return ShellyLoginStatus.ClientError;
        }

        return ShellyLoginStatus.Success;
    }

    /// <summary>Creates fresh firmware 2.0+ authentication state from a stale challenge while reusing the already derived HA1.</summary>
    private static bool TryRefreshAuthenticationState(ShellyDeviceInfo deviceInfo, AuthenticationState currentState, ShellyAuthChallenge challenge, out AuthenticationState? refreshedState, out string? errorMessage)
    {
        refreshedState = null;
        errorMessage = null;

        if (!currentState.UsesReusableNonce) return false;

        var challengeStatus = ValidateAuthenticationChallenge(challenge, requireStale: true, out errorMessage);
        if (challengeStatus != ShellyLoginStatus.Success) return false;

        var realm = deviceInfo.AuthenticationDomain ?? deviceInfo.Id;
        if (challenge.Realm is { } challengeRealm && !string.Equals(challengeRealm, realm, StringComparison.Ordinal))
        {
            errorMessage = "Stale authentication challenge changed the realm and cannot reuse the existing credential hash.";
            return false;
        }

        return TryCreateAuthenticationState(realm, currentState.Ha1Prefix, challenge.Nonce!, 1, usesReusableNonce: true, out refreshedState, out errorMessage);
    }

    /// <summary>Validates protocol fields shared by initial and stale authentication challenges and classifies unsupported mechanisms explicitly.</summary>
    private static ShellyLoginStatus ValidateAuthenticationChallenge(ShellyAuthChallenge challenge, bool requireStale, out string? errorMessage)
    {
        errorMessage = null;

        if (!string.Equals(challenge.AuthenticationType, "digest", StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = $"Unsupported authentication type '{challenge.AuthenticationType}'.";
            return ShellyLoginStatus.UnsupportedAuthentication;
        }

        if (!string.Equals(challenge.Algorithm, Sha256Algorithm, StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = $"Unsupported authentication algorithm '{challenge.Algorithm}'.";
            return ShellyLoginStatus.UnsupportedAuthentication;
        }

        if (string.IsNullOrWhiteSpace(challenge.Nonce))
        {
            errorMessage = requireStale ? "Stale authentication challenge does not contain a replacement nonce." : "Authentication challenge does not contain a nonce.";
            return ShellyLoginStatus.ClientError;
        }

        if (requireStale && challenge.Stale != true)
        {
            errorMessage = "Authentication challenge does not mark the nonce as stale.";
            return ShellyLoginStatus.ClientError;
        }

        return ShellyLoginStatus.Success;
    }

    /// <summary>Builds immutable authentication bytes for one challenge and precomputes the digest input reused while its nonce remains valid.</summary>
    private static bool TryCreateAuthenticationState(string realm, byte[] ha1Prefix, string nonce, int legacyNonceCount, bool usesReusableNonce, out AuthenticationState? authenticationState, out string? errorMessage)
    {
        authenticationState = null;
        errorMessage = null;

        long legacyNonce = 0;
        var nonceCountText = ModernNonceCountPlaceholder;
        if (!usesReusableNonce)
        {
            var hasValidLegacyNonce = long.TryParse(nonce, NumberStyles.None, CultureInfo.InvariantCulture, out legacyNonce) && legacyNonce >= 0;
            if (!hasValidLegacyNonce)
            {
                errorMessage = "Legacy authentication challenge contains a non-numeric nonce.";
                return false;
            }

            nonceCountText = legacyNonceCount.ToString(CultureInfo.InvariantCulture);
        }

        var clientNonce = RandomNumberGenerator.GetInt32(1, int.MaxValue);
        var template = CreateAuthenticationTemplate(realm, nonce, legacyNonce, clientNonce, nonceCountText, usesReusableNonce);
        var responseOffset = FindJsonValueOffset(template, "\"response\":\""u8);
        if (responseOffset < 0)
        {
            errorMessage = "Failed to locate the authentication response field.";
            return false;
        }

        var digestInput = CreateAuthenticationDigestInput(ha1Prefix, nonce, nonceCountText, clientNonce, out var digestNonceCountOffset);
        if (!usesReusableNonce)
        {
            Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
            SHA256.HashData(digestInput, hash);
            WriteLowerHex(hash, template.AsSpan(responseOffset, AuthenticationResponseFieldLength));
            authenticationState = new AuthenticationState(template, ha1Prefix, null, -1, -1, -1);
            return true;
        }

        var nonceCountOffset = FindJsonValueOffset(template, "\"nc\":\""u8);
        if (nonceCountOffset < 0)
        {
            errorMessage = "Failed to locate the authentication nonce counter field.";
            return false;
        }

        authenticationState = new AuthenticationState(template, ha1Prefix, digestInput, nonceCountOffset, digestNonceCountOffset, responseOffset);
        return true;
    }

    /// <summary>Creates the authentication JSON suffix once for a nonce so request sends only copy and patch fixed offsets.</summary>
    private static byte[] CreateAuthenticationTemplate(string realm, string nonce, long legacyNonce, int clientNonce, string nonceCountText, bool usesReusableNonce)
    {
        var templateBuffer = new ArrayBufferWriter<byte>(256);
        using var jsonWriter = new Utf8JsonWriter(templateBuffer);

        jsonWriter.WriteStartObject();
        jsonWriter.WritePropertyName("auth");
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString("realm", realm);
        jsonWriter.WriteString("username", AuthenticationUser);
        if (usesReusableNonce) jsonWriter.WriteString("nonce", nonce);
        else jsonWriter.WriteNumber("nonce", legacyNonce);
        jsonWriter.WriteNumber("cnonce", clientNonce);
        if (usesReusableNonce) jsonWriter.WriteString("nc", nonceCountText);
        jsonWriter.WriteString("response", AuthenticationResponsePlaceholder);
        jsonWriter.WriteString("algorithm", Sha256Algorithm);
        jsonWriter.WriteEndObject();
        jsonWriter.WriteEndObject();
        jsonWriter.Flush();

        var template = templateBuffer.WrittenSpan.ToArray();
        template[0] = (byte)',';
        return template;
    }

    /// <summary>Creates the immutable lowercase HA1 prefix retained after login so the plain-text password is no longer needed.</summary>
    private static byte[] CreateHa1Prefix(string realm, string password)
    {
        var credentialBytes = Encoding.UTF8.GetBytes($"{AuthenticationUser}:{realm}:{password}");
        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(credentialBytes, hash);

        var ha1Prefix = new byte[AuthenticationResponseFieldLength + 1];
        WriteLowerHex(hash, ha1Prefix.AsSpan(0, AuthenticationResponseFieldLength));
        ha1Prefix[^1] = (byte)':';
        return ha1Prefix;
    }

    /// <summary>Creates the SHA-256 input whose nonce counter is the only value changed between firmware 2.0+ requests.</summary>
    private static byte[] CreateAuthenticationDigestInput(byte[] ha1Prefix, string nonce, string nonceCountText, int clientNonce, out int nonceCountOffset)
    {
        var digestTail = Encoding.UTF8.GetBytes($"{nonce}:{nonceCountText}:{clientNonce}:auth:{WebSocketHa2}");
        var digestInput = new byte[ha1Prefix.Length + digestTail.Length];
        ha1Prefix.CopyTo(digestInput, 0);
        digestTail.CopyTo(digestInput, ha1Prefix.Length);

        nonceCountOffset = ha1Prefix.Length + Encoding.UTF8.GetByteCount(nonce) + 1;
        return digestInput;
    }

    /// <summary>Writes a fixed-size digest as lowercase hexadecimal ASCII without allocating a string.</summary>
    private static void WriteLowerHex(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        var converted = Convert.TryToHexString(source, destination, out var bytesWritten) && bytesWritten == destination.Length;
        if (!converted) throw new InvalidOperationException("The hexadecimal digest did not fit the reserved destination.");

        var status = Ascii.ToLowerInPlace(destination, out var bytesLowercased);
        if (status != OperationStatus.Done || bytesLowercased != destination.Length) throw new InvalidOperationException("The hexadecimal digest could not be normalized to lowercase ASCII.");
    }

    /// <summary>Finds the first byte after a JSON property prefix inside a prebuilt ASCII-compatible suffix.</summary>
    private static int FindJsonValueOffset(byte[] suffix, ReadOnlySpan<byte> prefix)
    {
        var index = suffix.AsSpan().IndexOf(prefix);
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

    /// <summary>Provides growable pooled storage for JSON serialization and transfers the final buffer to the send queue without another copy.</summary>
    private sealed class PooledByteBufferWriter : IBufferWriter<byte>, IDisposable
    {
        private readonly ArrayPool<byte> _bufferPool;
        private byte[]? _buffer;
        private int _writtenCount;

        /// <summary>Creates a pooled writer with enough initial capacity for common Shelly command frames.</summary>
        public PooledByteBufferWriter(ArrayPool<byte> bufferPool, int initialCapacity)
        {
            _bufferPool = bufferPool;
            _buffer = bufferPool.Rent(initialCapacity);
        }

        /// <summary>Gets the number of bytes currently written into the owned buffer.</summary>
        public int WrittenCount => _writtenCount;

        /// <summary>Commits bytes previously obtained from GetMemory or GetSpan.</summary>
        public void Advance(int count)
        {
            var buffer = GetBuffer();
            if (count < 0 || _writtenCount > buffer.Length - count) throw new ArgumentOutOfRangeException(nameof(count));
            _writtenCount += count;
        }

        /// <summary>Returns writable memory with at least the requested capacity after the current payload.</summary>
        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            EnsureAdditionalCapacity(sizeHint);
            return GetBuffer().AsMemory(_writtenCount);
        }

        /// <summary>Returns a writable span with at least the requested capacity after the current payload.</summary>
        public Span<byte> GetSpan(int sizeHint = 0)
        {
            EnsureAdditionalCapacity(sizeHint);
            return GetBuffer().AsSpan(_writtenCount);
        }

        /// <summary>Moves the write cursor backward so the final request brace can be replaced by the authentication suffix.</summary>
        public void Rewind(int count)
        {
            if (count < 0 || count > _writtenCount) throw new ArgumentOutOfRangeException(nameof(count));
            _writtenCount -= count;
        }

        /// <summary>Transfers buffer ownership to an outgoing message and prevents disposal from returning that buffer to the pool.</summary>
        public OutgoingMessage DetachMessage()
        {
            var buffer = GetBuffer();
            var message = new OutgoingMessage(buffer, _writtenCount);
            _buffer = null;
            _writtenCount = 0;
            return message;
        }

        /// <summary>Returns the owned buffer when serialization did not transfer it to the send queue.</summary>
        public void Dispose()
        {
            var buffer = _buffer;
            if (buffer is null) return;

            _buffer = null;
            _writtenCount = 0;
            _bufferPool.Return(buffer);
        }

        /// <summary>Ensures subsequent writes can append the requested number of bytes without losing already serialized data.</summary>
        private void EnsureAdditionalCapacity(int sizeHint)
        {
            if (sizeHint < 0) throw new ArgumentOutOfRangeException(nameof(sizeHint));
            if (sizeHint == 0) sizeHint = 1;

            var buffer = GetBuffer();
            if (sizeHint <= buffer.Length - _writtenCount) return;

            var requiredCapacity = checked(_writtenCount + sizeHint);
            var expandedCapacity = Math.Max(requiredCapacity, checked(buffer.Length * 2));
            var expandedBuffer = _bufferPool.Rent(expandedCapacity);
            buffer.AsSpan(0, _writtenCount).CopyTo(expandedBuffer);
            _bufferPool.Return(buffer);
            _buffer = expandedBuffer;
        }

        /// <summary>Returns the currently owned buffer and rejects access after ownership was transferred or disposed.</summary>
        private byte[] GetBuffer()
        {
            return _buffer ?? throw new ObjectDisposedException(nameof(PooledByteBufferWriter));
        }
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

    /// <summary>Contains the immutable authentication template and digest state bound to one authentication nonce.</summary>
    private sealed class AuthenticationState
    {
        /// <summary>Creates authentication state for either one fixed legacy digest or one reusable firmware 2.0+ nonce.</summary>
        public AuthenticationState(byte[] template, byte[] ha1Prefix, byte[]? digestInput, int nonceCountOffset, int digestNonceCountOffset, int responseOffset)
        {
            Template = template;
            Ha1Prefix = ha1Prefix;
            DigestInput = digestInput;
            NonceCountOffset = nonceCountOffset;
            DigestNonceCountOffset = digestNonceCountOffset;
            ResponseOffset = responseOffset;
        }

        /// <summary>Gets the immutable JSON suffix copied after the serialized RPC body for this authentication nonce.</summary>
        public byte[] Template { get; }

        /// <summary>Gets the lowercase HA1 bytes followed by a colon so stale nonce renewal does not require the password.</summary>
        public byte[] Ha1Prefix { get; }

        /// <summary>Gets the prebuilt firmware 2.0+ digest input, or null when the legacy response is already fixed in the suffix.</summary>
        public byte[]? DigestInput { get; }

        /// <summary>Gets the nonce-counter offset inside the firmware 2.0+ suffix.</summary>
        public int NonceCountOffset { get; }

        /// <summary>Gets the nonce-counter offset inside the firmware 2.0+ digest input.</summary>
        public int DigestNonceCountOffset { get; }

        /// <summary>Gets the response offset inside the firmware 2.0+ suffix.</summary>
        public int ResponseOffset { get; }

        /// <summary>Gets whether this state uses firmware 2.0+ reusable-nonce semantics.</summary>
        public bool UsesReusableNonce => DigestInput is not null;

        /// <summary>Gets or sets the last nonce count assigned while the authenticated-send semaphore is held.</summary>
        public int NonceCount { get; set; }
    }

    /// <summary>Provides type erasure for pending requests stored together while preserving typed completion in each concrete request.</summary>
    private abstract class PendingRequest
    {
        /// <summary>Completes this request from a response while the receive buffer is still valid.</summary>
        public abstract void CompleteResponse(Memory<byte> message);

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
        public override void CompleteResponse(Memory<byte> message)
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