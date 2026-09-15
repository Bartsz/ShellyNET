using ShellyNET.Client.Model;

namespace ShellyNET.Client;

/// <summary> Parser assumes alphabetic order of keys which gives single pass parsing. </summary>
public class ShellyComponentsConfigParser
{
    public List<ShellyComponentConfigBase> ParseConfig(Memory<byte> payload)
    {
        var result = new List<ShellyComponentConfigBase>(64);
        var reader = new Utf8JsonReader(payload.Span);

        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.PropertyName) continue;

            if (reader.ValueSpan.StartsWith("ble"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.BLEConfig);
                if (config is BLEConfig bleConfig) result.Add(config);
                reader.Read();
            }
            while (reader.ValueSpan.StartsWith("camera:"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.CameraConfig);
                if (config is CameraConfig cameraConfig) result.Add(config);
                reader.Read();
            }
            while (reader.ValueSpan.StartsWith("camerazone:"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.CameraZoneConfig);
                if (config is CameraZoneConfig cameraZoneConfig) result.Add(config);
                reader.Read();
            }
            while (reader.ValueSpan.StartsWith("cb:"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.CBConfig);
                if (config is CBConfig cbConfig) result.Add(config);
                reader.Read();
            }
            while (reader.ValueSpan.StartsWith("cct:"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.CCTConfig);
                if (config is CCTConfig cctConfig) result.Add(config);
                reader.Read();
            }
            if (reader.ValueSpan.StartsWith("cloud"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.CloudConfig);
                if (config is CloudConfig cloudConfig) result.Add(config);
                reader.Read();
            }
            while (reader.ValueSpan.StartsWith("cover:"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.CoverConfig);
                if (config is CoverConfig coverConfig) result.Add(config);
                reader.Read();
            }
            //if (reader.ValueSpan.StartsWith("dali:"u8)) // DALI does not provide configuration
            while (reader.ValueSpan.StartsWith("devicepower:"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.DevicePowerConfig);
                if (config is DevicePowerConfig devicePowerConfig) result.Add(config);
                reader.Read();
            }
            while (reader.ValueSpan.StartsWith("em:"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.EMConfig);
                if (config is EMConfig emConfig) result.Add(config);
                reader.Read();
            }
            while (reader.ValueSpan.StartsWith("em1:"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.EM1Config);
                if (config is EM1Config em1Config) result.Add(config);
                reader.Read();
            }
            while (reader.ValueSpan.StartsWith("em1data:"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.EM1DataConfig);
                if (config is EM1DataConfig em1DataConfig) result.Add(config);
                reader.Read();
            }
            while (reader.ValueSpan.StartsWith("emdata:"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.EMDataConfig);
                if (config is EMDataConfig emDataConfig) result.Add(config);
                reader.Read();
            }
            if (reader.ValueSpan.StartsWith("eth"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.EthConfig);
                if (config is EthConfig ethernetConfig) result.Add(config);
                reader.Read();
            }
            while (reader.ValueSpan.StartsWith("flood:"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.FloodConfig);
                if (config is FloodConfig floodConfig) result.Add(config);
                reader.Read();
            }
            while (reader.ValueSpan.StartsWith("humidity:"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.HumidityConfig);
                if (config is HumidityConfig humidityConfig) result.Add(config);
                reader.Read();
            }
            while (reader.ValueSpan.StartsWith("illuminance:"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.IlluminanceConfig);
                if (config is IlluminanceConfig illuminanceConfig) result.Add(config);
                reader.Read();
            }
            while (reader.ValueSpan.StartsWith("input:"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.InputConfig);
                if (config is InputConfig inputConfig) result.Add(config);
                reader.Read();
            }

