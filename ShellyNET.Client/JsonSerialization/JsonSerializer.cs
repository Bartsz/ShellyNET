using ShellyNET.Client.Model;
using ShellyNET.Client.Model.Messages;
using System.Text.Json.Serialization;

namespace ShellyNET.Client;


/// <summary>Provides compile-time JSON metadata for all Shelly.GetConfig models, enabling reflection-free serialization and deserialization.</summary>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(CameraMotionSensitivity))]
[JsonSerializable(typeof(CameraNightVisionMode))]
[JsonSerializable(typeof(CameraAntiflickerMode))]
[JsonSerializable(typeof(CameraZoneType))]
[JsonSerializable(typeof(CCTInitialState))]
[JsonSerializable(typeof(CoverInputMode))]
[JsonSerializable(typeof(CoverInitialState))]
[JsonSerializable(typeof(CoverObstructionDirection))]
[JsonSerializable(typeof(CoverObstructionAction))]
[JsonSerializable(typeof(CoverSafetySwitchDirection))]
[JsonSerializable(typeof(CoverSafetySwitchAction))]
[JsonSerializable(typeof(CoverSafetySwitchAllowedMove))]
[JsonSerializable(typeof(EMBlinkModeSelector))]
[JsonSerializable(typeof(EMPhaseSelector))]
[JsonSerializable(typeof(EthIPv4Mode))]
[JsonSerializable(typeof(FloodAlarmMode))]
[JsonSerializable(typeof(InputType))]
[JsonSerializable(typeof(LightInputMode))]
[JsonSerializable(typeof(LightInitialState))]
[JsonSerializable(typeof(MQTTSslCa))]
[JsonSerializable(typeof(PillMode))]
[JsonSerializable(typeof(PillPin0Mode))]
[JsonSerializable(typeof(PillPin1Mode))]
[JsonSerializable(typeof(PillPin2Mode))]
[JsonSerializable(typeof(PresenceSensorPosition))]
[JsonSerializable(typeof(PresenceSensorPower))]
[JsonSerializable(typeof(PresenceSensorSensitivity))]
[JsonSerializable(typeof(RGBInputMode))]
[JsonSerializable(typeof(RGBInitialState))]
[JsonSerializable(typeof(RGBCCTInitialState))]
[JsonSerializable(typeof(RGBCCTNightModeMode))]
[JsonSerializable(typeof(RGBWInputMode))]
[JsonSerializable(typeof(RGBWInitialState))]
[JsonSerializable(typeof(SerialMode))]
[JsonSerializable(typeof(SwitchInputMode))]
[JsonSerializable(typeof(SwitchInitialState))]
[JsonSerializable(typeof(SysAddonType))]
[JsonSerializable(typeof(WifiStationIPv4Mode))]
[JsonSerializable(typeof(WsSslCa))]
[JsonSerializable(typeof(ShellyGetConfigResponse))]
[JsonSerializable(typeof(CameraLedConfig))]
[JsonSerializable(typeof(CameraAudioInputConfig))]
[JsonSerializable(typeof(CameraAudioOutputConfig))]
[JsonSerializable(typeof(CameraAudioConfig))]
[JsonSerializable(typeof(CameraSoundsConfig))]
[JsonSerializable(typeof(CameraMotionRecordingConfig))]
[JsonSerializable(typeof(CameraMotionConfig))]
[JsonSerializable(typeof(CameraNightVisionConfig))]
[JsonSerializable(typeof(CameraRtspConfig))]
[JsonSerializable(typeof(CameraVideoConfig))]
[JsonSerializable(typeof(CameraStreamConfig))]
[JsonSerializable(typeof(CameraConfig))]
[JsonSerializable(typeof(CameraZoneConfig))]
[JsonSerializable(typeof(StorageNfsConfig))]
[JsonSerializable(typeof(StorageConfig))]
[JsonSerializable(typeof(BLERpcConfig))]
[JsonSerializable(typeof(BLEObserverConfig))]
[JsonSerializable(typeof(BLEConfig))]
[JsonSerializable(typeof(CBConfig))]
[JsonSerializable(typeof(CCTNightModeConfig))]
[JsonSerializable(typeof(CCTButtonDoublePushConfig))]
[JsonSerializable(typeof(CCTButtonPresetsConfig))]
[JsonSerializable(typeof(CCTConfig))]
[JsonSerializable(typeof(CloudConfig))]
[JsonSerializable(typeof(CoverMotorConfig))]
[JsonSerializable(typeof(CoverObstructionDetectionConfig))]
[JsonSerializable(typeof(CoverSafetySwitchConfig))]
[JsonSerializable(typeof(CoverSlatConfig))]
[JsonSerializable(typeof(CoverConfig))]
[JsonSerializable(typeof(DevicePowerConfig))]
[JsonSerializable(typeof(EM1DataConfig))]
[JsonSerializable(typeof(EMDataConfig))]
[JsonSerializable(typeof(MbRtuClientConfig))]
[JsonSerializable(typeof(EMReverseConfig))]
[JsonSerializable(typeof(EMPhaseAlarmConfig))]
[JsonSerializable(typeof(EMAlarmsConfig))]
[JsonSerializable(typeof(EMConfig))]
[JsonSerializable(typeof(ElectricalAlarmsConfig))]
[JsonSerializable(typeof(EM1Config))]
[JsonSerializable(typeof(EthConfig))]
[JsonSerializable(typeof(FloodConfig))]
[JsonSerializable(typeof(HumidityConfig))]
[JsonSerializable(typeof(IlluminanceConfig))]
[JsonSerializable(typeof(ValueTransformationConfig))]
[JsonSerializable(typeof(InputConfig))]
[JsonSerializable(typeof(LightNightModeConfig))]
[JsonSerializable(typeof(LightButtonDoublePushConfig))]
[JsonSerializable(typeof(LightButtonPresetsConfig))]
[JsonSerializable(typeof(LightWarmupConfig))]
[JsonSerializable(typeof(LightConfig))]
[JsonSerializable(typeof(MatterConfig))]
[JsonSerializable(typeof(ModbusConfig))]
[JsonSerializable(typeof(MQTTConfig))]
[JsonSerializable(typeof(PM1Config))]
[JsonSerializable(typeof(PillConfig))]
[JsonSerializable(typeof(PresenceSensorStateConfig))]
[JsonSerializable(typeof(PresenceSensorConfig))]
[JsonSerializable(typeof(PresenceUiConfig))]
[JsonSerializable(typeof(PresenceNightModeConfig))]
[JsonSerializable(typeof(PresenceLedsConfig))]
[JsonSerializable(typeof(PresenceConfig))]
[JsonSerializable(typeof(PresenceZoneConfig))]
[JsonSerializable(typeof(RGBNightModeConfig))]
[JsonSerializable(typeof(RGBButtonDoublePushConfig))]
[JsonSerializable(typeof(RGBButtonPresetsConfig))]
[JsonSerializable(typeof(RGBConfig))]
[JsonSerializable(typeof(RGBCCTNightModeConfig))]
[JsonSerializable(typeof(RGBCCTConfig))]
[JsonSerializable(typeof(RGBWNightModeConfig))]
[JsonSerializable(typeof(RGBWButtonDoublePushConfig))]
[JsonSerializable(typeof(RGBWButtonPresetsConfig))]
[JsonSerializable(typeof(RGBWConfig))]
[JsonSerializable(typeof(ScriptConfig))]
[JsonSerializable(typeof(SerialPortConfig))]
[JsonSerializable(typeof(SerialModbusServerConfig))]
[JsonSerializable(typeof(SerialConfig))]
[JsonSerializable(typeof(SmokeConfig))]
[JsonSerializable(typeof(SwitchCountsConfig))]
[JsonSerializable(typeof(SwitchConfig))]
[JsonSerializable(typeof(SysDeviceConfig))]
[JsonSerializable(typeof(SysLocationConfig))]
[JsonSerializable(typeof(SysDebugToggleConfig))]
[JsonSerializable(typeof(SysDebugUdpConfig))]
[JsonSerializable(typeof(SysDebugConfig))]
[JsonSerializable(typeof(SysRpcUdpConfig))]
[JsonSerializable(typeof(SysSntpConfig))]
[JsonSerializable(typeof(SysConfig))]
[JsonSerializable(typeof(TemperatureConfig))]
[JsonSerializable(typeof(VoltmeterConfig))]
[JsonSerializable(typeof(WifiRangeExtenderConfig))]
[JsonSerializable(typeof(WifiAccessPointConfig))]
[JsonSerializable(typeof(WifiStationConfig))]
[JsonSerializable(typeof(WifiRoamConfig))]
[JsonSerializable(typeof(WifiConfig))]
[JsonSerializable(typeof(OutboundWsConfig))]
[JsonSerializable(typeof(ZigbeeConfig))]
public sealed partial class ShellyConfigsJsonSerializerContext : JsonSerializerContext;

/// <summary>Provides source-generated System.Text.Json metadata used by the Shelly client.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ShellyDeviceInfo))] 
[JsonSerializable(typeof(ShellyRpcError))]
[JsonSerializable(typeof(ShellyAuthChallenge))]
internal partial class ShellyJsonSerializerContext : JsonSerializerContext;



/// <summary>Normalizes Shelly nonce values to strings while accepting both legacy JSON numbers and firmware 2.0+ JSON strings.</summary>
internal sealed class ShellyNonceJsonConverter : JsonConverter<string>
{
    /// <summary>Reads either a numeric legacy nonce or a string nonce into one normalized string representation.</summary>
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String) return reader.GetString() ?? throw new JsonException("Authentication nonce cannot be null.");

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out var numericNonce))
        {
            return numericNonce.ToString(CultureInfo.InvariantCulture);
        }

        throw new JsonException("Authentication nonce must be a JSON number or string.");
    }

    /// <summary>Writes the normalized nonce as a JSON string; authentication request templates perform their own version-specific formatting.</summary>
    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}