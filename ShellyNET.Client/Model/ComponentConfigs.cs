namespace ShellyNET.Client.Model;

// Generated from the current Shelly Gen2+ component documentation for Shelly.GetConfig payloads.
// Streamer, HTTP, KVS, Schedule, Webhook and XMOD are services without a GetConfig payload.
// Shelly.GetConfig is the aggregate service itself, so its component keys are dynamic.
// DALI exposes GetConfig in the namespace but explicitly provides no configuration payload.
// DevicePower.GetConfig returns JSON null. EMData, EM1Data and MbRtuClient return empty JSON objects.

/// <summary>Selects the camera motion-detection sensitivity preset.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<CameraMotionSensitivity>))]
public enum CameraMotionSensitivity
{
    /// <summary>Reduces motion sensitivity to limit incidental detections.</summary>
    [JsonStringEnumMemberName("low")]
    Low,
    /// <summary>Balances sensitivity and resistance to incidental detections.</summary>
    [JsonStringEnumMemberName("medium")]
    Medium,
    /// <summary>Maximizes sensitivity to detect smaller or weaker motion.</summary>
    [JsonStringEnumMemberName("high")]
    High
}

/// <summary>Selects how the camera changes between daylight and infrared imaging.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<CameraNightVisionMode>))]
public enum CameraNightVisionMode
{
    /// <summary>Uses ambient-light thresholds to switch day and night modes automatically.</summary>
    [JsonStringEnumMemberName("auto")]
    Auto,
    /// <summary>Forces daylight imaging.</summary>
    [JsonStringEnumMemberName("day")]
    Day,
    /// <summary>Forces infrared night imaging.</summary>
    [JsonStringEnumMemberName("night")]
    Night
}

/// <summary>Selects mains-frequency compensation used to reduce video banding under artificial lighting.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<CameraAntiflickerMode>))]
public enum CameraAntiflickerMode
{
    /// <summary>Uses anti-flicker timing for 50 Hz mains.</summary>
    [JsonStringEnumMemberName("50Hz")]
    Hz50,
    /// <summary>Uses anti-flicker timing for 60 Hz mains.</summary>
    [JsonStringEnumMemberName("60Hz")]
    Hz60
}

/// <summary>Selects the behavior assigned to a configured camera polygon.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<CameraZoneType>))]
public enum CameraZoneType
{
    /// <summary>Uses the polygon as a motion-detection zone.</summary>
    [JsonStringEnumMemberName("motion")]
    Motion,
    /// <summary>Uses the polygon as a privacy-masking zone.</summary>
    [JsonStringEnumMemberName("privacy")]
    Privacy
}

/// <summary>Selects the CCT output state applied after power-up.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<CCTInitialState>))]
public enum CCTInitialState
{
    /// <summary>Starts with the output off.</summary>
    [JsonStringEnumMemberName("off")]
    Off,
    /// <summary>Starts with the output on.</summary>
    [JsonStringEnumMemberName("on")]
    On,
    /// <summary>Restores the last persisted output state.</summary>
    [JsonStringEnumMemberName("restore_last")]
    RestoreLast
}

/// <summary>Selects how physical inputs control the cover.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<CoverInputMode>))]
public enum CoverInputMode
{
    /// <summary>Uses single-input cover control.</summary>
    [JsonStringEnumMemberName("single")]
    Single,
    /// <summary>Uses separate inputs for the two movement directions.</summary>
    [JsonStringEnumMemberName("dual")]
    Dual,
    /// <summary>Keeps physical inputs detached from cover movement.</summary>
    [JsonStringEnumMemberName("detached")]
    Detached
}

/// <summary>Selects the cover action/state applied after power-up.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<CoverInitialState>))]
public enum CoverInitialState
{
    /// <summary>Moves or initializes the cover toward the open state.</summary>
    [JsonStringEnumMemberName("open")]
    Open,
    /// <summary>Moves or initializes the cover toward the closed state.</summary>
    [JsonStringEnumMemberName("closed")]
    Closed,
    /// <summary>Leaves the cover stopped after power-up.</summary>
    [JsonStringEnumMemberName("stopped")]
    Stopped
}

/// <summary>Selects movement directions in which obstruction detection is active.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<CoverObstructionDirection>))]
public enum CoverObstructionDirection
{
    /// <summary>Monitors obstruction while opening.</summary>
    [JsonStringEnumMemberName("open")]
    Open,
    /// <summary>Monitors obstruction while closing.</summary>
    [JsonStringEnumMemberName("close")]
    Close,
    /// <summary>Monitors obstruction in both movement directions.</summary>
    [JsonStringEnumMemberName("both")]
    Both
}

/// <summary>Selects what the cover does after detecting an obstruction.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<CoverObstructionAction>))]
public enum CoverObstructionAction
{
    /// <summary>Stops movement when an obstruction is detected.</summary>
    [JsonStringEnumMemberName("stop")]
    Stop,
    /// <summary>Reverses movement when an obstruction is detected.</summary>
    [JsonStringEnumMemberName("reverse")]
    Reverse
}

/// <summary>Selects movement directions affected by the safety switch.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<CoverSafetySwitchDirection>))]
public enum CoverSafetySwitchDirection
{
    /// <summary>Applies the safety switch while opening.</summary>
    [JsonStringEnumMemberName("open")]
    Open,
    /// <summary>Applies the safety switch while closing.</summary>
    [JsonStringEnumMemberName("close")]
    Close,
    /// <summary>Applies the safety switch in both directions.</summary>
    [JsonStringEnumMemberName("both")]
    Both
}

/// <summary>Selects what the cover does when the safety switch becomes active.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<CoverSafetySwitchAction>))]
public enum CoverSafetySwitchAction
{
    /// <summary>Stops movement.</summary>
    [JsonStringEnumMemberName("stop")]
    Stop,
    /// <summary>Reverses movement.</summary>
    [JsonStringEnumMemberName("reverse")]
    Reverse,
    /// <summary>Pauses movement while the safety condition is active.</summary>
    [JsonStringEnumMemberName("pause")]
    Pause
}

/// <summary>Selects the movement explicitly permitted while the safety switch is active.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<CoverSafetySwitchAllowedMove>))]
public enum CoverSafetySwitchAllowedMove
{
    /// <summary>Allows movement only in the reverse direction.</summary>
    [JsonStringEnumMemberName("reverse")]
    Reverse
}

/// <summary>Selects the energy quantity used to drive the meter LED pulse indication.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<EMBlinkModeSelector>))]
public enum EMBlinkModeSelector
{
    /// <summary>Uses active energy for LED pulse indication.</summary>
    [JsonStringEnumMemberName("active_energy")]
    ActiveEnergy,
    /// <summary>Uses apparent energy for LED pulse indication.</summary>
    [JsonStringEnumMemberName("apparent_energy")]
    ApparentEnergy
}

/// <summary>Selects the phase used for component-level measurements or indications.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<EMPhaseSelector>))]
public enum EMPhaseSelector
{
    /// <summary>Selects phase A.</summary>
    [JsonStringEnumMemberName("a")]
    A,
    /// <summary>Selects phase B.</summary>
    [JsonStringEnumMemberName("b")]
    B,
    /// <summary>Selects phase C.</summary>
    [JsonStringEnumMemberName("c")]
    C,
    /// <summary>Uses all phases.</summary>
    [JsonStringEnumMemberName("all")]
    All
}

/// <summary>Selects how the Ethernet interface obtains its IPv4 configuration in client mode.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<EthIPv4Mode>))]
public enum EthIPv4Mode
{
    /// <summary>Obtains IPv4 settings from DHCP.</summary>
    [JsonStringEnumMemberName("dhcp")]
    Dhcp,
    /// <summary>Uses explicitly configured IPv4 settings.</summary>
    [JsonStringEnumMemberName("static")]
    Static
}

/// <summary>Selects how the flood sensor announces a detected water condition.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<FloodAlarmMode>))]
public enum FloodAlarmMode
{
    /// <summary>Suppresses the local alarm.</summary>
    [JsonStringEnumMemberName("disabled")]
    Disabled,
    /// <summary>Uses the normal alarm behavior.</summary>
    [JsonStringEnumMemberName("normal")]
    Normal,
    /// <summary>Uses the more intense alarm behavior.</summary>
    [JsonStringEnumMemberName("intense")]
    Intense,
    /// <summary>Uses the rain-oriented alarm behavior.</summary>
    [JsonStringEnumMemberName("rain")]
    Rain
}

/// <summary>Selects how an input signal is interpreted and which related settings are applicable.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<InputType>))]
public enum InputType
{
    /// <summary>Treats the input as a maintained switch.</summary>
    [JsonStringEnumMemberName("switch")]
    Switch,
    /// <summary>Treats the input as a stateless button.</summary>
    [JsonStringEnumMemberName("button")]
    Button,
    /// <summary>Treats the input as an analog value.</summary>
    [JsonStringEnumMemberName("analog")]
    Analog,
    /// <summary>Treats the input as a pulse counter and frequency source.</summary>
    [JsonStringEnumMemberName("count")]
    Count
}

/// <summary>Selects how the associated input controls a dimmable light.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<LightInputMode>))]
public enum LightInputMode
{
    /// <summary>Makes output state follow the input state.</summary>
    [JsonStringEnumMemberName("follow")]
    Follow,
    /// <summary>Toggles output whenever the input changes.</summary>
    [JsonStringEnumMemberName("flip")]
    Flip,
    /// <summary>Turns the output on when the input activates.</summary>
    [JsonStringEnumMemberName("activate")]
    Activate,
    /// <summary>Detaches input state from the output.</summary>
    [JsonStringEnumMemberName("detached")]
    Detached,
    /// <summary>Uses button presses for toggle and hold-to-dim control.</summary>
    [JsonStringEnumMemberName("dim")]
    Dim,
    /// <summary>Uses dual-input dimming where supported.</summary>
    [JsonStringEnumMemberName("dual_dim")]
    DualDim
}

/// <summary>Selects the light state applied after power-up.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<LightInitialState>))]
public enum LightInitialState
{
    /// <summary>Starts with the light off.</summary>
    [JsonStringEnumMemberName("off")]
    Off,
    /// <summary>Starts with the light on.</summary>
    [JsonStringEnumMemberName("on")]
    On,
    /// <summary>Restores the last persisted output state.</summary>
    [JsonStringEnumMemberName("restore_last")]
    RestoreLast,
    /// <summary>Applies the toggle startup behavior supported by the device.</summary>
    [JsonStringEnumMemberName("toggle")]
    Toggle
}

/// <summary>Selects TLS certificate validation for the MQTT connection.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MQTTSslCa>))]
public enum MQTTSslCa
{
    /// <summary>Uses TLS without validating the server certificate.</summary>
    [JsonStringEnumMemberName("*")]
    DisableValidation,
    /// <summary>Validates the server with the user-provided CA.</summary>
    [JsonStringEnumMemberName("user_ca.pem")]
    UserCa,
    /// <summary>Validates the server with the built-in CA bundle.</summary>
    [JsonStringEnumMemberName("ca.pem")]
    BuiltInCa
}

/// <summary>Selects which peripheral function the Pill exposes and therefore which dynamic components are created.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<PillMode>))]
public enum PillMode
{
    /// <summary>Enables OneWire temperature-sensor operation.</summary>
    [JsonStringEnumMemberName("onewire")]
    OneWire,
    /// <summary>Enables DHT22 temperature and humidity operation.</summary>
    [JsonStringEnumMemberName("dht22")]
    Dht22,
    /// <summary>Enables analog input operation.</summary>
    [JsonStringEnumMemberName("analog_in")]
    AnalogIn,
    /// <summary>Enables the solid-state relay peripheral mode.</summary>
    [JsonStringEnumMemberName("ssr")]
    Ssr,
    /// <summary>Enables configurable digital I/O pins.</summary>
    [JsonStringEnumMemberName("digital_io")]
    DigitalIo,
    /// <summary>Enables the serial port and related Serial component.</summary>
    [JsonStringEnumMemberName("serial")]
    Serial
}

