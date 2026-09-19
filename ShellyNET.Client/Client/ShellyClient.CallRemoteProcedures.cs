using ShellyNET.Client.Model;
using ShellyNET.Client.Model.Messages;
using ShellyNET.Client.Parsers;
using System.Net.WebSockets;
using System.Text.Json.Serialization.Metadata;

namespace ShellyNET.Client;

public partial class ShellyClient
{
    private readonly ShellyComponentsConfigParser _configParser = new();

    /// <summary>Materializes a successful JSON-RPC result from the complete response and the exact result payload.</summary>
    private delegate T? RpcResultParser<T>(Memory<byte> response, Memory<byte> resultPayload);

    /// <summary>Loads device identity without authentication and refreshes the information used by connection initialization.</summary>
    public async Task<ShellyRpcResult<ShellyDeviceInfo>> Call_Shelly_GetDeviceInfoAsync(CancellationToken ct = default)
    {
        var result = await SendRpcCoreAsync<EmptyRpcParameters, ShellyDeviceInfo>(RpcMethods.Shelly_GetDeviceInfo, null, null, static (_, resultPayload) => JsonSerializer.Deserialize(resultPayload.Span, ShellyJsonSerializerContext.Default.ShellyDeviceInfo), ct).ConfigureAwait(false);
        if (result.Status != ShellyRpcResultStatus.Success) return result;
        if (result.Value is null) return new ShellyRpcResult<ShellyDeviceInfo>(ShellyRpcResultStatus.ClientError, null, null, "Shelly.GetDeviceInfo returned an empty result.");

        Volatile.Write(ref _deviceInfo, result.Value);
        if (!result.Value.AuthenticationEnabled) Interlocked.Exchange(ref _authenticationState, null);
        return result;
    }

