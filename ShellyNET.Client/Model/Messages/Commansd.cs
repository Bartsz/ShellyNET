namespace ShellyNET.Client.Model.Messages;


/// <summary>Represents one typed Shelly RPC request while allowing source-generated serialization for each parameter shape.</summary>
public readonly struct RpcRequest<TParams> where TParams : struct
{
    /// <summary>Creates a request with the correlation identifier, method, and optional component parameters.</summary>
    public RpcRequest(ushort id, RpcMethods method, TParams? parameters)
    {
        Id = id;
        Method = method;
        Params = parameters;
    }

    /// <summary>Gets the identifier used to correlate the device response with this request.</summary>
    public ushort Id { get; }

    /// <summary>Gets the client source identifier required by Shelly RPC frames.</summary>
    public string Src => "_";

    /// <summary>Gets the Shelly RPC method invoked by this request.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter<RpcMethods>))]
    public RpcMethods Method { get; }

    /// <summary>Gets the method parameters, or null when the RPC method has no parameters.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TParams? Params { get; }
}

/// <summary>Provides a serializable parameter type for Shelly RPC methods that do not accept parameters.</summary>
public readonly struct EmptyRpcParameters
{
}

public readonly struct NullRpcResponse();

/// <summary>Provides the parameters required to disengage a circuit breaker through CB.Set.</summary>
/// <param name="Id">CB component id.</param>
/// <param name="Output">Required breaker output. The documented command only accepts false, which disengages the breaker lever.</param>
public readonly record struct CbSetParams(byte Id, bool Output);

/// <summary>Reports the circuit breaker state that existed before CB.Set changed it.</summary>
/// <param name="WasOn">Whether the breaker lever was engaged before the command executed.</param>
public readonly record struct CbSetResult(bool WasOn);

/// <summary>Provides the lower timestamp boundary used to retrieve circuit breaker activity.</summary>
/// <param name="Id">CB component id.</param>
/// <param name="After">Loads activity entries created after this UTC Unix timestamp, in whole seconds.</param>
public readonly record struct CbGetLogParams(byte Id, long After);

/// <summary>Describes one circuit breaker activity entry returned by CB.GetLog.</summary>
/// <param name="Ts">UTC Unix timestamp in whole seconds; if device time is not synchronized, Shelly reports device uptime instead.</param>
/// <param name="Output">Breaker lever state: true means engaged.</param>
/// <param name="Source">Origin of the activity, for example init, WS_in or HTTP-related input.</param>
public readonly record struct CbLogRecord(long Ts, bool Output, string Source);

/// <summary>Contains the recent circuit breaker activity returned by CB.GetLog.</summary>
/// <param name="Records">Up to the last 50 activity records returned by the device.</param>
public readonly record struct CbGetLogResult(CbLogRecord[] Records);

/// <summary>Provides optional state, brightness and color-temperature changes for CCT.Set.</summary>
/// <param name="Id">CCT component id.</param>
/// <param name="On">Optional output state.</param>
/// <param name="Brightness">Optional brightness percentage. Shelly uses the 0..100 percentage domain.</param>
/// <param name="Ct">Optional color temperature in Kelvin. The accepted range is device/configuration specific; configured ct_range is within 1000..10000 K.</param>
/// <param name="TransitionDuration">Optional transition duration in seconds.</param>
/// <param name="ToggleAfter">Optional flip-back timer in seconds.</param>
/// <param name="Offset">Optional relative brightness adjustment, -100..100. Must not be sent together with Brightness.</param>
/// <param name="Tag">Optional state-change origin tag, maximum 20 characters.</param>
public readonly record struct CctSetParams(byte Id, bool? On = null, byte? Brightness = null, ushort? Ct = null, float? TransitionDuration = null, float? ToggleAfter = null, sbyte? Offset = null, string? Tag = null);

/// <summary>Provides the component and optional origin tag used by CCT.Toggle.</summary>
/// <param name="Id">CCT component id.</param>
/// <param name="Tag">Optional state-change origin tag, maximum 20 characters.</param>
public readonly record struct CctToggleParams(byte Id, string? Tag = null);

/// <summary>Identifies the cover component that should run its calibration procedure.</summary>
/// <param name="Id">Cover component id.</param>
public readonly record struct CoverCalibrateParams(byte Id);

/// <summary>Provides optional movement constraints for opening a cover.</summary>
/// <param name="Id">Cover component id.</param>
/// <param name="Duration">Optional movement time in seconds. Documented range starts at 0.1 s and is capped by maxtime_open.</param>
/// <param name="Tag">Optional state-change origin tag, maximum 20 characters.</param>
public readonly record struct CoverOpenParams(byte Id, float? Duration = null, string? Tag = null);