/// <summary>Selects the function assigned to Pill pin 0.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<PillPin0Mode>))]
public enum PillPin0Mode
{
    /// <summary>Leaves the pin unused.</summary>
    [JsonStringEnumMemberName("none")]
    None,
    /// <summary>Reserves the pin for the selected peripheral function.</summary>
    [JsonStringEnumMemberName("reserved")]
    Reserved,
    /// <summary>Configures the pin as a digital input.</summary>
    [JsonStringEnumMemberName("digital_in")]
    DigitalIn,
    /// <summary>Configures the pin as a digital output.</summary>
    [JsonStringEnumMemberName("digital_out")]
    DigitalOut
}

/// <summary>Selects the function assigned to Pill pin 1.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<PillPin1Mode>))]
public enum PillPin1Mode
{
    /// <summary>Leaves the pin unused.</summary>
    [JsonStringEnumMemberName("none")]
    None,
    /// <summary>Reserves the pin for the selected peripheral function.</summary>
    [JsonStringEnumMemberName("reserved")]
    Reserved,
    /// <summary>Configures the pin as a digital input.</summary>
    [JsonStringEnumMemberName("digital_in")]
    DigitalIn,
    /// <summary>Configures the pin as a digital output.</summary>
    [JsonStringEnumMemberName("digital_out")]
    DigitalOut
}

/// <summary>Selects the function assigned to Pill pin 2.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<PillPin2Mode>))]
public enum PillPin2Mode
{
    /// <summary>Leaves the pin unused.</summary>
    [JsonStringEnumMemberName("none")]
    None,
    /// <summary>Reserves the pin for the selected peripheral function.</summary>
    [JsonStringEnumMemberName("reserved")]
    Reserved,
    /// <summary>Configures the pin as a digital input.</summary>
    [JsonStringEnumMemberName("digital_in")]
    DigitalIn,
    /// <summary>Configures the pin as a digital output.</summary>
    [JsonStringEnumMemberName("digital_out")]
    DigitalOut
}

/// <summary>Selects the sensor mounting position used by the presence-tracking algorithm.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<PresenceSensorPosition>))]
public enum PresenceSensorPosition
{
    /// <summary>Optimizes tracking for a sensor mounted at the left side of the room.</summary>
    [JsonStringEnumMemberName("left")]
    Left,
    /// <summary>Optimizes tracking for a centrally mounted sensor.</summary>
    [JsonStringEnumMemberName("center")]
    Center,
    /// <summary>Optimizes tracking for a sensor mounted at the right side of the room.</summary>
    [JsonStringEnumMemberName("right")]
    Right
}

/// <summary>Selects the radar power preset used for presence tracking.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<PresenceSensorPower>))]
public enum PresenceSensorPower
{
    /// <summary>Uses the low-power radar preset.</summary>
    [JsonStringEnumMemberName("low")]
    Low,
    /// <summary>Uses the medium-power radar preset.</summary>
    [JsonStringEnumMemberName("medium")]
    Medium,
    /// <summary>Uses the high-power radar preset.</summary>
    [JsonStringEnumMemberName("high")]
    High
}

/// <summary>Selects the bundled sensitivity preset for the presence algorithm.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<PresenceSensorSensitivity>))]
public enum PresenceSensorSensitivity
{
    /// <summary>Uses lower detection sensitivity.</summary>
    [JsonStringEnumMemberName("low")]
    Low,
    /// <summary>Uses balanced detection sensitivity.</summary>
    [JsonStringEnumMemberName("medium")]
    Medium,
    /// <summary>Uses higher detection sensitivity.</summary>
    [JsonStringEnumMemberName("high")]
    High
}

/// <summary>Selects how the associated input controls the RGB output.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<RGBInputMode>))]
public enum RGBInputMode
{
    /// <summary>Makes output state follow the input.</summary>
    [JsonStringEnumMemberName("follow")]
    Follow,
    /// <summary>Toggles output when the input changes.</summary>
    [JsonStringEnumMemberName("flip")]
    Flip,
    /// <summary>Turns the output on when the input activates.</summary>
    [JsonStringEnumMemberName("activate")]
    Activate,
    /// <summary>Detaches input from output state.</summary>
    [JsonStringEnumMemberName("detached")]
    Detached,
    /// <summary>Uses button press and hold for dimming.</summary>
    [JsonStringEnumMemberName("dim")]
    Dim
}

/// <summary>Selects the RGB output state applied after power-up.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<RGBInitialState>))]
public enum RGBInitialState
{
    /// <summary>Starts with the output off.</summary>
    [JsonStringEnumMemberName("off")]
    Off,
    /// <summary>Starts with the output on.</summary>
    [JsonStringEnumMemberName("on")]
    On,
    /// <summary>Restores the last persisted output state.</summary>
    [JsonStringEnumMemberName("restore_last")]
    RestoreLast
}

/// <summary>Selects the RGBCCT output state applied after power-up.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<RGBCCTInitialState>))]
public enum RGBCCTInitialState
{
    /// <summary>Starts with the output off.</summary>
    [JsonStringEnumMemberName("off")]
    Off,
    /// <summary>Starts with the output on.</summary>
    [JsonStringEnumMemberName("on")]
    On,
    /// <summary>Restores the last persisted output state.</summary>
    [JsonStringEnumMemberName("restore_last")]
    RestoreLast
}

/// <summary>Selects which color model is used while RGBCCT night mode is active.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<RGBCCTNightModeMode>))]
public enum RGBCCTNightModeMode
{
    /// <summary>Uses RGB color control during night mode.</summary>
    [JsonStringEnumMemberName("rgb")]
    Rgb,
    /// <summary>Uses correlated-color-temperature control during night mode.</summary>
    [JsonStringEnumMemberName("cct")]
    Cct
}

/// <summary>Selects how the associated input controls the RGBW output.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<RGBWInputMode>))]
public enum RGBWInputMode
{
    /// <summary>Makes output state follow the input.</summary>
    [JsonStringEnumMemberName("follow")]
    Follow,
    /// <summary>Toggles output when the input changes.</summary>
    [JsonStringEnumMemberName("flip")]
    Flip,
    /// <summary>Turns the output on when the input activates.</summary>
    [JsonStringEnumMemberName("activate")]
    Activate,
    /// <summary>Detaches input from output state.</summary>
    [JsonStringEnumMemberName("detached")]
    Detached,
    /// <summary>Uses button press and hold for dimming.</summary>
    [JsonStringEnumMemberName("dim")]
    Dim
}

/// <summary>Selects the RGBW output state applied after power-up.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<RGBWInitialState>))]
public enum RGBWInitialState
{
    /// <summary>Starts with the output off.</summary>
    [JsonStringEnumMemberName("off")]
    Off,
    /// <summary>Starts with the output on.</summary>
    [JsonStringEnumMemberName("on")]
    On,
    /// <summary>Restores the last persisted output state.</summary>
    [JsonStringEnumMemberName("restore_last")]
    RestoreLast
}

/// <summary>Selects which subsystem owns and uses the serial port.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<SerialMode>))]
public enum SerialMode
{
    /// <summary>Keeps the serial port inactive.</summary>
    [JsonStringEnumMemberName("disabled")]
    Disabled,
    /// <summary>Exposes the port to scripts through the UART API.</summary>
    [JsonStringEnumMemberName("jsuart")]
    JsUart,
    /// <summary>Runs a Modbus RTU server on the port.</summary>
    [JsonStringEnumMemberName("mb_server")]
    ModbusServer,
    /// <summary>Runs a Modbus RTU client and exposes an MbRtuClient component.</summary>
    [JsonStringEnumMemberName("mb_client")]
    ModbusClient
}

/// <summary>Selects how the associated physical input controls the switch output.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<SwitchInputMode>))]
public enum SwitchInputMode
{
    /// <summary>Keeps the physical input detached from the relay output.</summary>
    [JsonStringEnumMemberName("detached")]
    Detached,
    /// <summary>Makes relay state follow input state.</summary>
    [JsonStringEnumMemberName("follow")]
    Follow,
    /// <summary>Toggles relay state whenever input state changes.</summary>
    [JsonStringEnumMemberName("flip")]
    Flip,
    /// <summary>Turns the relay on when the input activates.</summary>
    [JsonStringEnumMemberName("activate")]
    Activate,
    /// <summary>Uses stateless button presses to control the relay.</summary>
    [JsonStringEnumMemberName("momentary")]
    Momentary,
    /// <summary>Cycles the output using supported stateless-input behavior.</summary>
    [JsonStringEnumMemberName("cycle")]
    Cycle
}

/// <summary>Selects the switch output state applied after power-up.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<SwitchInitialState>))]
public enum SwitchInitialState
{
    /// <summary>Starts with the relay on.</summary>
    [JsonStringEnumMemberName("on")]
    On,
    /// <summary>Starts with the relay off.</summary>
    [JsonStringEnumMemberName("off")]
    Off,
    /// <summary>Restores the last persisted relay state.</summary>
    [JsonStringEnumMemberName("restore_last")]
    RestoreLast,
    /// <summary>Initializes the relay to match the associated input state.</summary>
    [JsonStringEnumMemberName("match_input")]
    MatchInput
}

/// <summary>Selects the optional hardware add-on enabled by the system configuration.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<SysAddonType>))]
public enum SysAddonType
{
    /// <summary>Enables a supported sensor add-on.</summary>
    [JsonStringEnumMemberName("sensor")]
    Sensor,
    /// <summary>Enables the Pro Output add-on.</summary>
    [JsonStringEnumMemberName("prooutput")]
    ProOutput,
    /// <summary>Enables the LoRa add-on.</summary>
    [JsonStringEnumMemberName("LoRa")]
    LoRa,
    /// <summary>Enables the RS485 add-on.</summary>
    [JsonStringEnumMemberName("RS485")]
    Rs485
}

/// <summary>Selects how a Wi-Fi station obtains its IPv4 configuration.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<WifiStationIPv4Mode>))]
public enum WifiStationIPv4Mode
{
    /// <summary>Obtains IPv4 settings from DHCP.</summary>
    [JsonStringEnumMemberName("dhcp")]
    Dhcp,
    /// <summary>Uses explicitly configured IPv4 settings.</summary>
    [JsonStringEnumMemberName("static")]
    Static
}

/// <summary>Selects TLS certificate validation for the outbound WebSocket connection.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<WsSslCa>))]
public enum WsSslCa
{
    /// <summary>Uses TLS without validating the server certificate.</summary>
    [JsonStringEnumMemberName("*")]
    DisableValidation,
    /// <summary>Validates the server with the user-provided CA.</summary>
    [JsonStringEnumMemberName("user_ca.pem")]
    UserCa,
    /// <summary>Validates the server with the built-in CA bundle.</summary>
    [JsonStringEnumMemberName("ca.pem")]
    BuiltInCa
}

/// <summary>Represents the aggregate object returned by Shelly.GetConfig; dynamic component keys such as switch:0 are retained verbatim.</summary>
public sealed record class ShellyGetConfigResponse
{
    /// <summary>Preserves every component configuration under the exact component key returned by the device.</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement> Components { get; init; } = [];

}

public interface IShellyComponentConfig
{
    static abstract string ComponentName { get; }
}


public abstract record class ShellyComponentConfigBase;


/// <summary>Controls whether the camera indicates activity with its LED.</summary>
public sealed record class CameraLedConfig
{
    /// <summary>Determines whether the camera activity LED is enabled.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

}

/// <summary>Controls microphone capture for camera streams and recordings.</summary>
public sealed record class CameraAudioInputConfig
{
    /// <summary>Determines whether microphone audio is captured.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

}

/// <summary>Controls speaker output used for camera sounds and playback.</summary>
public sealed record class CameraAudioOutputConfig
{
    /// <summary>Sets speaker volume as a percentage from 0 to 100.</summary>
    [JsonPropertyName("volume")]
    public double Volume { get; init; }

}

/// <summary>Groups microphone and speaker settings used by the camera.</summary>
public sealed record class CameraAudioConfig
{
    /// <summary>Controls microphone capture.</summary>
    [JsonPropertyName("input")]
    public CameraAudioInputConfig? Input { get; init; }

    /// <summary>Controls speaker playback.</summary>
    [JsonPropertyName("output")]
    public CameraAudioOutputConfig? Output { get; init; }

}

/// <summary>Controls whether built-in and event sounds may be played.</summary>
public sealed record class CameraSoundsConfig
{
    /// <summary>Determines whether sound playback is permitted.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

}

