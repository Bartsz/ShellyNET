using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using static NidoWeaver.Benchmark.Benchmarks.JsonBenchmarks.ShellyParser;

namespace NidoWeaver.Benchmark.Benchmarks.JsonBenchmarks;


//BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9445/25H2/2025Update/HudsonValley2)
//12th Gen Intel Core i7-1280P 2.00GHz, 1 CPU, 20 logical and 14 physical cores
//.NET SDK 11.0.100-preview.3.26207.106
//  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
//  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


//| Method                      | Mean     | Error   | StdDev  | Gen0   | Allocated |
//|---------------------------- |---------:|--------:|--------:|-------:|----------:|
//| ParseSwitch                 | 290.8 ns | 5.68 ns | 6.76 ns |      - |         - |
//| ParseSwitchUsingJsonContext | 606.9 ns | 8.75 ns | 8.18 ns | 0.0534 |     672 B |

[MemoryDiagnoser]
public class ShellyJsonBenchmark
{
    private byte[] _json = null!;
    private ShellyParser _parser = new ShellyParser();

    [GlobalSetup]
    public void Setup()
    {
        // Allocation happens once, outside the measured benchmark.
        //
        // Normalized to valid JSON:
        // - false instead of False
        // - no trailing commas
        var jsonPrettified =
            """
            {
                "ts": 835476242,
                "switch:0": {
                    "id": 0,
                    "source": "init",
                    "output": false,
                    "apower": 0.0,
                    "voltage": 237.0,
                    "temperature": {
                        "tC": 23.0,
                        "tF": 73.4
                    }
                },
                "switch:1": {
                    "id": 0,
                    "source": "init",
                    "output": false,
                    "apower": 0.0,
                    "voltage": 237.0,
                    "temperature": {
                        "tC": 23.0,
                        "tF": 73.4
                    }
                },
                "switch:2": {
                    "id": 0,
                    "source": "init",
                    "output": false,
                    "apower": 0.0,
                    "voltage": 237.0,
                    "temperature": {
                        "tC": 23.0,
                        "tF": 73.4
                    }
                }
            }
            """u8.ToArray();

        _json = Minify(jsonPrettified);
    }

    [Benchmark]
    public double ParseSwitch()
    {
        return _parser.Parse(_json);

        // Make all parsed values observable to the benchmark so that
        // the JIT cannot discard the work.
    }

    [Benchmark]
    public DeviceStatus ParseSwitchUsingJsonContext()
    {
        return JsonSerializer.Deserialize(_json, DeviceJsonContext.Default.DeviceStatus);
    }


    private static byte[] Minify(ReadOnlySpan<byte> json)
    {
        byte[] result = new byte[json.Length];
        int j = 0;

        foreach (byte b in json)
        {
            if (b is not (byte)' ' and not (byte)'\r' and not (byte)'\n' and not (byte)'\t')
                result[j++] = b;
        }

        Array.Resize(ref result, j);
        return result;
    }
}


public class ShellyParser
{
    private NumericPayload[] _payloads = new NumericPayload[10];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte Parse(ReadOnlySpan<byte> json)
    {
        var reader = new Utf8JsonReader(json);
        DateTimeOffset ts = default!;
        byte result = 0;
        Utf8JsonReader readerCopy = default;

        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
            ThrowInvalidJson();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                ThrowInvalidJson();

            var replaceReader = false;
            if (!reader.ValueSpan.SequenceEqual("ts"u8) && ts == default)
            {
                readerCopy = reader;
                replaceReader = true;

                while (true)
                {
                    reader.Read();
                    if (reader.ValueSpan == "ts"u8)
                    {
                        break;
                    }
                }
            }

            reader.Read(); // read unix epoch
            var tsUnix = ParseUnixSeconds(reader.ValueSpan);
            ts = DateTimeOffset.FromUnixTimeMilliseconds(tsUnix);
            reader.Read();
            if (replaceReader)
            {
                reader = readerCopy;
            }

            if (reader.ValueSpan == "ts"u8)
            {
                reader.Read();
                reader.Read();
            }

            if (reader.ValueSpan.StartsWith("switch"u8))
            {
                byte id = (byte)(reader.ValueSpan[^1] - '0');
                reader.Read();// read "{"
                result = ParseSwitch(ref reader, ts, id);
                reader.Read();
            }

            // Unknown top-level component.
            //
            // reader is currently positioned on PropertyName.
            // Skip() skips its value, including nested objects/arrays.

        }