            while (reader.ValueSpan.StartsWith("light:"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.LightConfig);
                reader.Read();
                if (config is LightConfig lightConfig) result.Add(config);
            }
            if (reader.ValueSpan.StartsWith("matter"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.MatterConfig);
                if (config is MatterConfig matterConfig) result.Add(config);
                reader.Read();
            }
            if (reader.ValueSpan.StartsWith("modbus"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.ModbusConfig);
                if (config is ModbusConfig modbusConfig) result.Add(config);
                reader.Read();
            }
            while (reader.ValueSpan.StartsWith("mbrtuclient:"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.ModbusConfig);
                if (config is ModbusConfig modbusRTUClientConfig) result.Add(config);
                reader.Read();
            }
            if (reader.ValueSpan.StartsWith("mqtt"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.MQTTConfig);
                if (config is MQTTConfig mqttConfig) result.Add(config);
                reader.Read();
            }
            while (reader.ValueSpan.StartsWith("pill:"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.PillConfig);
                if (config is PillConfig pillConfig) result.Add(config);
                reader.Read();
            }
            while (reader.ValueSpan.StartsWith("pm1:"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.PM1Config);
                if (config is PM1Config pm1Config) result.Add(config);
                reader.Read();
            }
            while (reader.ValueSpan.StartsWith("presence:"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.PresenceConfig);
                if (config is PresenceConfig presenceConfig) result.Add(config);
                reader.Read();
            }
            while (reader.ValueSpan.StartsWith("presencezone:"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.PresenceZoneConfig);
                if (config is PresenceZoneConfig presenceZoneConfig) result.Add(config);
                reader.Read();
            }
            while (reader.ValueSpan.StartsWith("rgb:"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.RGBConfig);
                if (config is RGBConfig rgbConfig) result.Add(config);
                reader.Read();
            }
            while (reader.ValueSpan.StartsWith("rgbcct:"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.RGBCCTConfig);
                if (config is RGBCCTConfig rgbcctConfig) result.Add(config);
                reader.Read();
            }
            while (reader.ValueSpan.StartsWith("rgbw:"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.RGBWConfig);
                if (config is RGBWConfig rgbwConfig) result.Add(config);
                reader.Read();
            }
            while (reader.ValueSpan.StartsWith("script:"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.ScriptConfig);
                if (config is ScriptConfig scriptConfig) result.Add(config);
                reader.Read();
            }
            while (reader.ValueSpan.StartsWith("serial:"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.SerialConfig);
                if (config is SerialConfig serialConfig) result.Add(config);
                reader.Read();
            }
            while (reader.ValueSpan.StartsWith("smoke:"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.SmokeConfig);
                if (config is SmokeConfig smokeConfig) result.Add(config);
                reader.Read();
            }
            while (reader.ValueSpan.StartsWith("storage:"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.StorageConfig);
                if (config is StorageConfig storageConfig) result.Add(config);
                reader.Read();
            }
            while (reader.ValueSpan.StartsWith("switch:"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.SwitchConfig);
                if (config is SwitchConfig switchConfig) result.Add(config);
                reader.Read();
            }
            if (reader.ValueSpan.StartsWith("sys"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.SysConfig);
                if (config is SysConfig systemConfig) result.Add(config);
                reader.Read();
            }
            while (reader.ValueSpan.StartsWith("temperature:"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.TemperatureConfig);
                if (config is TemperatureConfig temperatureConfig) result.Add(config);
                reader.Read();
            }
            while (reader.ValueSpan.StartsWith("voltmeter:"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.VoltmeterConfig);
                if (config is VoltmeterConfig voltmeterConfig) result.Add(config);
                reader.Read();
            }
            if (reader.ValueSpan.StartsWith("wifi"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.WifiConfig);
                if (config is WifiConfig wiFiConfig) result.Add(config);
                reader.Read();
            }
            if (reader.ValueSpan.StartsWith("zigbee"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.ZigbeeConfig);
                if (config is ZigbeeConfig zigbeeConfig) result.Add(config);
                reader.Read();
            }
            if (reader.ValueSpan.StartsWith("ws"u8))
            {
                var config = JsonSerializer.Deserialize(ref reader, ShellyConfigsJsonSerializerContext.Default.OutboundWsConfig);
                if (config is OutboundWsConfig outboundWebsocketConfig) result.Add(config);
                reader.Read();
            }
        }

        return result;
    }
}