/// <summary>Controls automatic clip recording when motion is detected.</summary>
public sealed record class CameraMotionRecordingConfig
{
    /// <summary>Determines whether detected motion starts a recording clip.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

}

/// <summary>Controls motion detection behavior and its recording response.</summary>
public sealed record class CameraMotionConfig
{
    /// <summary>Sets the sensitivity preset used by motion detection.</summary>
    [JsonPropertyName("sensitivity")]
    public CameraMotionSensitivity Sensitivity { get; init; }

    /// <summary>Controls motion-triggered recording.</summary>
    [JsonPropertyName("recording")]
    public CameraMotionRecordingConfig? Recording { get; init; }

}

/// <summary>Controls infrared night imaging and automatic day/night switching.</summary>
public sealed record class CameraNightVisionConfig
{
    /// <summary>Selects automatic, forced-day, or forced-night operation.</summary>
    [JsonPropertyName("mode")]
    public CameraNightVisionMode Mode { get; init; }

    /// <summary>Determines whether infrared LEDs illuminate the scene in night mode.</summary>
    [JsonPropertyName("ir_leds")]
    public bool IrLeds { get; init; }

    /// <summary>Sets the ambient-light switching threshold from 0 to 100.</summary>
    [JsonPropertyName("light_threshold")]
    public double LightThreshold { get; init; }

    /// <summary>Sets switching sensitivity from 0 to 100 to balance responsiveness and stability.</summary>
    [JsonPropertyName("sensitivity")]
    public double Sensitivity { get; init; }

}

/// <summary>Controls the camera RTSP server used by local streaming clients.</summary>
public sealed record class CameraRtspConfig
{
    /// <summary>Determines whether the RTSP server is available.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

}

/// <summary>Controls image processing applied to camera video.</summary>
public sealed record class CameraVideoConfig
{
    /// <summary>Adjusts image brightness from 0 to 100.</summary>
    [JsonPropertyName("brightness")]
    public double Brightness { get; init; }

    /// <summary>Adjusts image contrast from 0 to 100.</summary>
    [JsonPropertyName("contrast")]
    public double Contrast { get; init; }

    /// <summary>Adjusts color saturation from 0 to 100.</summary>
    [JsonPropertyName("saturation")]
    public double Saturation { get; init; }

    /// <summary>Adjusts image sharpness from 0 to 100.</summary>
    [JsonPropertyName("sharpness")]
    public double Sharpness { get; init; }

    /// <summary>Adjusts tint within the supported -256 to 256 range.</summary>
    [JsonPropertyName("tint")]
    public double Tint { get; init; }

    /// <summary>Adjusts image color temperature within the supported -256 to 256 range.</summary>
    [JsonPropertyName("temperature")]
    public double Temperature { get; init; }

    /// <summary>Flips the captured image vertically.</summary>
    [JsonPropertyName("flip")]
    public bool Flip { get; init; }

    /// <summary>Mirrors the captured image horizontally.</summary>
    [JsonPropertyName("mirror")]
    public bool Mirror { get; init; }

    /// <summary>Matches exposure timing to local mains frequency to reduce banding.</summary>
    [JsonPropertyName("antiflicker")]
    public CameraAntiflickerMode Antiflicker { get; init; }

}

/// <summary>Controls resolution and bitrate for one camera stream.</summary>
public sealed record class CameraStreamConfig
{
    /// <summary>Selects a resolution and frame-rate combination supported by Camera.GetCapabilities.</summary>
    [JsonPropertyName("resolution")]
    public string? Resolution { get; init; }

    /// <summary>Sets stream bitrate in kbps within the range reported by Camera.GetCapabilities.</summary>
    [JsonPropertyName("bitrate")]
    public double? Bitrate { get; init; }

}

/// <summary>Configures camera capture, audio, image processing, motion handling and streams.</summary>
public sealed record class CameraConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "Camera";

    /// <summary>Identifies the camera component instance.</summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>Provides the user-facing camera name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Controls activity LED behavior.</summary>
    [JsonPropertyName("led")]
    public CameraLedConfig? Led { get; init; }

    /// <summary>Controls microphone and speaker behavior.</summary>
    [JsonPropertyName("audio")]
    public CameraAudioConfig? Audio { get; init; }

    /// <summary>Controls camera sound playback.</summary>
    [JsonPropertyName("sounds")]
    public CameraSoundsConfig? Sounds { get; init; }

    /// <summary>Controls motion detection and motion-triggered recordings.</summary>
    [JsonPropertyName("motion")]
    public CameraMotionConfig? Motion { get; init; }

    /// <summary>Controls infrared imaging and automatic day/night switching.</summary>
    [JsonPropertyName("night_vision")]
    public CameraNightVisionConfig? NightVision { get; init; }

    /// <summary>Controls the local RTSP server.</summary>
    [JsonPropertyName("rtsp")]
    public CameraRtspConfig? Rtsp { get; init; }

    /// <summary>Controls image processing parameters.</summary>
    [JsonPropertyName("video")]
    public CameraVideoConfig? Video { get; init; }

    /// <summary>Defines per-stream resolution and bitrate by stream identifier.</summary>
    [JsonPropertyName("streams")]
    public Dictionary<string, CameraStreamConfig>? Streams { get; init; }

}

/// <summary>Defines a polygon used for camera motion detection or privacy masking.</summary>
public sealed record class CameraZoneConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "CameraZone";

    /// <summary>Identifies the camera-zone component instance.</summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>Determines whether the zone participates in camera processing.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

    /// <summary>Selects whether the polygon detects motion or masks video.</summary>
    [JsonPropertyName("type")]
    public CameraZoneType Type { get; init; }

    /// <summary>Defines polygon vertices as normalized x/y coordinate pairs in the 0..10000 space.</summary>
    [JsonPropertyName("coordinates")]
    public int[]? Coordinates { get; init; }

    /// <summary>Provides an optional RGB preview color for the zone.</summary>
    [JsonPropertyName("color")]
    public byte[]? Color { get; init; }

    /// <summary>Provides an optional user-facing zone name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

}

/// <summary>Configures an NFS target used by the storage component.</summary>
public sealed record class StorageNfsConfig
{
    /// <summary>Sets the NFS host; null or omission disables the target according to device behavior.</summary>
    [JsonPropertyName("host")]
    public string? Host { get; init; }

    /// <summary>Sets the exported NFS path; null or omission disables the target according to device behavior.</summary>
    [JsonPropertyName("path")]
    public string? Path { get; init; }

}

/// <summary>Configures a storage instance and its optional network filesystem target.</summary>
public sealed record class StorageConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "Storage";

    /// <summary>Identifies the storage component instance.</summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>Provides the user-facing storage name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Configures the NFS storage target when used.</summary>
    [JsonPropertyName("nfs")]
    public StorageNfsConfig? Nfs { get; init; }

}

/// <summary>Controls RPC access over Bluetooth Low Energy.</summary>
public sealed record class BLERpcConfig
{
    /// <summary>Determines whether RPC communication is exposed over BLE.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

}

/// <summary>Represents the legacy BLE observer configuration retained for firmware compatibility.</summary>
public sealed record class BLEObserverConfig
{
    /// <summary>Determines whether the legacy BLE observer is enabled on firmware that still reports it.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

}

/// <summary>Configures Bluetooth Low Energy services exposed by the device.</summary>
public sealed record class BLEConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "BLE";

    /// <summary>Controls current BLE RPC availability.</summary>
    [JsonPropertyName("rpc")]
    public BLERpcConfig? Rpc { get; init; }

    /// <summary>Keeps BLE running according to firmware-specific power-management behavior.</summary>
    [JsonPropertyName("keep_running")]
    public bool? KeepRunning { get; init; }

    /// <summary>Carries the obsolete observer setting when returned by older firmware.</summary>
    [JsonPropertyName("observer")]
    public BLEObserverConfig? Observer { get; init; }

}

/// <summary>Configures circuit-breaker voltage protection and automatic recovery behavior.</summary>
public sealed record class CBConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "CB";

    /// <summary>Identifies the circuit-breaker component instance.</summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>Provides the user-facing circuit-breaker name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Sets the voltage below which undervoltage protection is triggered.</summary>
    [JsonPropertyName("undervoltage_limit")]
    public double UndervoltageLimit { get; init; }

    /// <summary>Sets the voltage above which overvoltage protection is triggered.</summary>
    [JsonPropertyName("voltage_limit")]
    public double VoltageLimit { get; init; }

    /// <summary>Sets how long a voltage fault must persist before protection reacts.</summary>
    [JsonPropertyName("reaction_delay")]
    public double ReactionDelay { get; init; }

    /// <summary>Determines whether the breaker can recover automatically after voltage returns to the acceptable range.</summary>
    [JsonPropertyName("autorecovery_enable")]
    public bool AutorecoveryEnable { get; init; }

    /// <summary>Sets the allowed voltage deviation used by protection and autorecovery decisions.</summary>
    [JsonPropertyName("voltage_thr")]
    public double VoltageThreshold { get; init; }

    /// <summary>Sets how long acceptable voltage must persist before automatic recovery.</summary>
    [JsonPropertyName("autorecovery_delay")]
    public double AutorecoveryDelay { get; init; }

}

/// <summary>Controls brightness and color temperature during the configured night period.</summary>
public sealed record class CCTNightModeConfig
{
    /// <summary>Determines whether night-mode limits are applied.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

    /// <summary>Sets the night-mode brightness limit, or null to adopt current brightness when night mode starts.</summary>
    [JsonPropertyName("brightness")]
    public double? Brightness { get; init; }

    /// <summary>Sets the night-mode color temperature, or null to adopt the current value when night mode starts.</summary>
    [JsonPropertyName("ct")]
    public double? Ct { get; init; }

    /// <summary>Defines the daily start and end times during which night mode is active.</summary>
    [JsonPropertyName("active_between")]
    public string[]? ActiveBetween { get; init; }

}

/// <summary>Defines the output preset applied after a supported double button press.</summary>
public sealed record class CCTButtonDoublePushConfig
{
    /// <summary>Sets preset brightness, or null to preserve the current brightness.</summary>
    [JsonPropertyName("brightness")]
    public double? Brightness { get; init; }

    /// <summary>Sets preset color temperature, or null to preserve the current color temperature.</summary>
    [JsonPropertyName("ct")]
    public double? Ct { get; init; }

}

/// <summary>Groups CCT button-triggered output presets.</summary>
public sealed record class CCTButtonPresetsConfig
{
    /// <summary>Defines the optional double-push preset; null disables it.</summary>
    [JsonPropertyName("button_doublepush")]
    public CCTButtonDoublePushConfig? ButtonDoublePush { get; init; }

}

/// <summary>Configures tunable-white output startup, timers, night mode, button behavior and protection limits.</summary>
public sealed record class CCTConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "CCT";

    /// <summary>Identifies the CCT component instance.</summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>Provides the user-facing CCT name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Selects the output state applied after power-up.</summary>
    [JsonPropertyName("initial_state")]
    public CCTInitialState InitialState { get; init; }

    /// <summary>Determines whether automatic turn-on is enabled.</summary>
    [JsonPropertyName("auto_on")]
    public bool AutoOn { get; init; }

    /// <summary>Sets the delay before automatic turn-on.</summary>
    [JsonPropertyName("auto_on_delay")]
    public double AutoOnDelay { get; init; }

    /// <summary>Determines whether automatic turn-off is enabled.</summary>
    [JsonPropertyName("auto_off")]
    public bool AutoOff { get; init; }

    /// <summary>Sets the delay before automatic turn-off.</summary>
    [JsonPropertyName("auto_off_delay")]
    public double AutoOffDelay { get; init; }

    /// <summary>Sets the default transition time between output levels.</summary>
    [JsonPropertyName("transition_duration")]
    public double TransitionDuration { get; init; }

    /// <summary>Sets the minimum brightness applied when toggling on from a lower level.</summary>
    [JsonPropertyName("min_brightness_on_toggle")]
    public double MinBrightnessOnToggle { get; init; }

    /// <summary>Controls scheduled night-mode brightness and color temperature.</summary>
    [JsonPropertyName("night_mode")]
    public CCTNightModeConfig? NightMode { get; init; }

    /// <summary>Controls how quickly brightness changes while a dimming button is held.</summary>
    [JsonPropertyName("button_fade_rate")]
    public double? ButtonFadeRate { get; init; }

    /// <summary>Defines supported button presets.</summary>
    [JsonPropertyName("button_presets")]
    public CCTButtonPresetsConfig? ButtonPresets { get; init; }

    /// <summary>Remaps the logical brightness range where supported.</summary>
    [JsonPropertyName("range_map")]
    public double[]? RangeMap { get; init; }

    /// <summary>Defines or reports the supported color-temperature range where applicable.</summary>
    [JsonPropertyName("ct_range")]
    public double[]? CtRange { get; init; }

    /// <summary>Sets the overcurrent protection limit where supported.</summary>
    [JsonPropertyName("current_limit")]
    public double? CurrentLimit { get; init; }

    /// <summary>Sets the overpower protection limit where supported.</summary>
    [JsonPropertyName("power_limit")]
    public double? PowerLimit { get; init; }

    /// <summary>Sets the overvoltage protection limit where supported.</summary>
    [JsonPropertyName("voltage_limit")]
    public double? VoltageLimit { get; init; }

}