        return result;
    }


    private DateTimeOffset ReaderTimestampProperty(ref Utf8JsonReader reader)
    {
        // read "{"
        //reader.Read();
        if (!reader.ValueSpan.SequenceEqual("ts"u8))
        {
            while (true)
            {
                reader.Read();
                if (reader.ValueSpan == "ts"u8)
                {
                    break;
                }
            }
        }

        reader.Read(); // read unix epoch
        var tsUnix = ParseUnixSeconds(reader.ValueSpan);
        reader.Read();
        return DateTimeOffset.FromUnixTimeMilliseconds(tsUnix);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte ParseSwitch(ref Utf8JsonReader reader, DateTimeOffset timestamp, byte id)
    {
        // reader is positioned at:  "switch:0": {
        //                                       ^ StartObject
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) ThrowInvalidJson();

            if (reader.ValueSpan.IsEmpty)
            {
                reader.Read();
                reader.Read();
            }

            // id
            if (reader.ValueTextEquals("id"u8))
            {
                reader.Read();
                reader.Read();
            }

            // source
            if (reader.ValueTextEquals("source"u8))
            {
                reader.Read();
                reader.Read();
            }

            var state = reader.CurrentState;
            var pos = reader.Position;
            var bytesRead = reader.BytesConsumed;

            // output
            if (reader.ValueTextEquals("output"u8))
            {
                reader.Read();
                var output = reader.GetBoolean();
                var p1 = new NumericPayload(timestamp, output ? 1 : 0, id, ComponentType.Switch, PropertyType.Output);
                _payloads[0] = p1;
                reader.Read();
            }
            // apower
            if (reader.ValueTextEquals("apower"u8))
            {
                reader.Read();
                var apower = reader.GetDouble();
                var p3 = new NumericPayload(timestamp, apower, id, ComponentType.Switch, PropertyType.AperentPower);
                _payloads[2] = p3;
                reader.Read();
            }
            // voltage
            if (reader.ValueTextEquals("voltage"u8))
            {
                reader.Read();
                var voltage = reader.GetDouble();
                var p2 = new NumericPayload(timestamp, voltage, id, ComponentType.Switch, PropertyType.Voltage);
                _payloads[1] = p2;
                reader.Read();
            }
            // temperature
            if (reader.ValueTextEquals("temperature"u8))
            {
                reader.Read();
                if (reader.TokenType != JsonTokenType.StartObject) ThrowInvalidJson();

                var temperatureC = ParseTemperature(ref reader);
                var p4 = new NumericPayload(timestamp, temperatureC, id, ComponentType.Switch, PropertyType.Temperature);
                _payloads[3] = p4;
                reader.Read();
            }
            // source and any other field we do not care about.

        }

        return (byte)(_payloads[0].Id + _payloads[1].Id + _payloads[2].Id + _payloads[3].Id);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double ParseTemperature(ref Utf8JsonReader reader)
    {
        double temperatureC = 0;

        // reader is positioned on temperature StartObject.
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) ThrowInvalidJson();

            ReadOnlySpan<byte> name = reader.ValueSpan;

            if (name.IsEmpty)
            {
                reader.Skip();
                continue;
            }

            if (reader.ValueTextEquals("tC"u8))
            {
                ReadNext(ref reader);

                temperatureC = reader.GetDouble();
            }
            else // tF and anything else.
            {
                reader.Skip();
            }
        }

        return temperatureC;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ReadNext(ref Utf8JsonReader reader)
    {
        if (!reader.Read()) ThrowInvalidJson();
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowInvalidJson()
    {
        throw new JsonException();
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static long ParseUnixSeconds(ReadOnlySpan<byte> value)
    {
        int dot = value.Length;

        if (dot >= 2 && value[^2] == (byte)'.') dot -= 2;
        else if (dot >= 3 && value[^3] == (byte)'.') dot -= 3;
        else if (dot >= 4 && value[^4] == (byte)'.') dot -= 4;

        ReadOnlySpan<byte> integerPart = value[..dot];

        int seconds = int.Parse(integerPart, NumberStyles.None, CultureInfo.InvariantCulture);
        int ms = 0;

        int fractionLength = value.Length - dot - 1;

        if (fractionLength > 0)
        {
            var fractionSpan = value[(dot + 1)..];
            ms = int.Parse(fractionSpan);
        }

        long epochMs = (long)seconds * 1000 + ms;
        return epochMs;
    }



    public partial struct NumericPayload
    {
        public NumericPayload(DateTimeOffset timestamp, double value, byte id, ComponentType componentType, PropertyType propertyType)
        {
            Timestamp = timestamp;
            Value = value;
            Id = id;
            ComponentType = componentType;
            PropertyType = propertyType;
        }

        public DateTimeOffset Timestamp { get; }
        public double Value { get; set; }
        public byte Id { get; }
        public ComponentType ComponentType { get; }
        public PropertyType PropertyType { get; }

        public bool AsBool => Value != 0;
    }

    public enum PropertyType : byte
    {
        Output,
        AperentPower,
        OnTime,
        Temperature,
        Brightness,
        TimerStatedAt,
        TimerDuration,
        Transition,
        Id,
        Voltage,
    }

    public enum ComponentType : byte
    {
        Camera,
        CameraZone,
        Streamer,
        Storage,
        BluetoothLowEnergy,
        CB,
        CCT,
        Cloud,
        Cover,
        DALI,
        DevicePower,
        EM,
        EM1,
        EM1Data,
        EMData,
        Ethernet,
        Flood,
        HTTP,
        Humidity,
        Illuminance,
        Input,
        KVS,
        Light,
        Matter,
        ModbusRTUClient,
        Modbus,
        MQTT,
        PM1,
        Pill,
        Presence,
        PresenceZone,
        RGB,
        RGBCCT,
        RGBW,
        Schedule,
        Script,
        Serial,
        Shelly,
        Smoke,
        Switch,
        System,
        Temperature,
        Voltmeter,
        Webhook,
        WiFi,
        OutboundWebsocket,
        XMOD,
        Zigbee,
    }


    public struct DeviceStatus
    {
        [JsonPropertyName("ts")]
        public long Timestamp { get; set; }

        [JsonPropertyName("switch:0")]
        public SwitchStatus Switch0 { get; set; }
    }

    public struct SwitchStatus
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonPropertyName("output")]
        public bool Output { get; set; }

        [JsonPropertyName("apower")]
        public double ActivePower { get; set; }

        [JsonPropertyName("voltage")]
        public double Voltage { get; set; }

        [JsonPropertyName("temperature")]
        public Temperature Temperature { get; set; }
    }

    public struct Temperature
    {
        [JsonPropertyName("tC")]
        public double Celsius { get; set; }

        [JsonPropertyName("tF")]
        public double Fahrenheit { get; set; }
    }

}


[JsonSerializable(typeof(DeviceStatus))]
public partial class DeviceJsonContext : JsonSerializerContext
{
}