    /// <summary>Loads the complete device configuration and converts component entries into their strongly typed configuration models.</summary>
    public async Task<ShellyRpcResult<List<ShellyComponentConfigBase>>> Call_Shelly_GetDeviceConfigAsync(CancellationToken ct = default)
    {
        var response = await SendRpcAsync<EmptyRpcParameters, byte[]>(RpcMethods.Shelly_GetConfig, null, static (message, _) => message.ToArray(), ct).ConfigureAwait(false);
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

    /// <summary>Loads the complete device status and publishes it through the existing component-state processing path.</summary>
    public async Task<ShellyRpcResult<IShellyStatusMessage[]>> Call_Shelly_GetDeviceStatus(CancellationToken ct = default)
    {
        var response = await SendRpcAsync<EmptyRpcParameters, ShellyNotifyStatusParseResult>(RpcMethods.Shelly_GetStatus, null, (messge, resultPayload) =>
        {
            var parser = new ShellyNotifyStatusParser();
            return parser.ProcessNotifyStatusMessage(messge, StatesContainingObject.Results, ct);
        }, ct).ConfigureAwait(false);

        if (!response.IsSuccess) return new ShellyRpcResult<IShellyStatusMessage[]>(response.Status, null, response.RpcError, response.ErrorMessage);

        var messages = response.Value.MessageSpan.ToArray().OfType<IShellyStatusMessage>();
        var textMessages = response.Value.StringMessageSpan.ToArray().OfType<IShellyStatusMessage>();
        var allMessages = messages.Concat(textMessages).ToArray();
        response.Value.Dispose();
        return new ShellyRpcResult<IShellyStatusMessage[]>(ShellyRpcResultStatus.Success, allMessages, null, null);
    }

    /// <summary>Disengages the requested circuit breaker and returns whether it was engaged before the command.</summary>
    public async Task<ShellyRpcResult<DeviceUpdateResult>> Call_Shelly_CheckForUpdate(CancellationToken ct = default)
    {
        return await SendRpcResultAsync<DeviceUpdateResult>(RpcMethods.Shelly_CheckForUpdate, ct);
    }

    /// <summary>Disengages the requested circuit breaker and returns whether it was engaged before the command.</summary>
    public async Task<ShellyRpcResult<NullRpcResponse>> Call_Shelly_Reboot(CancellationToken ct = default)
    {
        return await SendRpcResultAsync<NullRpcResponse>(RpcMethods.Shelly_Reboot, ct);
    }

    /// <summary>Disengages the requested circuit breaker and returns whether it was engaged before the command.</summary>
    public ValueTask<ShellyRpcResult<CbSetResult>> CallCbSetAsync(byte componentId, CancellationToken cancellationToken = default)
    {
        var parameters = new CbSetParams(componentId, false);
        return SendRpcResultAsync<CbSetParams, CbSetResult>(RpcMethods.CB_Set, parameters, cancellationToken);
    }

    /// <summary>Retrieves circuit breaker activity created after the supplied UTC time.</summary>
    public ValueTask<ShellyRpcResult<CbGetLogResult>> CallCbGetLogAsync(byte componentId, DateTimeOffset after, CancellationToken cancellationToken = default)
    {
        var unixTimestamp = after.ToUnixTimeSeconds();
        var parameters = new CbGetLogParams(componentId, unixTimestamp);
        return SendRpcResultAsync<CbGetLogParams, CbGetLogResult>(RpcMethods.CB_GetLog, parameters, cancellationToken);
    }

    /// <summary>Applies selected output, brightness or color-temperature changes to a CCT component.</summary>
    public ValueTask CallCctSetAsync(byte componentId, bool? state = null, byte? brightnessPercent = null, ushort? colorTemperatureKelvin = null, TimeSpan? transitionTime = null, TimeSpan? toggleAfter = null, sbyte? brightnessOffsetPercent = null, string? tag = null, CancellationToken cancellationToken = default)
    {
        var hasNoRequestedValue = state is null && brightnessPercent is null && colorTemperatureKelvin is null;
        if (hasNoRequestedValue) throw new ArgumentException("CCT.Set requires at least one of state, brightnessPercent or colorTemperatureKelvin.");

        ValidatePercent(brightnessPercent, nameof(brightnessPercent), allowZero: true);
        ValidateSignedPercent(brightnessOffsetPercent, nameof(brightnessOffsetPercent));

        var hasBrightnessConflict = brightnessPercent is not null && brightnessOffsetPercent is not null;
        if (hasBrightnessConflict) throw new ArgumentException("brightnessPercent and brightnessOffsetPercent are mutually exclusive.");

        ValidateTag(tag);

        var transitionDurationSeconds = ToSeconds(transitionTime, nameof(transitionTime));
        var toggleAfterSeconds = ToSeconds(toggleAfter, nameof(toggleAfter));
        var parameters = new CctSetParams(componentId, state, brightnessPercent, colorTemperatureKelvin, transitionDurationSeconds, toggleAfterSeconds, brightnessOffsetPercent, tag);
        return SendRpcAckAsync(RpcMethods.CCT_Set, parameters, cancellationToken);
    }

    /// <summary>Toggles the output state of a CCT component.</summary>
    public ValueTask CallCctToggleAsync(byte componentId, string? tag = null, CancellationToken cancellationToken = default)
    {
        ValidateTag(tag);
        var parameters = new CctToggleParams(componentId, tag);
        return SendRpcAckAsync(RpcMethods.CCT_Toggle, parameters, cancellationToken);
    }

    /// <summary>Starts the calibration procedure for the requested cover component.</summary>
    public ValueTask CallCoverCalibrateAsync(byte componentId, CancellationToken cancellationToken = default)
    {
        var parameters = new CoverCalibrateParams(componentId);
        return SendRpcAckAsync(RpcMethods.Cover_Calibrate, parameters, cancellationToken);
    }

    /// <summary>Opens a cover, optionally limiting movement duration and tagging the state change.</summary>
    public ValueTask CallCoverOpenAsync(byte componentId, TimeSpan? duration = null, string? tag = null, CancellationToken cancellationToken = default)
    {
        ValidateCoverDuration(duration, nameof(duration));
        ValidateTag(tag);
        var durationSeconds = ToSeconds(duration, nameof(duration));
        var parameters = new CoverOpenParams(componentId, durationSeconds, tag);
        return SendRpcAckAsync(RpcMethods.Cover_Open, parameters, cancellationToken);
    }

    /// <summary>Closes a cover, optionally limiting movement duration and tagging the state change.</summary>
    public ValueTask CallCoverCloseAsync(byte componentId, TimeSpan? duration = null, string? tag = null, CancellationToken cancellationToken = default)
    {
        ValidateCoverDuration(duration, nameof(duration));
        ValidateTag(tag);
        var durationSeconds = ToSeconds(duration, nameof(duration));
        var parameters = new CoverCloseParams(componentId, durationSeconds, tag);
        return SendRpcAckAsync(RpcMethods.Cover_Close, parameters, cancellationToken);
    }

    /// <summary>Stops the current cover movement.</summary>
    public ValueTask CallCoverStopAsync(byte componentId, string? tag = null, CancellationToken cancellationToken = default)
    {
        ValidateTag(tag);
        var parameters = new CoverStopParams(componentId, tag);
        return SendRpcAckAsync(RpcMethods.Cover_Stop, parameters, cancellationToken);
    }

    /// <summary>Moves a cover or its slats to an absolute position or by a relative amount.</summary>
    public ValueTask CallCoverGoToPositionAsync(byte componentId, byte? positionPercent = null, sbyte? relativePercent = null, byte? slatPositionPercent = null, sbyte? slatRelativePercent = null, string? tag = null, CancellationToken cancellationToken = default)
    {
        var hasCoverPositionConflict = positionPercent is not null && relativePercent is not null;
        if (hasCoverPositionConflict) throw new ArgumentException("positionPercent and relativePercent are mutually exclusive.");

        var hasSlatPositionConflict = slatPositionPercent is not null && slatRelativePercent is not null;
        if (hasSlatPositionConflict) throw new ArgumentException("slatPositionPercent and slatRelativePercent are mutually exclusive.");

        var hasNoRequestedPosition = positionPercent is null && relativePercent is null && slatPositionPercent is null && slatRelativePercent is null;
        if (hasNoRequestedPosition) throw new ArgumentException("At least one cover or slat position must be specified.");

        ValidatePercent(positionPercent, nameof(positionPercent), allowZero: true);
        ValidateSignedPercent(relativePercent, nameof(relativePercent));
        ValidatePercent(slatPositionPercent, nameof(slatPositionPercent), allowZero: true);
        ValidateSignedPercent(slatRelativePercent, nameof(slatRelativePercent));
        ValidateTag(tag);

        var parameters = new CoverGoToPositionParams(componentId, positionPercent, relativePercent, slatPositionPercent, slatRelativePercent, tag);
        return SendRpcAckAsync(RpcMethods.Cover_GoToPosition, parameters, cancellationToken);
    }

    /// <summary>Starts a DALI bus scan to discover connected devices.</summary>
    public ValueTask CallDaliStartScanAsync(CancellationToken cancellationToken = default)
    {
        return SendRpcAckAsync(RpcMethods.DALI_StartScan, cancellationToken);
    }

    /// <summary>Pings the DALI devices already known by Shelly.</summary>
    public ValueTask CallDaliPingKnownDevicesAsync(CancellationToken cancellationToken = default)
    {
        return SendRpcAckAsync(RpcMethods.DALI_PingKnownDevices, cancellationToken);
    }

    /// <summary>Applies selected output or brightness changes to a light component.</summary>
    public ValueTask CallLightSetAsync(byte componentId, bool? state = null, byte? brightnessPercent = null, TimeSpan? transitionTime = null, TimeSpan? toggleAfter = null, sbyte? brightnessOffsetPercent = null, string? tag = null, CancellationToken cancellationToken = default)
    {
        var hasNoRequestedValue = state is null && brightnessPercent is null;
        if (hasNoRequestedValue) throw new ArgumentException("Light.Set requires at least one of state or brightnessPercent.");

        ValidatePercent(brightnessPercent, nameof(brightnessPercent), allowZero: true);
        ValidateSignedPercent(brightnessOffsetPercent, nameof(brightnessOffsetPercent));

        var hasBrightnessConflict = brightnessPercent is not null && brightnessOffsetPercent is not null;
        if (hasBrightnessConflict) throw new ArgumentException("brightnessPercent and brightnessOffsetPercent are mutually exclusive.");

        ValidateTag(tag);

        var transitionDurationSeconds = ToSeconds(transitionTime, nameof(transitionTime));
        var toggleAfterSeconds = ToSeconds(toggleAfter, nameof(toggleAfter));
        var parameters = new LightSetParams(componentId, state, brightnessPercent, transitionDurationSeconds, toggleAfterSeconds, brightnessOffsetPercent, tag);
        return SendRpcAckAsync(RpcMethods.Light_Set, parameters, cancellationToken);
    }

    /// <summary>Toggles the output state of a light component.</summary>
    public ValueTask CallLightToggleAsync(byte componentId, string? tag = null, CancellationToken cancellationToken = default)
    {
        ValidateTag(tag);
        var parameters = new LightToggleParams(componentId, tag);
        return SendRpcAckAsync(RpcMethods.Light_Toggle, parameters, cancellationToken);
    }

    /// <summary>Starts output calibration for the requested light component.</summary>
    public ValueTask CallLightCalibrateAsync(byte componentId, CancellationToken cancellationToken = default)
    {
        var parameters = new LightCalibrateParams(componentId);
        return SendRpcAckAsync(RpcMethods.Light_Calibrate, parameters, cancellationToken);
    }

    /// <summary>Applies selected output, brightness or color changes to an RGB component.</summary>
    public ValueTask CallRgbSetAsync(byte componentId, bool? state = null, byte? brightnessPercent = null, Rgb24? color = null, TimeSpan? transitionTime = null, TimeSpan? toggleAfter = null, sbyte? brightnessOffsetPercent = null, string? tag = null, CancellationToken cancellationToken = default)
    {
        var hasNoRequestedValue = state is null && brightnessPercent is null;
        if (hasNoRequestedValue) throw new ArgumentException("RGB.Set requires at least one of state or brightnessPercent according to the RPC documentation.");

        ValidatePercent(brightnessPercent, nameof(brightnessPercent), allowZero: false);
        ValidateSignedPercent(brightnessOffsetPercent, nameof(brightnessOffsetPercent));

        var hasBrightnessConflict = brightnessPercent is not null && brightnessOffsetPercent is not null;
        if (hasBrightnessConflict) throw new ArgumentException("brightnessPercent and brightnessOffsetPercent are mutually exclusive.");

        ValidateTag(tag);

        var transitionDurationSeconds = ToSeconds(transitionTime, nameof(transitionTime));
        var toggleAfterSeconds = ToSeconds(toggleAfter, nameof(toggleAfter));
        var parameters = new RgbSetParams(componentId, state, brightnessPercent, color, transitionDurationSeconds, toggleAfterSeconds, brightnessOffsetPercent, tag);
        return SendRpcAckAsync(RpcMethods.RGB_Set, parameters, cancellationToken);
    }

    /// <summary>Toggles the output state of an RGB component.</summary>
    public ValueTask CallRgbToggleAsync(byte componentId, string? tag = null, CancellationToken cancellationToken = default)
    {
        ValidateTag(tag);
        var parameters = new RgbToggleParams(componentId, tag);
        return SendRpcAckAsync(RpcMethods.RGB_Toggle, parameters, cancellationToken);
    }

    /// <summary>Applies selected output, brightness, RGB or color-temperature changes to an RGBCCT component.</summary>
    public ValueTask CallRgbCctSetAsync(byte componentId, bool? state = null, byte? brightnessPercent = null, Rgb24? color = null, ushort? colorTemperatureKelvin = null, RgbCctMode? mode = null, TimeSpan? transitionTime = null, TimeSpan? toggleAfter = null, sbyte? brightnessOffsetPercent = null, string? tag = null, CancellationToken cancellationToken = default)
    {
        var hasNoRequestedValue = state is null && brightnessPercent is null;
        if (hasNoRequestedValue) throw new ArgumentException("RGBCCT.Set requires at least one of state or brightnessPercent according to the RPC documentation.");

        ValidatePercent(brightnessPercent, nameof(brightnessPercent), allowZero: false);
        ValidateSignedPercent(brightnessOffsetPercent, nameof(brightnessOffsetPercent));

        var hasBrightnessConflict = brightnessPercent is not null && brightnessOffsetPercent is not null;
        if (hasBrightnessConflict) throw new ArgumentException("brightnessPercent and brightnessOffsetPercent are mutually exclusive.");

        ValidateTag(tag);

        var transitionDurationSeconds = ToSeconds(transitionTime, nameof(transitionTime));
        var toggleAfterSeconds = ToSeconds(toggleAfter, nameof(toggleAfter));
        var parameters = new RgbCctSetParams(componentId, state, brightnessPercent, color, colorTemperatureKelvin, mode, transitionDurationSeconds, toggleAfterSeconds, brightnessOffsetPercent, tag);
        return SendRpcAckAsync(RpcMethods.RGBCCT_Set, parameters, cancellationToken);
    }

    /// <summary>Toggles the output state of an RGBCCT component.</summary>
    public ValueTask CallRgbCctToggleAsync(byte componentId, string? tag = null, CancellationToken cancellationToken = default)
    {
        ValidateTag(tag);
        var parameters = new RgbCctToggleParams(componentId, tag);
        return SendRpcAckAsync(RpcMethods.RGBCCT_Toggle, parameters, cancellationToken);
    }

    /// <summary>Applies selected output, brightness, RGB or white-channel changes to an RGBW component.</summary>
    public ValueTask CallRgbWSetAsync(byte componentId, bool? state = null, byte? brightnessPercent = null, Rgb24? color = null, byte? white = null, TimeSpan? transitionTime = null, TimeSpan? toggleAfter = null, sbyte? brightnessOffsetPercent = null, short? whiteOffset = null, string? tag = null, CancellationToken cancellationToken = default)
    {
        var hasNoRequestedValue = state is null && brightnessPercent is null;
        if (hasNoRequestedValue) throw new ArgumentException("RGBW.Set requires at least one of state or brightnessPercent according to the RPC documentation.");

        ValidatePercent(brightnessPercent, nameof(brightnessPercent), allowZero: false);
        ValidateSignedPercent(brightnessOffsetPercent, nameof(brightnessOffsetPercent));

        var hasBrightnessConflict = brightnessPercent is not null && brightnessOffsetPercent is not null;
        if (hasBrightnessConflict) throw new ArgumentException("brightnessPercent and brightnessOffsetPercent are mutually exclusive.");

        var hasWhiteConflict = white is not null && whiteOffset is not null;
        if (hasWhiteConflict) throw new ArgumentException("white and whiteOffset are mutually exclusive.");

        var isWhiteOffsetOutsideRange = whiteOffset is < -255 or > 255;
        if (isWhiteOffsetOutsideRange) throw new ArgumentOutOfRangeException(nameof(whiteOffset), "whiteOffset must be in -255..255.");

        ValidateTag(tag);

        var transitionDurationSeconds = ToSeconds(transitionTime, nameof(transitionTime));
        var toggleAfterSeconds = ToSeconds(toggleAfter, nameof(toggleAfter));
        var parameters = new RgbWSetParams(componentId, state, brightnessPercent, color, white, transitionDurationSeconds, toggleAfterSeconds, brightnessOffsetPercent, whiteOffset, tag);
        return SendRpcAckAsync(RpcMethods.RGBW_Set, parameters, cancellationToken);
    }

    /// <summary>Toggles the output state of an RGBW component.</summary>
    public ValueTask CallRgbWToggleAsync(byte componentId, string? tag = null, CancellationToken cancellationToken = default)
    {
        ValidateTag(tag);
        var parameters = new RgbWToggleParams(componentId, tag);
        return SendRpcAckAsync(RpcMethods.RGBW_Toggle, parameters, cancellationToken);
    }

    /// <summary>Restarts the Shelly device immediately or after the requested delay.</summary>
    public ValueTask CallShellyRebootAsync(TimeSpan? delay = null, CancellationToken cancellationToken = default)
    {
        if (delay is null) return SendRpcAckAsync(RpcMethods.Shelly_Reboot, cancellationToken);
        if (delay.Value < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(delay));

        var delayMilliseconds = delay.Value.Ticks / TimeSpan.TicksPerMillisecond;
        var parameters = new ShellyRebootParams(delayMilliseconds);
        return SendRpcAckAsync(RpcMethods.Shelly_Reboot, parameters, cancellationToken);
    }

    /// <summary>Sets a switch output and returns whether the switch was on before the command.</summary>
    public ValueTask<ShellyRpcResult<SwitchSetResult>> CallSwitchSetAsync(byte componentId, bool state, TimeSpan? toggleAfter = null, string? tag = null, CancellationToken cancellationToken = default)
    {
        ValidateTag(tag);
        var toggleAfterSeconds = ToSeconds(toggleAfter, nameof(toggleAfter));
        var parameters = new SwitchSetParams(componentId, state, toggleAfterSeconds, tag);
        return SendRpcResultAsync<SwitchSetParams, SwitchSetResult>(RpcMethods.Switch_Set, parameters, cancellationToken);
    }

    /// <summary>Toggles a switch output and returns whether the switch was on before the command.</summary>
    public ValueTask<ShellyRpcResult<SwitchToggleResult>> CallSwitchToggleAsync(byte componentId, string? tag = null, CancellationToken cancellationToken = default)
    {
        ValidateTag(tag);
        var parameters = new SwitchToggleParams(componentId, tag);
        return SendRpcResultAsync<SwitchToggleParams, SwitchToggleResult>(RpcMethods.Switch_Toggle, parameters, cancellationToken);
    }

    /// <summary>Mutes the smoke alarm associated with the requested smoke component.</summary>
    public ValueTask CallSmokeMuteAsync(byte componentId, CancellationToken cancellationToken = default)
    {
        var parameters = new SmokeMuteParams(componentId);
        return SendRpcAckAsync(RpcMethods.Smoke_Mute, parameters, cancellationToken);
    }

    /// <summary>Sets the device clock from a UTC time using a whole-second Unix timestamp.</summary>
    public ValueTask CallSysSetTimeAsync(DateTimeOffset utcTime, CancellationToken cancellationToken = default)
    {
        var unixTimestamp = utcTime.ToUnixTimeSeconds();
        var parameters = new SysSetTimeParams(unixTimestamp);
        return SendRpcAckAsync(RpcMethods.Sys_SetTime, parameters, cancellationToken);
    }

    /// <summary>Sends a typed RPC and materializes its successful result through source-generated serializer metadata.</summary>
    private ValueTask<ShellyRpcResult<TResult>> SendRpcResultAsync<TParams, TResult>(RpcMethods method, TParams parameters, CancellationToken cancellationToken) where TParams : struct
    {
        var pendingResult = SendRpcAsync<TParams, TResult>(method, parameters, static (_, resultPayload) => DeserializeRpcResult<TResult>(resultPayload.Span), cancellationToken);
        var result = new ValueTask<ShellyRpcResult<TResult>>(pendingResult);
        return result;
    }

    /// <summary>Sends a typed RPC and materializes its successful result through source-generated serializer metadata.</summary>
    private ValueTask<ShellyRpcResult<TResult>> SendRpcResultAsync<TResult>(RpcMethods method, CancellationToken cancellationToken)
    {
        // It doesn't look good that we need to pass params type if we don't pass params
        var pendingResult = SendRpcAsync<EmptyRpcParameters, TResult>(method, null, static (_, resultPayload) => DeserializeRpcResult<TResult>(resultPayload.Span), cancellationToken);
        var result = new ValueTask<ShellyRpcResult<TResult>>(pendingResult);
        return result;
    }

    /// <summary>Sends a typed RPC and waits for the device response when the successful result payload is not needed by the caller.</summary>
    private ValueTask SendRpcAckAsync<TParams>(RpcMethods method, TParams parameters, CancellationToken cancellationToken) where TParams : struct
    {
        // It doesn't look good that we need to pass result type if we don't expect results
        var pendingResult = SendRpcAsync<TParams, IgnoredRpcResult>(method, parameters, null, cancellationToken);
        var acknowledgement = new ValueTask(pendingResult);
        return acknowledgement;
    }

    /// <summary>Sends a parameterless RPC and waits for the device response when the successful result payload is not needed by the caller.</summary>
    private ValueTask SendRpcAckAsync(RpcMethods method, CancellationToken cancellationToken)
    {
        var pendingResult = SendRpcAsync<EmptyRpcParameters, IgnoredRpcResult>(method, null, null, cancellationToken);
        var acknowledgement = new ValueTask(pendingResult);
        return acknowledgement;
    }

    /// <summary>Deserializes an RPC result with source-generated metadata and reports missing serializer registrations as a configuration error.</summary>
    private static TResult? DeserializeRpcResult<TResult>(ReadOnlySpan<byte> resultPayload)
    {
        var typeInfo = ShellyJsonSerializerContext.Default.GetTypeInfo(typeof(TResult));
        if (typeInfo is JsonTypeInfo<TResult> resultTypeInfo) return JsonSerializer.Deserialize(resultPayload, resultTypeInfo);

        var resultTypeName = typeof(TResult).FullName;
        throw new InvalidOperationException($"Register '{resultTypeName}' with JsonSerializableAttribute on ShellyJsonSerializerContext.");
    }

    /// <summary>Converts an optional non-negative duration to the floating-point seconds representation used by Shelly RPC.</summary>
    private static float? ToSeconds(TimeSpan? value, string parameterName)
    {
        if (value is null) return null;
        if (value.Value < TimeSpan.Zero) throw new ArgumentOutOfRangeException(parameterName, "Duration cannot be negative.");

        var seconds = value.Value.TotalSeconds;
        if (seconds > float.MaxValue) throw new ArgumentOutOfRangeException(parameterName);

        return (float)seconds;
    }

    /// <summary>Rejects cover movement durations that are shorter than the minimum accepted by the RPC API.</summary>
    private static void ValidateCoverDuration(TimeSpan? duration, string parameterName)
    {
        if (duration is null) return;

        var isBelowMinimumDuration = duration.Value.TotalSeconds < 0.1;
        if (isBelowMinimumDuration) throw new ArgumentOutOfRangeException(parameterName, "Cover movement duration must be at least 0.1 seconds.");
    }

    /// <summary>Rejects percentage values outside the range accepted by the target RPC method.</summary>
    private static void ValidatePercent(byte? value, string parameterName, bool allowZero)
    {
        if (value is null) return;

        var minimumValue = allowZero ? (byte)0 : (byte)1;
        var isOutsideRange = value.Value < minimumValue || value.Value > 100;
        if (isOutsideRange) throw new ArgumentOutOfRangeException(parameterName, $"Value must be in {minimumValue}..100.");
    }

    /// <summary>Rejects relative percentage values outside Shelly's -100..100 domain.</summary>
    private static void ValidateSignedPercent(sbyte? value, string parameterName)
    {
        var isOutsideRange = value is < -100 or > 100;
        if (isOutsideRange) throw new ArgumentOutOfRangeException(parameterName, "Value must be in -100..100.");
    }

    /// <summary>Rejects state-change tags longer than the maximum accepted by Shelly RPC.</summary>
    private static void ValidateTag(string? tag)
    {
        var isTooLong = tag is { Length: > 20 };
        if (isTooLong) throw new ArgumentOutOfRangeException(nameof(tag), "Shelly RPC tags are limited to 20 characters.");
    }

    /// <summary>Sends one typed RPC with the active authentication state and retries once when a reusable nonce becomes stale.</summary>
    private async Task<ShellyRpcResult<TResult>> SendRpcAsync<TParams, TResult>(RpcMethods method, TParams? parameters, RpcResultParser<TResult>? resultParser, CancellationToken ct) where TParams : struct
    {
        var authenticationState = Volatile.Read(ref _authenticationState);
        var result = await SendRpcCoreAsync(method, parameters, authenticationState, resultParser, ct).ConfigureAwait(false);

        if (authenticationState?.UsesReusableNonce != true) return result;
        if (result.RpcError is not { Code: 401 } rpcError) return result;
        if (!TryReadAuthenticationChallenge(rpcError.Message, out var challenge, out _)) return result;
        if (challenge!.Stale != true) return result;

        var deviceInfo = DeviceInfo;
        if (deviceInfo is null) return new ShellyRpcResult<TResult>(ShellyRpcResultStatus.ConnectionLost, default, null, null);

        if (!TryRefreshAuthenticationState(deviceInfo, authenticationState, challenge, out var refreshedState, out var refreshError))
        {
            return new ShellyRpcResult<TResult>(ShellyRpcResultStatus.ClientError, default, null, refreshError ?? "Shelly returned an invalid stale authentication challenge.");
        }

        Interlocked.CompareExchange(ref _authenticationState, refreshedState, authenticationState);
        var currentAuthenticationState = Volatile.Read(ref _authenticationState);
        if (currentAuthenticationState is null) return new ShellyRpcResult<TResult>(ShellyRpcResultStatus.ConnectionLost, default, null, null);

        return await SendRpcCoreAsync(method, parameters, currentAuthenticationState, resultParser, ct).ConfigureAwait(false);
    }

    /// <summary>Sends one typed RPC with an explicitly selected authentication state so login and stale-nonce retry can control authentication.</summary>
    private async Task<ShellyRpcResult<TResult>> SendRpcCoreAsync<TParams, TResult>(RpcMethods method, TParams? parameters, AuthenticationState? authenticationState, RpcResultParser<TResult>? resultParser, CancellationToken ct) where TParams : struct
    {
        var outputWriter = _webSocketOutputQueue?.Writer;
        var isConnected = !_disconnectRequested && _webSocket?.State == WebSocketState.Open && outputWriter is not null;
        if (!isConnected) return new ShellyRpcResult<TResult>(ShellyRpcResultStatus.NotConnected, default, null, null);

        var pendingRequest = new PendingRequest<TResult>(resultParser);
        if (!TryReservePendingRequest(pendingRequest, out var requestId)) return new ShellyRpcResult<TResult>(ShellyRpcResultStatus.ClientError, default, null, "All 65536 numeric request ids are currently in use.");

        var authenticatedSendLockTaken = false;
        if (authenticationState?.UsesReusableNonce == true)
        {
            try
            {
                await _authenticatedSendSemaphore.WaitAsync(ct).ConfigureAwait(false);
                authenticatedSendLockTaken = true;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                TryRemovePendingRequest(requestId, pendingRequest);
                return new ShellyRpcResult<TResult>(ShellyRpcResultStatus.Canceled, default, null, null);
            }
        }

        EnqueueStatus enqueueStatus;
        try
        {
            try
            {
                var outgoingMessage = SerializeRpcRequest(requestId, method, parameters, authenticationState);
                enqueueStatus = await EnqueueOutgoingMessageAsync(outputWriter!, outgoingMessage, ct).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                TryRemovePendingRequest(requestId, pendingRequest);
                return new ShellyRpcResult<TResult>(ShellyRpcResultStatus.ClientError, default, null, exception.Message);
            }
        }
        finally
        {
            if (authenticatedSendLockTaken) _authenticatedSendSemaphore.Release();
        }

        if (enqueueStatus != EnqueueStatus.Queued)
        {
            TryRemovePendingRequest(requestId, pendingRequest);
            var status = enqueueStatus == EnqueueStatus.Canceled ? ShellyRpcResultStatus.Canceled : ShellyRpcResultStatus.ConnectionLost;
            return new ShellyRpcResult<TResult>(status, default, null, null);
        }

        return await WaitForPendingResponseAsync(requestId, pendingRequest, ct).ConfigureAwait(false);
    }

    /// <summary>Serializes one complete outbound RPC directly into pooled memory and appends authentication when the selected state requires it.</summary>
    private OutgoingMessage SerializeRpcRequest<TParams>(ushort requestId, RpcMethods method, TParams? parameters, AuthenticationState? authenticationState) where TParams : struct
    {

        using var bufferWriter = new PooledByteBufferWriter(_pool, InitialRpcRequestBufferSize);
        var request = new RpcRequest<TParams>(requestId, method, parameters);
        var requestTypeInfo = GetRpcRequestTypeInfo<TParams>();

        using (var jsonWriter = new Utf8JsonWriter(bufferWriter))
        {
            JsonSerializer.Serialize(jsonWriter, request, requestTypeInfo);
            jsonWriter.Flush();
        }

        if (authenticationState is not null) AppendAuthentication(bufferWriter, authenticationState);
        return bufferWriter.DetachMessage();
    }

    /// <summary>Returns source-generated metadata for a registered typed RPC request and fails clearly when a request type was omitted from the serializer context.</summary>
    private static JsonTypeInfo<RpcRequest<TParams>> GetRpcRequestTypeInfo<TParams>() where TParams : struct
    {
        var typeInfo = ShellyJsonSerializerContext.Default.GetTypeInfo(typeof(RpcRequest<TParams>));
        if (typeInfo is JsonTypeInfo<RpcRequest<TParams>> requestTypeInfo) return requestTypeInfo;

        var requestTypeName = typeof(RpcRequest<TParams>).FullName;
        throw new InvalidOperationException($"Register '{requestTypeName}' with JsonSerializableAttribute on ShellyJsonSerializerContext.");
    }

    /// <summary>Reserves an unused request identifier for a response that must remain correlated with this pending operation.</summary>
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

    /// <summary>Completes a pending operation while the reusable receive buffer still owns the full device response.</summary>
    private bool TryCompletePendingRequest(ushort requestId, Memory<byte> message)
    {
        if (!_pendingRequests.TryGetValue(requestId, out var pendingRequest)) return false;

        var requestEntry = new KeyValuePair<ushort, PendingRequest>(requestId, pendingRequest);
        if (!_pendingRequests.TryRemove(requestEntry)) return true;

        pendingRequest.CompleteResponse(message);
        return true;
    }

    /// <summary>Removes a pending operation only when the identifier still belongs to the same request instance.</summary>
    private bool TryRemovePendingRequest(ushort requestId, PendingRequest pendingRequest)
    {
        var requestEntry = new KeyValuePair<ushort, PendingRequest>(requestId, pendingRequest);
        return ((ICollection<KeyValuePair<ushort, PendingRequest>>)_pendingRequests).Remove(requestEntry);
    }

}