/// <summary>Controls whether the device maintains its Shelly Cloud connection and which endpoint it uses.</summary>
public sealed record class CloudConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "Cloud";

    /// <summary>Determines whether cloud connectivity is enabled.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

    /// <summary>Specifies the cloud server endpoint when one is configured.</summary>
    [JsonPropertyName("server")]
    public string? Server { get; init; }

}

/// <summary>Controls motor-idle detection used to recognize completed cover movement.</summary>
public sealed record class CoverMotorConfig
{
    /// <summary>Sets the power threshold below which the motor is considered idle.</summary>
    [JsonPropertyName("idle_power_thr")]
    public double? IdlePowerThreshold { get; init; }

    /// <summary>Sets how long low power must persist before motor idle is confirmed.</summary>
    [JsonPropertyName("idle_confirm_period")]
    public double? IdleConfirmPeriod { get; init; }

}

/// <summary>Controls motor obstruction detection and the response to a detected obstruction.</summary>
public sealed record class CoverObstructionDetectionConfig
{
    /// <summary>Determines whether obstruction detection is active.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

    /// <summary>Selects movement directions monitored for obstruction.</summary>
    [JsonPropertyName("direction")]
    public CoverObstructionDirection Direction { get; init; }

    /// <summary>Selects the response when an obstruction is detected.</summary>
    [JsonPropertyName("action")]
    public CoverObstructionAction Action { get; init; }

    /// <summary>Sets the motor-power threshold used to identify an obstruction.</summary>
    [JsonPropertyName("power_thr")]
    public double PowerThreshold { get; init; }

    /// <summary>Sets the initial movement interval during which obstruction detection is held off.</summary>
    [JsonPropertyName("holdoff")]
    public double Holdoff { get; init; }

}

/// <summary>Controls how an external safety switch constrains cover movement.</summary>
public sealed record class CoverSafetySwitchConfig
{
    /// <summary>Determines whether the safety-switch logic is active.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

    /// <summary>Selects movement directions affected by the safety switch.</summary>
    [JsonPropertyName("direction")]
    public CoverSafetySwitchDirection Direction { get; init; }

    /// <summary>Selects the response when the safety switch activates.</summary>
    [JsonPropertyName("action")]
    public CoverSafetySwitchAction Action { get; init; }

    /// <summary>Selects the movement explicitly allowed while the safety condition is active, when supported.</summary>
    [JsonPropertyName("allowed_move")]
    public CoverSafetySwitchAllowedMove? AllowedMove { get; init; }

}

/// <summary>Controls slat calibration and positioning for covers with tilt/slat support.</summary>
public sealed record class CoverSlatConfig
{
    /// <summary>Determines whether slat positioning is enabled.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

    /// <summary>Sets the calibrated slat opening time.</summary>
    [JsonPropertyName("open_time")]
    public double OpenTime { get; init; }

    /// <summary>Sets the calibrated slat closing time.</summary>
    [JsonPropertyName("close_time")]
    public double CloseTime { get; init; }

    /// <summary>Sets the slat adjustment step used by incremental control.</summary>
    [JsonPropertyName("step")]
    public double Step { get; init; }

    /// <summary>Determines whether slat position is retained across supported cover movements.</summary>
    [JsonPropertyName("retain_pos")]
    public bool RetainPosition { get; init; }

    /// <summary>Enables precise slat control where supported.</summary>
    [JsonPropertyName("precise_ctl")]
    public bool PreciseControl { get; init; }

}

/// <summary>Configures motorized-cover input behavior, travel timing, safety, obstruction handling and optional slat control.</summary>
public sealed record class CoverConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "Cover";

    /// <summary>Identifies the cover component instance.</summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>Provides the user-facing cover name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Selects how physical inputs control the cover.</summary>
    [JsonPropertyName("in_mode")]
    public CoverInputMode InputMode { get; init; }

    /// <summary>Determines whether physical input changes are ignored.</summary>
    [JsonPropertyName("in_locked")]
    public bool? InputLocked { get; init; }

    /// <summary>Selects the startup state or movement behavior where supported.</summary>
    [JsonPropertyName("initial_state")]
    public CoverInitialState? InitialState { get; init; }

    /// <summary>Sets the overpower protection limit where supported.</summary>
    [JsonPropertyName("power_limit")]
    public double? PowerLimit { get; init; }

    /// <summary>Sets the overvoltage protection limit where supported.</summary>
    [JsonPropertyName("voltage_limit")]
    public double? VoltageLimit { get; init; }

    /// <summary>Sets the undervoltage protection limit where supported.</summary>
    [JsonPropertyName("undervoltage_limit")]
    public double? UndervoltageLimit { get; init; }

    /// <summary>Sets the overcurrent protection limit where supported.</summary>
    [JsonPropertyName("current_limit")]
    public double? CurrentLimit { get; init; }

    /// <summary>Controls motor-idle detection.</summary>
    [JsonPropertyName("motor")]
    public CoverMotorConfig? Motor { get; init; }

    /// <summary>Sets the maximum allowed opening travel time.</summary>
    [JsonPropertyName("maxtime_open")]
    public double? MaxTimeOpen { get; init; }

    /// <summary>Sets the maximum allowed closing travel time.</summary>
    [JsonPropertyName("maxtime_close")]
    public double? MaxTimeClose { get; init; }

    /// <summary>Swaps the logical mapping of the physical cover inputs.</summary>
    [JsonPropertyName("swap_inputs")]
    public bool? SwapInputs { get; init; }

    /// <summary>Reverses the configured motor movement directions.</summary>
    [JsonPropertyName("invert_directions")]
    public bool? InvertDirections { get; init; }

    /// <summary>Enables maintenance behavior intended for service or calibration scenarios.</summary>
    [JsonPropertyName("maintenance_mode")]
    public bool? MaintenanceMode { get; init; }

    /// <summary>Controls obstruction detection and response.</summary>
    [JsonPropertyName("obstruction_detection")]
    public CoverObstructionDetectionConfig? ObstructionDetection { get; init; }

    /// <summary>Controls external safety-switch behavior.</summary>
    [JsonPropertyName("safety_switch")]
    public CoverSafetySwitchConfig? SafetySwitch { get; init; }

    /// <summary>Controls optional slat/tilt positioning.</summary>
    [JsonPropertyName("slat")]
    public CoverSlatConfig? Slat { get; init; }

}

/// <summary>Represents the DevicePower configuration result; current firmware returns JSON null, so instances are not expected.</summary>
public sealed record class DevicePowerConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "DevicePower";

}

/// <summary>Represents the empty configuration object returned by EM1Data.GetConfig.</summary>
public sealed record class EM1DataConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "EM1Data";

}

/// <summary>Represents the empty configuration object returned by EMData.GetConfig.</summary>
public sealed record class EMDataConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "EMData";
}
 

/// <summary>Controls per-phase reversal of measured active power and energy direction.</summary>
public sealed record class EMReverseConfig
{
    /// <summary>Reverses measurement direction for phase A.</summary>
    [JsonPropertyName("a")]
    public bool? A { get; init; }

    /// <summary>Reverses measurement direction for phase B.</summary>
    [JsonPropertyName("b")]
    public bool? B { get; init; }

    /// <summary>Reverses measurement direction for phase C.</summary>
    [JsonPropertyName("c")]
    public bool? C { get; init; }

}

/// <summary>Defines under/over thresholds for voltage, current and power on one phase.</summary>
public sealed record class EMPhaseAlarmConfig
{
    /// <summary>Defines under/over voltage thresholds; null disables the threshold set.</summary>
    [JsonPropertyName("voltage")]
    public double?[]? Voltage { get; init; }

    /// <summary>Defines under/over current thresholds; null disables the threshold set.</summary>
    [JsonPropertyName("current")]
    public double?[]? Current { get; init; }

    /// <summary>Defines under/over power thresholds; null disables the threshold set.</summary>
    [JsonPropertyName("power")]
    public double?[]? Power { get; init; }

}

/// <summary>Groups electrical alarm thresholds by phase.</summary>
public sealed record class EMAlarmsConfig
{
    /// <summary>Defines alarm thresholds for phase A.</summary>
    [JsonPropertyName("a")]
    public EMPhaseAlarmConfig? A { get; init; }

    /// <summary>Defines alarm thresholds for phase B.</summary>
    [JsonPropertyName("b")]
    public EMPhaseAlarmConfig? B { get; init; }

    /// <summary>Defines alarm thresholds for phase C.</summary>
    [JsonPropertyName("c")]
    public EMPhaseAlarmConfig? C { get; init; }

}

/// <summary>Configures a three-phase energy meter, including phase selection, measurement direction and alarms.</summary>
public sealed record class EMConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "EM";

    /// <summary>Identifies the EM component instance.</summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>Provides the user-facing meter name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Selects which energy quantity drives LED pulse indication.</summary>
    [JsonPropertyName("blink_mode_selector")]
    public EMBlinkModeSelector? BlinkModeSelector { get; init; }

    /// <summary>Selects the phase used for supported component-level behavior.</summary>
    [JsonPropertyName("phase_selector")]
    public EMPhaseSelector? PhaseSelector { get; init; }

    /// <summary>Determines whether the meter checks phase ordering.</summary>
    [JsonPropertyName("monitor_phase_sequence")]
    public bool? MonitorPhaseSequence { get; init; }

    /// <summary>Controls measurement-direction reversal per phase.</summary>
    [JsonPropertyName("reverse")]
    public EMReverseConfig? Reverse { get; init; }

    /// <summary>Defines per-phase under/over alarm thresholds.</summary>
    [JsonPropertyName("alarms")]
    public EMAlarmsConfig? Alarms { get; init; }

    /// <summary>Selects the current-transformer type from the device-specific set reported by the meter.</summary>
    [JsonPropertyName("ct_type")]
    public string? CtType { get; init; }

}

/// <summary>Defines under/over alarm thresholds for a single electrical measurement channel.</summary>
public sealed record class ElectricalAlarmsConfig
{
    /// <summary>Defines under/over voltage thresholds; null disables the threshold set.</summary>
    [JsonPropertyName("voltage")]
    public double?[]? Voltage { get; init; }

    /// <summary>Defines under/over current thresholds; null disables the threshold set.</summary>
    [JsonPropertyName("current")]
    public double?[]? Current { get; init; }

    /// <summary>Defines under/over active-power thresholds; null disables the threshold set.</summary>
    [JsonPropertyName("power")]
    public double?[]? Power { get; init; }

}

/// <summary>Configures a single-channel energy meter, measurement direction, transformer type and alarms.</summary>
public sealed record class EM1Config : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "EM1";

    /// <summary>Identifies the EM1 component instance.</summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>Provides the user-facing meter name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Reverses measured active-power and energy direction when supported.</summary>
    [JsonPropertyName("reverse")]
    public bool? Reverse { get; init; }

    /// <summary>Selects the current-transformer type from the device-specific supported set.</summary>
    [JsonPropertyName("ct_type")]
    public string? CtType { get; init; }

    /// <summary>Defines under/over electrical alarm thresholds.</summary>
    [JsonPropertyName("alarms")]
    public ElectricalAlarmsConfig? Alarms { get; init; }

}