/// <summary>Provides optional movement constraints for closing a cover.</summary>
/// <param name="Id">Cover component id.</param>
/// <param name="Duration">Optional movement time in seconds. Documentation says the movement is bounded by maxtime_close; its range text currently names maxtime_open.</param>
/// <param name="Tag">Optional state-change origin tag, maximum 20 characters.</param>
public readonly record struct CoverCloseParams(byte Id, float? Duration = null, string? Tag = null);

/// <summary>Identifies the cover movement that should be stopped and optionally tags the state change.</summary>
/// <param name="Id">Cover component id.</param>
/// <param name="Tag">Optional state-change origin tag, maximum 20 characters.</param>
public readonly record struct CoverStopParams(byte Id, string? Tag = null);

/// <summary>Provides absolute or relative cover and slat targets for Cover.GoToPosition.</summary>
/// <param name="Id">Cover component id.</param>
/// <param name="Pos">Absolute cover position in percent, 0..100. Mutually exclusive with Rel.</param>
/// <param name="Rel">Relative cover movement in percent, -100..100. Mutually exclusive with Pos; overshoot is capped by Shelly.</param>
/// <param name="SlatPos">Optional absolute slat position, 0..100, on devices supporting slat control.</param>
/// <param name="SlatRel">Optional relative slat movement, -100..100, on devices supporting slat control.</param>
/// <param name="Tag">Optional state-change origin tag, maximum 20 characters.</param>
public readonly record struct CoverGoToPositionParams(byte Id, byte? Pos = null, sbyte? Rel = null, byte? SlatPos = null, sbyte? SlatRel = null, string? Tag = null);

/// <summary>Provides optional state and brightness changes for Light.Set.</summary>
/// <param name="Id">Light component id.</param>
/// <param name="On">Optional light state.</param>
/// <param name="Brightness">Optional brightness percentage, 0..100.</param>
/// <param name="TransitionDuration">Optional transition duration in seconds.</param>
/// <param name="ToggleAfter">Optional flip-back timer in seconds.</param>
/// <param name="Offset">Optional relative brightness adjustment, -100..100. Must not be sent together with Brightness.</param>
/// <param name="Tag">Optional state-change origin tag, maximum 20 characters.</param>
public readonly record struct LightSetParams(byte Id, bool? On = null, byte? Brightness = null, float? TransitionDuration = null, float? ToggleAfter = null, sbyte? Offset = null, string? Tag = null);

/// <summary>Provides the component and optional origin tag used by Light.Toggle.</summary>
/// <param name="Id">Light component id.</param>
/// <param name="Tag">Optional state-change origin tag, maximum 20 characters.</param>
public readonly record struct LightToggleParams(byte Id, string? Tag = null);

/// <summary>Identifies the light component whose output calibration should be started.</summary>
/// <param name="Id">Light component id. On PlusRGBWPM one call calibrates all light instances and this id is ignored.</param>
public readonly record struct LightCalibrateParams(byte Id);

/// <summary>Provides optional state, brightness and RGB changes for RGB.Set.</summary>
/// <param name="Id">RGB component id.</param>
/// <param name="On">Optional output state.</param>
/// <param name="Brightness">Optional brightness percentage, documented as 1..100 for RGB.Set.</param>
/// <param name="Rgb">Optional RGB channels; each channel is 0..255.</param>
/// <param name="TransitionDuration">Optional transition duration in seconds.</param>
/// <param name="ToggleAfter">Optional flip-back timer in seconds.</param>
/// <param name="Offset">Optional relative brightness adjustment, -100..100. Must not be sent together with Brightness.</param>
/// <param name="Tag">Optional state-change origin tag, maximum 20 characters.</param>
public readonly record struct RgbSetParams(byte Id, bool? On = null, byte? Brightness = null, Rgb24? Rgb = null, float? TransitionDuration = null, float? ToggleAfter = null, sbyte? Offset = null, string? Tag = null);

/// <summary>Provides the component and optional origin tag used by RGB.Toggle.</summary>
/// <param name="Id">RGB component id.</param>
/// <param name="Tag">Optional state-change origin tag, maximum 20 characters.</param>
public readonly record struct RgbToggleParams(byte Id, string? Tag = null);

/// <summary>Provides optional state, brightness and color-model changes for RGBCCT.Set.</summary>
/// <param name="Id">RGBCCT component id.</param>
/// <param name="On">Optional output state.</param>
/// <param name="Brightness">Optional brightness percentage, documented as 1..100 for RGBCCT.Set.</param>
/// <param name="Rgb">Optional RGB channels; each channel is 0..255 and is meaningful in rgb mode.</param>
/// <param name="Ct">Optional color temperature in Kelvin and meaningful in cct mode.</param>
/// <param name="Mode">Optional operating mode: rgb or cct.</param>
/// <param name="TransitionDuration">Optional transition duration in seconds.</param>
/// <param name="ToggleAfter">Optional flip-back timer in seconds.</param>
/// <param name="Offset">Optional relative brightness adjustment, -100..100. Must not be sent together with Brightness.</param>
/// <param name="Tag">Optional state-change origin tag, maximum 20 characters.</param>
public readonly record struct RgbCctSetParams(byte Id, bool? On = null, byte? Brightness = null, Rgb24? Rgb = null, ushort? Ct = null, RgbCctMode? Mode = null, float? TransitionDuration = null, float? ToggleAfter = null, sbyte? Offset = null, string? Tag = null);