/// <summary>Configures Ethernet client/server mode, IPv4 addressing and the optional DHCP server range.</summary>
public sealed record class EthernetConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "Eth";

    /// <summary>Determines whether the Ethernet interface is enabled.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

    /// <summary>Selects DHCP-server operation instead of normal client operation.</summary>
    [JsonPropertyName("server_mode")]
    public bool ServerMode { get; init; }

    /// <summary>Selects DHCP or static addressing in client mode.</summary>
    [JsonPropertyName("ipv4mode")]
    public EthIPv4Mode? IPv4Mode { get; init; }

    /// <summary>Sets the interface IPv4 address when static or server addressing is used.</summary>
    [JsonPropertyName("ip")]
    public string? Ip { get; init; }

    /// <summary>Sets the network mask for static or server addressing.</summary>
    [JsonPropertyName("netmask")]
    public string? Netmask { get; init; }

    /// <summary>Sets the gateway when applicable.</summary>
    [JsonPropertyName("gw")]
    public string? Gateway { get; init; }

    /// <summary>Sets the DNS resolver when applicable.</summary>
    [JsonPropertyName("nameserver")]
    public string? Nameserver { get; init; }

    /// <summary>Sets the first IPv4 address leased by the Ethernet DHCP server.</summary>
    [JsonPropertyName("dhcp_start")]
    public string? DhcpStart { get; init; }

    /// <summary>Sets the last IPv4 address leased by the Ethernet DHCP server.</summary>
    [JsonPropertyName("dhcp_end")]
    public string? DhcpEnd { get; init; }

}

/// <summary>Configures how the flood sensor reports and announces detected water.</summary>
public sealed record class FloodConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "Flood";

    /// <summary>Identifies the flood component instance.</summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>Provides the user-facing flood-sensor name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Selects the local alarm behavior used when water is detected.</summary>
    [JsonPropertyName("alarm_mode")]
    public FloodAlarmMode? AlarmMode { get; init; }

    /// <summary>Sets the holdoff interval used to avoid excessive repeated reports.</summary>
    [JsonPropertyName("report_holdoff")]
    public double? ReportHoldoff { get; init; }

}

/// <summary>Configures humidity reporting sensitivity and measurement correction.</summary>
public sealed record class HumidityConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "Humidity";

    /// <summary>Identifies the humidity component instance.</summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>Provides the user-facing humidity-sensor name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Sets the relative-humidity change required to trigger a report.</summary>
    [JsonPropertyName("report_thr")]
    public double ReportThreshold { get; init; }

    /// <summary>Applies a correction offset to measured relative humidity.</summary>
    [JsonPropertyName("offset")]
    public double Offset { get; init; }

}

/// <summary>Configures light-level thresholds used to classify the environment as dark or bright.</summary>
public sealed record class IlluminanceConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "Illuminance";

    /// <summary>Identifies the illuminance component instance.</summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>Provides the user-facing illuminance-sensor name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Sets the illuminance threshold below which the environment is treated as dark.</summary>
    [JsonPropertyName("dark_thr")]
    public double DarkThreshold { get; init; }

    /// <summary>Sets the illuminance threshold above which the environment is treated as bright.</summary>
    [JsonPropertyName("bright_thr")]
    public double BrightThreshold { get; init; }

}

/// <summary>Defines a JavaScript expression and output unit used to expose a transformed sensor value.</summary>
public sealed record class ValueTransformationConfig 
{
    /// <summary>Transforms the raw value x; null or an empty expression disables transformation.</summary>
    [JsonPropertyName("expr")]
    public string? Expression { get; init; }

    /// <summary>Labels the transformed value; null or an empty unit disables the transformed output.</summary>
    [JsonPropertyName("unit")]
    public string? Unit { get; init; }

}

/// <summary>Configures interpretation, reporting and optional value transformation for a physical input.</summary>
public sealed record class InputConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "Input";

    /// <summary>Identifies the Input component instance.</summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>Provides the user-facing input name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Selects how the physical signal is interpreted.</summary>
    [JsonPropertyName("type")]
    public InputType Type { get; init; }

    /// <summary>Determines whether the input emits events and reports active status values.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

    /// <summary>Inverts logical state or analog percentage where applicable.</summary>
    [JsonPropertyName("invert")]
    public bool? Invert { get; init; }

    /// <summary>Allows supported switch/button sequences to trigger a factory reset.</summary>
    [JsonPropertyName("factory_reset")]
    public bool? FactoryReset { get; init; }

    /// <summary>Sets the analog percentage change required before a new value is reported.</summary>
    [JsonPropertyName("report_thr")]
    public double? ReportThreshold { get; init; }

    /// <summary>Remaps the analog 0..100 percent span to a configured output span.</summary>
    [JsonPropertyName("range_map")]
    public double[]? RangeMap { get; init; }

    /// <summary>Selects the device-specific numeric analog input range without introducing a CLR enum.</summary>
    [JsonPropertyName("range")]
    public byte? Range { get; init; }

    /// <summary>Transforms reported analog percentage into a custom derived value.</summary>
    [JsonPropertyName("xpercent")]
    public ValueTransformationConfig? XPercent { get; init; }

    /// <summary>Sets the number of pulses required before counter changes are reported.</summary>
    [JsonPropertyName("count_rep_thr")]
    public int? CountReportThreshold { get; init; }

    /// <summary>Sets the reference interval used for frequency measurement.</summary>
    [JsonPropertyName("freq_window")]
    public int? FrequencyWindow { get; init; }

    /// <summary>Sets the percentage frequency change required before a report.</summary>
    [JsonPropertyName("freq_rep_thr")]
    public double? FrequencyReportThreshold { get; init; }

    /// <summary>Transforms raw counter values into custom derived values.</summary>
    [JsonPropertyName("xcounts")]
    public ValueTransformationConfig? XCounts { get; init; }

    /// <summary>Transforms the measured frequency into a custom derived value.</summary>
    [JsonPropertyName("xfreq")]
    public ValueTransformationConfig? XFrequency { get; init; }

}

/// <summary>Controls the light brightness limit during a scheduled night period.</summary>
public sealed record class LightNightModeConfig
{
    /// <summary>Determines whether night-mode brightness limiting is active.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

    /// <summary>Sets the brightness limit used while night mode is active.</summary>
    [JsonPropertyName("brightness")]
    public double Brightness { get; init; }

    /// <summary>Defines the daily start and end times for night mode.</summary>
    [JsonPropertyName("active_between")]
    public string[]? ActiveBetween { get; init; }

}

/// <summary>Defines the brightness applied after a supported double button press.</summary>
public sealed record class LightButtonDoublePushConfig
{
    /// <summary>Sets the brightness preset applied on double push.</summary>
    [JsonPropertyName("brightness")]
    public double Brightness { get; init; }

}

/// <summary>Groups button-triggered presets for a dimmable light.</summary>
public sealed record class LightButtonPresetsConfig
{
    /// <summary>Defines the optional double-push preset; null disables it.</summary>
    [JsonPropertyName("button_doublepush")]
    public LightButtonDoublePushConfig? ButtonDoublePush { get; init; }

}

/// <summary>Controls the temporary warmup level used when turning on at low brightness.</summary>
public sealed record class LightWarmupConfig
{
    /// <summary>Determines whether warmup is applied when toggling on below the low-brightness threshold.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

    /// <summary>Sets warmup brightness as a percentage.</summary>
    [JsonPropertyName("brightness")]
    public double Brightness { get; init; }

    /// <summary>Sets how long warmup brightness is held before normal output resumes.</summary>
    [JsonPropertyName("time_ms")]
    public double TimeMilliseconds { get; init; }

}

/// <summary>Configures dimmable-light startup, input control, timers, night mode, presets and protection limits.</summary>
public sealed record class LightConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "Light";

    /// <summary>Identifies the Light component instance.</summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>Provides the user-facing light name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Selects how the associated input controls the light.</summary>
    [JsonPropertyName("in_mode")]
    public LightInputMode InputMode { get; init; }

    /// <summary>Selects a device-specific numeric operating mode without introducing a CLR enum.</summary>
    [JsonPropertyName("op_mode")]
    public byte? OperationMode { get; init; }

    /// <summary>Selects the output state applied after power-up.</summary>
    [JsonPropertyName("initial_state")]
    public LightInitialState InitialState { get; init; }

    /// <summary>Determines whether automatic turn-on is enabled.</summary>
    [JsonPropertyName("auto_on")]
    public bool AutoOn { get; init; }

    /// <summary>Sets the delay before automatic turn-on.</summary>
    [JsonPropertyName("auto_on_delay")]
    public double AutoOnDelay { get; init; }

    /// <summary>Determines whether automatic turn-off is enabled.</summary>
    [JsonPropertyName("auto_off")]
    public bool AutoOff { get; init; }

    /// <summary>Sets the delay before automatic turn-off.</summary>
    [JsonPropertyName("auto_off_delay")]
    public double AutoOffDelay { get; init; }

    /// <summary>Sets the default transition time between brightness levels.</summary>
    [JsonPropertyName("transition_duration")]
    public double TransitionDuration { get; init; }

    /// <summary>Sets gamma correction for perceptually linear dimming where supported.</summary>
    [JsonPropertyName("gamma")]
    public double? Gamma { get; init; }

    /// <summary>Sets the minimum brightness applied when toggling on.</summary>
    [JsonPropertyName("min_brightness_on_toggle")]
    public double MinBrightnessOnToggle { get; init; }

    /// <summary>Controls scheduled night-mode brightness.</summary>
    [JsonPropertyName("night_mode")]
    public LightNightModeConfig? NightMode { get; init; }

    /// <summary>Controls dimming speed while a button is held.</summary>
    [JsonPropertyName("button_fade_rate")]
    public double? ButtonFadeRate { get; init; }

    /// <summary>Defines supported button presets.</summary>
    [JsonPropertyName("button_presets")]
    public LightButtonPresetsConfig? ButtonPresets { get; init; }

    /// <summary>Remaps the logical brightness span where supported.</summary>
    [JsonPropertyName("range_map")]
    public double[]? RangeMap { get; init; }

    /// <summary>Sets the overpower protection limit where supported.</summary>
    [JsonPropertyName("power_limit")]
    public double? PowerLimit { get; init; }

    /// <summary>Sets the overvoltage protection limit where supported.</summary>
    [JsonPropertyName("voltage_limit")]
    public double? VoltageLimit { get; init; }

    /// <summary>Sets the undervoltage protection limit where supported.</summary>
    [JsonPropertyName("undervoltage_limit")]
    public double? UndervoltageLimit { get; init; }

    /// <summary>Sets the overcurrent protection limit where supported.</summary>
    [JsonPropertyName("current_limit")]
    public double? CurrentLimit { get; init; }

    /// <summary>Controls low-brightness startup warmup where supported.</summary>
    [JsonPropertyName("warmup")]
    public LightWarmupConfig? Warmup { get; init; }

}

/// <summary>Controls whether the Matter server is enabled for commissioning and fabric participation.</summary>
public sealed record class MatterConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "Matter";

    /// <summary>Determines whether Matter functionality is enabled.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

}

/// <summary>Controls whether the Modbus TCP server is enabled.</summary>
public sealed record class ModbusConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "Modbus";

    /// <summary>Determines whether the device accepts Modbus TCP connections.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

}

/// <summary>Configures the outbound MQTT connection, topic routing, TLS and notification/control features.</summary>
public sealed record class MQTTConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "MQTT";


    /// <summary>Determines whether the MQTT connection is enabled.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

    /// <summary>Sets the broker host and optional port.</summary>
    [JsonPropertyName("server")]
    public string? Server { get; init; }

    /// <summary>Sets the MQTT client identifier; null lets the device identifier be used.</summary>
    [JsonPropertyName("client_id")]
    public string? ClientId { get; init; }

    /// <summary>Sets the MQTT username when authentication is required.</summary>
    [JsonPropertyName("user")]
    public string? User { get; init; }

    /// <summary>Selects plain TCP or the TLS certificate-validation strategy.</summary>
    [JsonPropertyName("ssl_ca")]
    public MQTTSslCa? SslCa { get; init; }

    /// <summary>Overrides the default device-id topic prefix.</summary>
    [JsonPropertyName("topic_prefix")]
    public string? TopicPrefix { get; init; }

    /// <summary>Determines whether NotifyStatus and NotifyEvent RPC notifications are published.</summary>
    [JsonPropertyName("rpc_ntf")]
    public bool RpcNotifications { get; init; }

    /// <summary>Determines whether complete component status payloads are published after significant changes.</summary>
    [JsonPropertyName("status_ntf")]
    public bool StatusNotifications { get; init; }

    /// <summary>Determines whether the configured client certificate is used for MQTT TLS.</summary>
    [JsonPropertyName("use_client_cert")]
    public bool UseClientCertificate { get; init; }

    /// <summary>Determines whether the MQTT RPC channel is available.</summary>
    [JsonPropertyName("enable_rpc")]
    public bool EnableRpc { get; init; }

    /// <summary>Determines whether the MQTT control interface is enabled.</summary>
    [JsonPropertyName("enable_control")]
    public bool EnableControl { get; init; }

}

/// <summary>Configures a single-channel power meter, measurement direction and electrical alarms.</summary>
public sealed record class PM1Config : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "PM1";

    /// <summary>Identifies the PM1 component instance.</summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>Provides the user-facing meter name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Reverses measured active-power and energy direction.</summary>
    [JsonPropertyName("reverse")]
    public bool Reverse { get; init; }

    /// <summary>Defines under/over electrical alarm thresholds.</summary>
    [JsonPropertyName("alarms")]
    public ElectricalAlarmsConfig? Alarms { get; init; }

}

/// <summary>Selects the active Pill peripheral mode and assigns roles to its configurable pins.</summary>
public sealed record class PillConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "Pill";

    /// <summary>Selects the peripheral subsystem exposed by the device.</summary>
    [JsonPropertyName("mode")]
    public PillMode Mode { get; init; }

    /// <summary>Assigns the function of pin 0.</summary>
    [JsonPropertyName("pin0_mode")]
    public PillPin0Mode Pin0Mode { get; init; }

    /// <summary>Assigns the function of pin 1.</summary>
    [JsonPropertyName("pin1_mode")]
    public PillPin1Mode Pin1Mode { get; init; }

    /// <summary>Assigns the function of pin 2.</summary>
    [JsonPropertyName("pin2_mode")]
    public PillPin2Mode Pin2Mode { get; init; }

}

/// <summary>Tunes thresholds used to enter and leave the presence detector tracking states.</summary>
public sealed record class PresenceSensorStateConfig
{
    /// <summary>Sets the motion threshold that activates detection.</summary>
    [JsonPropertyName("det_act_thr")]
    public double? DetectionActivationThreshold { get; init; }

    /// <summary>Sets the threshold used to release motion detection.</summary>
    [JsonPropertyName("det_free_thr")]
    public double? DetectionFreeThreshold { get; init; }

    /// <summary>Sets the threshold used to decide that tracking has been lost.</summary>
    [JsonPropertyName("act_free_thr")]
    public double? ActiveFreeThreshold { get; init; }

    /// <summary>Sets the threshold controlling still-object tracking.</summary>
    [JsonPropertyName("stat_free_thr")]
    public double? StaticFreeThreshold { get; init; }

    /// <summary>Sets the stillness timeout threshold before sleep/free behavior.</summary>
    [JsonPropertyName("sleep_free_thr")]
    public double? SleepFreeThreshold { get; init; }

}

/// <summary>Configures physical mounting and radar-detection tuning for the presence sensor.</summary>
public sealed record class PresenceSensorConfig
{
    /// <summary>Accounts for a sensor mounted with a 180-degree flip.</summary>
    [JsonPropertyName("flipped")]
    public bool Flipped { get; init; }

    /// <summary>Sets the sensor mounting height in meters.</summary>
    [JsonPropertyName("height")]
    public double Height { get; init; }

    /// <summary>Sets the vertical sensor tilt, or allows calibration to determine it when omitted.</summary>
    [JsonPropertyName("tilt")]
    public double? Tilt { get; init; }

    /// <summary>Sets the object-recognition threshold when manual tuning is used.</summary>
    [JsonPropertyName("points")]
    public double? Points { get; init; }

    /// <summary>Sets the velocity threshold when manual tuning is used.</summary>
    [JsonPropertyName("velocity")]
    public double? Velocity { get; init; }

    /// <summary>Sets the signal-to-noise threshold when manual tuning is used.</summary>
    [JsonPropertyName("snr")]
    public double? Snr { get; init; }

    /// <summary>Sets the maximum velocity difference used by manual tuning.</summary>
    [JsonPropertyName("max_velocity")]
    public double? MaxVelocity { get; init; }

    /// <summary>Selects the sensor mounting position in the room.</summary>
    [JsonPropertyName("position")]
    public PresenceSensorPosition Position { get; init; }

    /// <summary>Selects the sensor power preset.</summary>
    [JsonPropertyName("power")]
    public PresenceSensorPower Power { get; init; }

    /// <summary>Selects the bundled sensitivity preset when individual tuning values are not used.</summary>
    [JsonPropertyName("sensitivity")]
    public PresenceSensorSensitivity? Sensitivity { get; init; }

    /// <summary>Defines detection-state thresholds for manual tuning.</summary>
    [JsonPropertyName("state")]
    public PresenceSensorStateConfig? State { get; init; }

}

/// <summary>Controls presentation preferences for presence-sensor values.</summary>
public sealed record class PresenceUiConfig
{
    /// <summary>Uses imperial units in the device UI when enabled.</summary>
    [JsonPropertyName("imperial")]
    public bool Imperial { get; init; }

}

/// <summary>Controls presence-device LED brightness during a scheduled night period.</summary>
public sealed record class PresenceNightModeConfig
{
    /// <summary>Determines whether LED night mode is active.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

    /// <summary>Sets the LED brightness limit used during night mode.</summary>
    [JsonPropertyName("brightness")]
    public double Brightness { get; init; }

    /// <summary>Defines the daily start and end times for LED night mode.</summary>
    [JsonPropertyName("active_between")]
    public string[]? ActiveBetween { get; init; }

}

/// <summary>Controls normal and night-period brightness of the presence-device LEDs.</summary>
public sealed record class PresenceLedsConfig
{
    /// <summary>Sets the normal maximum LED brightness; current device examples return a numeric level.</summary>
    [JsonPropertyName("brightness")]
    public double Brightness { get; init; }

    /// <summary>Controls scheduled LED brightness limiting.</summary>
    [JsonPropertyName("night_mode")]
    public PresenceNightModeConfig? NightMode { get; init; }

}

/// <summary>Configures radar tracking limits, mounting geometry, detection tuning, ignored areas and LEDs.</summary>
public sealed record class PresenceConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "Presence";

    /// <summary>Determines whether radar tracking is enabled.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

    /// <summary>Sets how many objects may be tracked simultaneously, from 1 to 10.</summary>
    [JsonPropertyName("num_tracks")]
    public byte NumTracks { get; init; }

    /// <summary>Sets the lower vertical detection boundary in meters.</summary>
    [JsonPropertyName("zmin")]
    public double? ZMin { get; init; }

    /// <summary>Sets the upper vertical detection boundary in meters.</summary>
    [JsonPropertyName("zmax")]
    public double? ZMax { get; init; }

    /// <summary>Configures radar mounting and detection behavior.</summary>
    [JsonPropertyName("sensor")]
    public PresenceSensorConfig? Sensor { get; init; }

    /// <summary>Controls unit presentation in the device UI.</summary>
    [JsonPropertyName("ui")]
    public PresenceUiConfig? Ui { get; init; }

    /// <summary>Defines segments that the radar ignores when tracking objects.</summary>
    [JsonPropertyName("blind")]
    public int[][]? Blind { get; init; }

    /// <summary>Controls the device LEDs.</summary>
    [JsonPropertyName("leds")]
    public PresenceLedsConfig? Leds { get; init; }

    /// <summary>Identifies the default presence zone; this value is read-only.</summary>
    [JsonPropertyName("main_zone")]
    public string? MainZone { get; init; }

}

/// <summary>Defines one dynamic detection zone used to aggregate tracked objects into a presence state.</summary>
public sealed record class PresenceZoneConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "PresenceZone";

    /// <summary>Identifies the PresenceZone component instance.</summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>Provides the user-facing zone name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Determines whether the zone participates in presence evaluation.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

    /// <summary>Provides the RGB preview color for the zone.</summary>
    [JsonPropertyName("color")]
    public byte[]? Color { get; init; }

    /// <summary>Sets how long presence must persist before the zone becomes active.</summary>
    [JsonPropertyName("presence_delay")]
    public double? PresenceDelay { get; init; }

    /// <summary>Sets how long absence must persist before the zone clears.</summary>
    [JsonPropertyName("absence_delay")]
    public double? AbsenceDelay { get; init; }

    /// <summary>Defines the zone as one or more grid-cell rectangles [x0,y0,x1,y1].</summary>
    [JsonPropertyName("area")]
    public int[][]? Area { get; init; }

}

/// <summary>Controls RGB brightness and color during the configured night period.</summary>
public sealed record class RGBNightModeConfig
{
    /// <summary>Determines whether night-mode limits are applied.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

    /// <summary>Sets the night-mode brightness limit, or null to use current brightness when night mode starts.</summary>
    [JsonPropertyName("brightness")]
    public double? Brightness { get; init; }

    /// <summary>Sets night-mode RGB color, or null to use the current color when night mode starts.</summary>
    [JsonPropertyName("rgb")]
    public byte[]? Rgb { get; init; }

    /// <summary>Defines the daily start and end times during which night mode is active.</summary>
    [JsonPropertyName("active_between")]
    public string[]? ActiveBetween { get; init; }

}

/// <summary>Defines brightness and color applied after a supported double button press.</summary>
public sealed record class RGBButtonDoublePushConfig
{
    /// <summary>Sets preset brightness, or null to preserve current brightness.</summary>
    [JsonPropertyName("brightness")]
    public double? Brightness { get; init; }

    /// <summary>Sets preset RGB color, or null to preserve current color.</summary>
    [JsonPropertyName("rgb")]
    public byte[]? Rgb { get; init; }

}

/// <summary>Groups RGB button-triggered output presets.</summary>
public sealed record class RGBButtonPresetsConfig
{
    /// <summary>Defines the optional double-push preset; null disables it.</summary>
    [JsonPropertyName("button_doublepush")]
    public RGBButtonDoublePushConfig? ButtonDoublePush { get; init; }

}

/// <summary>Configures RGB startup, input control, timers, night mode, button presets and protection limits.</summary>
public sealed record class RGBConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "RGB";

    /// <summary>Identifies the RGB component instance.</summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>Provides the user-facing RGB output name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Selects how the associated input controls the RGB output.</summary>
    [JsonPropertyName("in_mode")]
    public RGBInputMode InputMode { get; init; }

    /// <summary>Selects the output state applied after power-up.</summary>
    [JsonPropertyName("initial_state")]
    public RGBInitialState InitialState { get; init; }

    /// <summary>Determines whether automatic turn-on is enabled.</summary>
    [JsonPropertyName("auto_on")]
    public bool AutoOn { get; init; }

    /// <summary>Sets the delay before automatic turn-on.</summary>
    [JsonPropertyName("auto_on_delay")]
    public double AutoOnDelay { get; init; }

    /// <summary>Determines whether automatic turn-off is enabled.</summary>
    [JsonPropertyName("auto_off")]
    public bool AutoOff { get; init; }

    /// <summary>Sets the delay before automatic turn-off.</summary>
    [JsonPropertyName("auto_off_delay")]
    public double AutoOffDelay { get; init; }

    /// <summary>Sets the default transition time for brightness or RGB changes.</summary>
    [JsonPropertyName("transition_duration")]
    public double TransitionDuration { get; init; }

    /// <summary>Sets the minimum brightness applied when toggling on.</summary>
    [JsonPropertyName("min_brightness_on_toggle")]
    public double MinBrightnessOnToggle { get; init; }

    /// <summary>Controls scheduled night-mode brightness and color.</summary>
    [JsonPropertyName("night_mode")]
    public RGBNightModeConfig? NightMode { get; init; }

    /// <summary>Controls dimming speed while a button is held.</summary>
    [JsonPropertyName("button_fade_rate")]
    public double? ButtonFadeRate { get; init; }

    /// <summary>Defines supported button presets.</summary>
    [JsonPropertyName("button_presets")]
    public RGBButtonPresetsConfig? ButtonPresets { get; init; }

    /// <summary>Sets the overcurrent protection limit where supported.</summary>
    [JsonPropertyName("current_limit")]
    public double? CurrentLimit { get; init; }

    /// <summary>Sets the overpower protection limit where supported.</summary>
    [JsonPropertyName("power_limit")]
    public double? PowerLimit { get; init; }

    /// <summary>Sets the overvoltage protection limit where supported.</summary>
    [JsonPropertyName("voltage_limit")]
    public double? VoltageLimit { get; init; }

}