/// <summary>Provides the component and optional origin tag used by RGBCCT.Toggle.</summary>
/// <param name="Id">RGBCCT component id.</param>
/// <param name="Tag">Optional state-change origin tag, maximum 20 characters.</param>
public readonly record struct RgbCctToggleParams(byte Id, string? Tag = null);

/// <summary>Provides optional state, brightness, RGB and white-channel changes for RGBW.Set.</summary>
/// <param name="Id">RGBW component id.</param>
/// <param name="On">Optional output state.</param>
/// <param name="Brightness">Optional brightness percentage, documented as 1..100 for RGBW.Set.</param>
/// <param name="Rgb">Optional RGB channels; each channel is 0..255.</param>
/// <param name="White">Optional white-channel level, 0..255.</param>
/// <param name="TransitionDuration">Optional transition duration in seconds.</param>
/// <param name="ToggleAfter">Optional flip-back timer in seconds.</param>
/// <param name="Offset">Optional relative brightness adjustment, -100..100. Must not be sent together with Brightness.</param>
/// <param name="OffsetWhite">Optional relative white-channel adjustment, -255..255. Must not be sent together with White.</param>
/// <param name="Tag">Optional state-change origin tag, maximum 20 characters.</param>
public readonly record struct RgbWSetParams(byte Id, bool? On = null, byte? Brightness = null, Rgb24? Rgb = null, byte? White = null, float? TransitionDuration = null, float? ToggleAfter = null, sbyte? Offset = null, short? OffsetWhite = null, string? Tag = null);

/// <summary>Provides the component and optional origin tag used by RGBW.Toggle.</summary>
/// <param name="Id">RGBW component id.</param>
/// <param name="Tag">Optional state-change origin tag, maximum 20 characters.</param>
public readonly record struct RgbWToggleParams(byte Id, string? Tag = null);

/// <summary>Provides an optional delay before Shelly.Reboot restarts the device.</summary>
/// <param name="DelayMs">Optional delay before reboot in milliseconds. Default is 1000 ms; values below 500 ms are capped by Shelly to 500 ms.</param>
public readonly record struct ShellyRebootParams(long? DelayMs = null);

/// <summary>Provides the requested output state and optional delayed toggle for Switch.Set.</summary>
/// <param name="Id">Switch component id.</param>
/// <param name="On">Required output state.</param>
/// <param name="ToggleAfter">Optional flip-back timer in seconds.</param>
/// <param name="Tag">Optional state-change origin tag, maximum 20 characters.</param>
public readonly record struct SwitchSetParams(byte Id, bool On, float? ToggleAfter = null, string? Tag = null);

/// <summary>Reports the switch state that existed before Switch.Set changed it.</summary>
/// <param name="WasOn">Whether the switch was on before the command executed.</param>
public readonly record struct SwitchSetResult(bool WasOn);

/// <summary>Provides the component and optional origin tag used by Switch.Toggle.</summary>
/// <param name="Id">Switch component id.</param>
/// <param name="Tag">Optional state-change origin tag, maximum 20 characters.</param>
public readonly record struct SwitchToggleParams(byte Id, string? Tag = null);

/// <summary>Reports the switch state that existed before Switch.Toggle changed it.</summary>
/// <param name="WasOn">Whether the switch was on before the command executed.</param>
public readonly record struct SwitchToggleResult(bool WasOn);

/// <summary>Identifies the smoke component whose associated alarm should be muted.</summary>
/// <param name="Id">Smoke component id whose associated smoke alarm should be muted.</param>
public readonly record struct SmokeMuteParams(byte Id);

/// <summary>Provides the UTC Unix timestamp used by Sys.SetTime.</summary>
/// <param name="UnixTime">UTC Unix timestamp in whole seconds.</param>
public readonly record struct SysSetTimeParams([property: JsonPropertyName("unixtime")] long UnixTime);

/// <summary>Reports the circuit breaker state that existed before CB.Set changed it.</summary>
/// <param name="WasOn">Whether the breaker lever was engaged before the command executed.</param>
public readonly record struct DeviceUpdateResult(FirmwareVersionInfo Stable);

public readonly record struct FirmwareVersionInfo(string Version, string BuildId);