/// <summary>Controls RGBCCT brightness, color, color temperature and color model during the night period.</summary>
public sealed record class RGBCCTNightModeConfig
{
    /// <summary>Determines whether night-mode limits are applied.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

    /// <summary>Sets the night-mode brightness limit, or null to preserve current brightness.</summary>
    [JsonPropertyName("brightness")]
    public double? Brightness { get; init; }

    /// <summary>Sets night-mode RGB color, or null to preserve current color.</summary>
    [JsonPropertyName("rgb")]
    public byte[]? Rgb { get; init; }

    /// <summary>Sets night-mode color temperature, or null to preserve the current value.</summary>
    [JsonPropertyName("ct")]
    public double? Ct { get; init; }

    /// <summary>Selects RGB or CCT output during night mode, or null to preserve the current mode.</summary>
    [JsonPropertyName("mode")]
    public RGBCCTNightModeMode? Mode { get; init; }

    /// <summary>Defines the daily start and end times during which night mode is active.</summary>
    [JsonPropertyName("active_between")]
    public string[]? ActiveBetween { get; init; }

}

/// <summary>Configures combined RGB/CCT output startup, timers, transitions and scheduled night behavior.</summary>
public sealed record class RGBCCTConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "RGBCCT";

    /// <summary>Identifies the RGBCCT component instance.</summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>Provides the user-facing RGBCCT output name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Selects the output state applied after power-up.</summary>
    [JsonPropertyName("initial_state")]
    public RGBCCTInitialState InitialState { get; init; }

    /// <summary>Determines whether automatic turn-on is enabled.</summary>
    [JsonPropertyName("auto_on")]
    public bool AutoOn { get; init; }

    /// <summary>Sets the delay before automatic turn-on.</summary>
    [JsonPropertyName("auto_on_delay")]
    public double AutoOnDelay { get; init; }

    /// <summary>Determines whether automatic turn-off is enabled.</summary>
    [JsonPropertyName("auto_off")]
    public bool AutoOff { get; init; }

    /// <summary>Sets the delay before automatic turn-off.</summary>
    [JsonPropertyName("auto_off_delay")]
    public double AutoOffDelay { get; init; }

    /// <summary>Sets the default transition time for brightness, RGB or color-temperature changes.</summary>
    [JsonPropertyName("transition_duration")]
    public double TransitionDuration { get; init; }

    /// <summary>Sets the minimum brightness applied when toggling on.</summary>
    [JsonPropertyName("min_brightness_on_toggle")]
    public double MinBrightnessOnToggle { get; init; }

    /// <summary>Controls scheduled night-mode output behavior.</summary>
    [JsonPropertyName("night_mode")]
    public RGBCCTNightModeConfig? NightMode { get; init; }

}

/// <summary>Controls RGBW brightness, color and white level during the configured night period.</summary>
public sealed record class RGBWNightModeConfig
{
    /// <summary>Determines whether night-mode limits are applied.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

    /// <summary>Sets the night-mode brightness limit, or null to preserve current brightness.</summary>
    [JsonPropertyName("brightness")]
    public double? Brightness { get; init; }

    /// <summary>Sets night-mode RGB color, or null to preserve current color.</summary>
    [JsonPropertyName("rgb")]
    public byte[]? Rgb { get; init; }

    /// <summary>Sets the night-mode white level, or null to preserve the current value.</summary>
    [JsonPropertyName("white")]
    public double? White { get; init; }

    /// <summary>Defines the daily start and end times during which night mode is active.</summary>
    [JsonPropertyName("active_between")]
    public string[]? ActiveBetween { get; init; }

}

/// <summary>Defines brightness, RGB color and white level applied after a supported double button press.</summary>
public sealed record class RGBWButtonDoublePushConfig
{
    /// <summary>Sets preset brightness, or null to preserve current brightness.</summary>
    [JsonPropertyName("brightness")]
    public double? Brightness { get; init; }

    /// <summary>Sets preset RGB color, or null to preserve current color.</summary>
    [JsonPropertyName("rgb")]
    public byte[]? Rgb { get; init; }

    /// <summary>Sets preset white level, or null to preserve the current value.</summary>
    [JsonPropertyName("white")]
    public double? White { get; init; }

}

/// <summary>Groups RGBW button-triggered output presets.</summary>
public sealed record class RGBWButtonPresetsConfig
{
    /// <summary>Defines the optional double-push preset; null disables it.</summary>
    [JsonPropertyName("button_doublepush")]
    public RGBWButtonDoublePushConfig? ButtonDoublePush { get; init; }

}

/// <summary>Configures RGBW startup, input control, timers, night mode, presets and protection limits.</summary>
public sealed record class RGBWConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "RGBW";

    /// <summary>Identifies the RGBW component instance.</summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>Provides the user-facing RGBW output name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Selects how the associated input controls the RGBW output.</summary>
    [JsonPropertyName("in_mode")]
    public RGBWInputMode InputMode { get; init; }

    /// <summary>Selects the output state applied after power-up.</summary>
    [JsonPropertyName("initial_state")]
    public RGBWInitialState InitialState { get; init; }

    /// <summary>Determines whether automatic turn-on is enabled.</summary>
    [JsonPropertyName("auto_on")]
    public bool AutoOn { get; init; }

    /// <summary>Sets the delay before automatic turn-on.</summary>
    [JsonPropertyName("auto_on_delay")]
    public double AutoOnDelay { get; init; }

    /// <summary>Determines whether automatic turn-off is enabled.</summary>
    [JsonPropertyName("auto_off")]
    public bool AutoOff { get; init; }

    /// <summary>Sets the delay before automatic turn-off.</summary>
    [JsonPropertyName("auto_off_delay")]
    public double AutoOffDelay { get; init; }

    /// <summary>Sets the default transition time for brightness, RGB or white-level changes.</summary>
    [JsonPropertyName("transition_duration")]
    public double TransitionDuration { get; init; }

    /// <summary>Sets the minimum brightness applied when toggling on.</summary>
    [JsonPropertyName("min_brightness_on_toggle")]
    public double MinBrightnessOnToggle { get; init; }

    /// <summary>Controls scheduled night-mode brightness, color and white level.</summary>
    [JsonPropertyName("night_mode")]
    public RGBWNightModeConfig? NightMode { get; init; }

    /// <summary>Controls dimming speed while a button is held.</summary>
    [JsonPropertyName("button_fade_rate")]
    public double? ButtonFadeRate { get; init; }

    /// <summary>Defines supported button presets.</summary>
    [JsonPropertyName("button_presets")]
    public RGBWButtonPresetsConfig? ButtonPresets { get; init; }

    /// <summary>Sets the overcurrent protection limit where supported.</summary>
    [JsonPropertyName("current_limit")]
    public double? CurrentLimit { get; init; }

    /// <summary>Sets the overpower protection limit where supported.</summary>
    [JsonPropertyName("power_limit")]
    public double? PowerLimit { get; init; }

    /// <summary>Sets the overvoltage protection limit where supported.</summary>
    [JsonPropertyName("voltage_limit")]
    public double? VoltageLimit { get; init; }

}

/// <summary>Configures script identity and whether the script starts automatically at boot.</summary>
public sealed record class ScriptConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "Script";

    /// <summary>Identifies the script component instance.</summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>Provides the script name shown by management interfaces.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Determines whether the script is started automatically after boot.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

}

/// <summary>Configures serial framing and hardware direction-control options.</summary>
public sealed record class SerialPortConfig
{
    /// <summary>Sets the serial bit rate.</summary>
    [JsonPropertyName("baud")]
    public int Baud { get; init; }

    /// <summary>Sets data bits, parity and stop bits using the three-character serial format.</summary>
    [JsonPropertyName("format")]
    public string? Format { get; init; }

    /// <summary>Enables half-duplex operation on hardware that supports it.</summary>
    [JsonPropertyName("hd")]
    public bool? HalfDuplex { get; init; }

    /// <summary>Selects the active level of the driver-enable signal on supported hardware.</summary>
    [JsonPropertyName("de_al")]
    public bool? DriverEnableActiveLevel { get; init; }

}

/// <summary>Configures the Modbus RTU server identity for the serial port.</summary>
public sealed record class SerialModbusServerConfig
{
    /// <summary>Sets the Modbus RTU server address from 1 to 247.</summary>
    [JsonPropertyName("addr")]
    public byte Address { get; init; }

}

/// <summary>Configures ownership and framing of a hardware serial port and its optional Modbus role.</summary>
public sealed record class SerialConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "Serial";

    /// <summary>Identifies the Serial component instance when firmware includes it in the configuration payload.</summary>
    [JsonPropertyName("id")]
    public int? Id { get; init; }

    /// <summary>Selects the subsystem using the serial port.</summary>
    [JsonPropertyName("mode")]
    public SerialMode Mode { get; init; }

    /// <summary>Configures baud rate and frame format for the selected mode.</summary>
    [JsonPropertyName("serial")]
    public SerialPortConfig? Serial { get; init; }

    /// <summary>Configures the RTU server address when server mode is selected.</summary>
    [JsonPropertyName("mb_server")]
    public SerialModbusServerConfig? ModbusServer { get; init; }

    /// <summary>Preserves the reserved Modbus-client configuration object for forward compatibility.</summary>
    [JsonPropertyName("mb_client")]
    public JsonElement? ModbusClient { get; init; }

}

/// <summary>Configures the user-facing identity of a smoke-sensor component.</summary>
public sealed record class SmokeConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "Smoke";

    /// <summary>Identifies the Smoke component instance.</summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>Provides the user-facing smoke-sensor name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

}

/// <summary>Controls optional lifecycle counters maintained by the switch.</summary>
public sealed record class SwitchCountsConfig
{
    /// <summary>Determines whether switch lifecycle counters are collected.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

    /// <summary>Sets the active-power threshold above which the on-above-threshold counter accumulates.</summary>
    [JsonPropertyName("power_thr")]
    public double? PowerThreshold { get; init; }

}

/// <summary>Configures relay input behavior, startup state, automatic timers, electrical protection and optional counters.</summary>
public sealed record class SwitchConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "Switch";

    /// <summary>Identifies the Switch component instance.</summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>Provides the user-facing switch name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Selects how the associated input controls the relay.</summary>
    [JsonPropertyName("in_mode")]
    public SwitchInputMode InputMode { get; init; }

    /// <summary>Determines whether physical input changes are ignored.</summary>
    [JsonPropertyName("in_locked")]
    public bool? InputLocked { get; init; }

    /// <summary>Selects the relay state applied after power-up.</summary>
    [JsonPropertyName("initial_state")]
    public SwitchInitialState InitialState { get; init; }

    /// <summary>Determines whether automatic turn-on is enabled.</summary>
    [JsonPropertyName("auto_on")]
    public bool AutoOn { get; init; }

    /// <summary>Sets the delay before automatic turn-on.</summary>
    [JsonPropertyName("auto_on_delay")]
    public double AutoOnDelay { get; init; }

    /// <summary>Determines whether automatic turn-off is enabled.</summary>
    [JsonPropertyName("auto_off")]
    public bool AutoOff { get; init; }

    /// <summary>Sets the delay before automatic turn-off.</summary>
    [JsonPropertyName("auto_off_delay")]
    public double AutoOffDelay { get; init; }

    /// <summary>Determines whether output state is restored after an over/undervoltage fault clears.</summary>
    [JsonPropertyName("autorecover_voltage_errors")]
    public bool? AutorecoverVoltageErrors { get; init; }

    /// <summary>Identifies the controlling Input component using the documented numeric values without a CLR enum.</summary>
    [JsonPropertyName("input_id")]
    public byte? InputId { get; init; }

    /// <summary>Sets the overpower protection limit where supported.</summary>
    [JsonPropertyName("power_limit")]
    public double? PowerLimit { get; init; }

    /// <summary>Sets the overvoltage protection limit where supported.</summary>
    [JsonPropertyName("voltage_limit")]
    public double? VoltageLimit { get; init; }

    /// <summary>Sets the undervoltage protection limit where supported.</summary>
    [JsonPropertyName("undervoltage_limit")]
    public double? UndervoltageLimit { get; init; }

    /// <summary>Sets the overcurrent protection limit where supported.</summary>
    [JsonPropertyName("current_limit")]
    public double? CurrentLimit { get; init; }

    /// <summary>Reverses measured active-power and energy direction where supported.</summary>
    [JsonPropertyName("reverse")]
    public bool? Reverse { get; init; }

    /// <summary>Controls optional switch lifecycle counters.</summary>
    [JsonPropertyName("counts")]
    public SwitchCountsConfig? Counts { get; init; }

}

/// <summary>Configures device identity, discoverability, optional hardware and security-related device behavior.</summary>
public sealed record class SysDeviceConfig
{
    /// <summary>Sets the user-facing device name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Reduces power consumption at the cost of execution speed and network latency.</summary>
    [JsonPropertyName("eco_mode")]
    public bool? EcoMode { get; init; }

    /// <summary>Reports the read-only base MAC address.</summary>
    [JsonPropertyName("mac")]
    public string? Mac { get; init; }

    /// <summary>Reports the read-only firmware build identifier.</summary>
    [JsonPropertyName("fw_id")]
    public string? FirmwareId { get; init; }

    /// <summary>Selects or reports the active device profile on multi-profile devices.</summary>
    [JsonPropertyName("profile")]
    public string? Profile { get; init; }

    /// <summary>Determines whether the device is exposed in discovery lists.</summary>
    [JsonPropertyName("discoverable")]
    public bool? Discoverable { get; init; }

    /// <summary>Selects the supported add-on board, or null when no add-on is enabled.</summary>
    [JsonPropertyName("addon_type")]
    public SysAddonType? AddonType { get; init; }

    /// <summary>Enables system-button toggling of outputs on supported devices.</summary>
    [JsonPropertyName("sys_btn_toggle")]
    public bool? SystemButtonToggle { get; init; }

    /// <summary>Controls TLS certificate date/time validation where enhanced-security policy permits it.</summary>
    [JsonPropertyName("tls_check_cert_validity_time")]
    public bool? TlsCheckCertificateValidityTime { get; init; }

    /// <summary>Reports or enables the enhanced-security mode subject to firmware and factory-state restrictions.</summary>
    [JsonPropertyName("enhanced_security")]
    public bool? EnhancedSecurity { get; init; }

}

/// <summary>Configures timezone and optional geographic coordinates used by schedules and sun-based calculations.</summary>
public sealed record class SysLocationConfig
{
    /// <summary>Sets the IANA timezone identifier, or null when unavailable.</summary>
    [JsonPropertyName("tz")]
    public string? Timezone { get; init; }

    /// <summary>Sets latitude in degrees for location-dependent calculations.</summary>
    [JsonPropertyName("lat")]
    public double? Latitude { get; init; }

    /// <summary>Sets longitude in degrees for location-dependent calculations.</summary>
    [JsonPropertyName("lon")]
    public double? Longitude { get; init; }

}

/// <summary>Controls one debug-log streaming destination.</summary>
public sealed record class SysDebugToggleConfig
{
    /// <summary>Determines whether this debug-log destination is enabled.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

}

/// <summary>Configures the UDP destination for streamed debug logs.</summary>
public sealed record class SysDebugUdpConfig
{
    /// <summary>Sets the UDP log destination; null disables UDP log streaming.</summary>
    [JsonPropertyName("addr")]
    public string? Address { get; init; }

}

/// <summary>Controls debug-log streaming over supported transports and local files.</summary>
public sealed record class SysDebugConfig
{
    /// <summary>Controls debug logs sent over MQTT.</summary>
    [JsonPropertyName("mqtt")]
    public SysDebugToggleConfig? Mqtt { get; init; }

    /// <summary>Controls debug logs sent over WebSocket.</summary>
    [JsonPropertyName("websocket")]
    public SysDebugToggleConfig? Websocket { get; init; }

    /// <summary>Configures debug logs sent over UDP.</summary>
    [JsonPropertyName("udp")]
    public SysDebugUdpConfig? Udp { get; init; }

    /// <summary>Controls device-local debug log files.</summary>
    [JsonPropertyName("file_log")]
    public SysDebugToggleConfig? FileLog { get; init; }

}

/// <summary>Configures RPC communication over UDP.</summary>
public sealed record class SysRpcUdpConfig
{
    /// <summary>Sets the default UDP destination used for RPC traffic.</summary>
    [JsonPropertyName("dst_addr")]
    public string? DestinationAddress { get; init; }

    /// <summary>Sets the inbound UDP RPC port; null disables inbound UDP RPC.</summary>
    [JsonPropertyName("listen_port")]
    public int? ListenPort { get; init; }

}

/// <summary>Configures the time server used for clock synchronization.</summary>
public sealed record class SysSntpConfig
{
    /// <summary>Sets the SNTP host and optional port used for time synchronization.</summary>
    [JsonPropertyName("server")]
    public string? Server { get; init; }

}

/// <summary>Configures device identity, location, diagnostics, UDP RPC and time synchronization.</summary>
public sealed record class SysConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "Sys";


    /// <summary>Groups device identity, profile, add-on and security settings.</summary>
    [JsonPropertyName("device")]
    public SysDeviceConfig? Device { get; init; }

    /// <summary>Provides timezone and geographic coordinates.</summary>
    [JsonPropertyName("location")]
    public SysLocationConfig? Location { get; init; }

    /// <summary>Controls diagnostic log destinations.</summary>
    [JsonPropertyName("debug")]
    public SysDebugConfig? Debug { get; init; }

    /// <summary>Preserves user-interface data whose keys are device/application defined.</summary>
    [JsonPropertyName("ui_data")]
    public Dictionary<string, JsonElement>? UiData { get; init; }

    /// <summary>Configures RPC over UDP.</summary>
    [JsonPropertyName("rpc_udp")]
    public SysRpcUdpConfig? RpcUdp { get; init; }

    /// <summary>Configures network time synchronization.</summary>
    [JsonPropertyName("sntp")]
    public SysSntpConfig? Sntp { get; init; }

    /// <summary>Reports the read-only configuration revision incremented after effective configuration changes.</summary>
    [JsonPropertyName("cfg_rev")]
    public int? ConfigRevision { get; init; }

}

/// <summary>Configures temperature reporting sensitivity and measurement correction.</summary>
public sealed record class TemperatureConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "Temperature";

    /// <summary>Identifies the Temperature component instance.</summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>Provides the user-facing temperature-sensor name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Sets the Celsius change required to trigger a temperature report.</summary>
    [JsonPropertyName("report_thr_C")]
    public double ReportThresholdCelsius { get; init; }

    /// <summary>Applies a Celsius correction offset to measured temperature.</summary>
    [JsonPropertyName("offset_C")]
    public double OffsetCelsius { get; init; }

}

/// <summary>Configures voltage reporting, device-specific input range and optional value transformation.</summary>
public sealed record class VoltmeterConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "Voltmeter";

    /// <summary>Identifies the Voltmeter component instance.</summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>Provides the user-facing voltmeter name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Sets the voltage change required to trigger a report.</summary>
    [JsonPropertyName("report_thr")]
    public double ReportThreshold { get; init; }

    /// <summary>Selects the device-specific numeric voltage input range without introducing a CLR enum.</summary>
    [JsonPropertyName("range")]
    public byte Range { get; init; }

    /// <summary>Transforms measured voltage into a custom derived value and unit.</summary>
    [JsonPropertyName("xvoltage")]
    public ValueTransformationConfig? XVoltage { get; init; }

}

/// <summary>Controls optional Wi-Fi range-extender functionality.</summary>
public sealed record class WifiRangeExtenderConfig
{
    /// <summary>Determines whether range-extender functionality is enabled.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

}

/// <summary>Configures the device-hosted Wi-Fi access point without exposing write-only credentials.</summary>
public sealed record class WifiAccessPointConfig
{
    /// <summary>Sets the access-point SSID, or null to restore the device default.</summary>
    [JsonPropertyName("ssid")]
    public string? Ssid { get; init; }

    /// <summary>Reports whether the access point currently has no password.</summary>
    [JsonPropertyName("is_open")]
    public bool IsOpen { get; init; }

    /// <summary>Determines whether the access point is enabled.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

    /// <summary>Controls range-extender behavior on devices that provide it.</summary>
    [JsonPropertyName("range_extender")]
    public WifiRangeExtenderConfig? RangeExtender { get; init; }

}

/// <summary>Configures one Wi-Fi station profile without exposing its write-only password.</summary>
public sealed record class WifiStationConfig
{
    /// <summary>Sets the network SSID.</summary>
    [JsonPropertyName("ssid")]
    public string? Ssid { get; init; }

    /// <summary>Reports whether the configured network is open; this value is read-only.</summary>
    [JsonPropertyName("is_open")]
    public bool IsOpen { get; init; }

    /// <summary>Determines whether this station profile may be used.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

    /// <summary>Selects DHCP or static IPv4 configuration.</summary>
    [JsonPropertyName("ipv4mode")]
    public WifiStationIPv4Mode IPv4Mode { get; init; }

    /// <summary>Sets the static IPv4 address when static mode is selected.</summary>
    [JsonPropertyName("ip")]
    public string? Ip { get; init; }

    /// <summary>Sets the static network mask.</summary>
    [JsonPropertyName("netmask")]
    public string? Netmask { get; init; }

    /// <summary>Sets the static gateway.</summary>
    [JsonPropertyName("gw")]
    public string? Gateway { get; init; }

    /// <summary>Sets the static DNS resolver.</summary>
    [JsonPropertyName("nameserver")]
    public string? Nameserver { get; init; }

}

/// <summary>Controls when the Wi-Fi client scans and roams to a stronger access point.</summary>
public sealed record class WifiRoamConfig
{
    /// <summary>Sets the RSSI threshold that triggers a search for a better access point.</summary>
    [JsonPropertyName("rssi_thr")]
    public double RssiThreshold { get; init; }

    /// <summary>Sets the scan interval; zero disables roaming scans.</summary>
    [JsonPropertyName("interval")]
    public double Interval { get; init; }

}

/// <summary>Configures the device access point, primary/fallback Wi-Fi stations and roaming behavior.</summary>
public sealed record class WifiConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "WiFi";

    /// <summary>Configures the device-hosted access point.</summary>
    [JsonPropertyName("ap")]
    public WifiAccessPointConfig? AccessPoint { get; init; }

    /// <summary>Configures the primary upstream Wi-Fi network.</summary>
    [JsonPropertyName("sta")]
    public WifiStationConfig? Station { get; init; }

    /// <summary>Configures the fallback upstream Wi-Fi network.</summary>
    [JsonPropertyName("sta1")]
    public WifiStationConfig? FallbackStation { get; init; }

    /// <summary>Controls roaming between access points.</summary>
    [JsonPropertyName("roam")]
    public WifiRoamConfig? Roam { get; init; }

}

/// <summary>Configures the persistent outbound WebSocket RPC connection.</summary>
public sealed record class OutboundWsConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "Ws";

    /// <summary>Determines whether the outbound WebSocket connection is enabled.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

    /// <summary>Sets the WebSocket server endpoint; wss:// selects TLS.</summary>
    [JsonPropertyName("server")]
    public string? Server { get; init; }

    /// <summary>Selects how TLS server certificates are validated.</summary>
    [JsonPropertyName("ssl_ca")]
    public WsSslCa SslCa { get; init; }

}

/// <summary>Controls whether Zigbee connectivity is enabled on the device.</summary>
public sealed record class ZigbeeConfig : ShellyComponentConfigBase, IShellyComponentConfig
{
    public static string ComponentName => "Zigbee";

    /// <summary>Determines whether the Zigbee subsystem is enabled.</summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; init; }

}