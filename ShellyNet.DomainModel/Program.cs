using ShellyNET.Client;
using ShellyNET.Client.Model.Messages;
using System.Net;
using System.Text.Json;

namespace ShellyNet.DomainModel;

public static class Program
{
    private static byte[] _json = null!;


    public static async Task Main(string[] args)
    {
        var client = new ShellyClient();
        client.DisconnectedUnexpectedly += HandleDisconnected;
        client.UnsupportedMessageTypeReceived += HandleUnsupportedMessage;
        client.UnknownMessageReceived += HandleUnknownMessage;
        try
        {
            var ip = new IPAddress([192, 168, 1, 103]);
            await client.ConnectAsync(ip);
            var response = await client.GetDeviceInfoAsync();
            if(response.IsSuccess && response.Value is ShellyDeviceInfo info && info.AuthenticationEnabled == true)
            {
                await client.LoginToDeviceAsync("abcd");
            }
            var config = await client.GetDeviceConfigAsync();
            var status = await client.GetDeviceStatus();

            while (true)
            {
                await Task.Delay(1000);
            }


        }
        catch (Exception)
        {
            await client.DisconnectAsync();
        }

        await client.DisconnectAsync();


    }

    private static void HandleUnknownMessage(object? sender, ShellyUnknownMessageEventArgs e)
    {
        Console.WriteLine();
        Console.WriteLine("UnknownMessageRecieved:".PadLeft(10, '-').PadRight(10, '-'));
        Console.WriteLine(e.Payload);
        Console.WriteLine(e.Reason);
        Console.WriteLine("".PadLeft(30,'-'));
        Console.WriteLine();
    }

    private static void HandleUnsupportedMessage(object? sender, ShellyUnsupportedMessageTypeEventArgs e)
    {
        Console.WriteLine();
        Console.WriteLine("UnknownMessageRecieved:".PadLeft(10, '-').PadRight(10, '-'));
        Console.WriteLine(e.MessageLength);
        Console.WriteLine(e.MessageType.ToString());
        Console.WriteLine("".PadLeft(30, '-'));
        Console.WriteLine();
    }

    private static void HandleDisconnected(object? sender, ShellyUnexpectedDisconnectionEventArgs e)
    {
        Console.WriteLine("Disconnected");
    }

    private static void TestParsing()
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
                },
                "switch:3": {
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
            }
            """u8.ToArray();

        _json = Minify(jsonPrettified);

        Parse(_json);

        // Make all parsed values observable to the benchmark so that
        // the JIT cannot discard the work.
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

    private static NumericPayload[] _payloads = new NumericPayload[10];

    public static byte Parse(ReadOnlySpan<byte> json)
    {
        var reader = new Utf8JsonReader(json);
        DateTimeOffset ts = default!;
        byte result = 0;

        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
            ThrowInvalidJson();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                ThrowInvalidJson();

            if (ts == default)
            {
                var startOfParamsReader = reader;
                ts = ReaderTimestampProperty(ref reader);
                reader = startOfParamsReader;
            }
            if (reader.ValueSpan.StartsWith("ts"u8))
            {
                reader.Read();
                reader.Read();
            }
            if (reader.ValueSpan.StartsWith("switch"u8))
            {
                byte id = (byte)(reader.ValueSpan[^1] - '0');
                reader.Read();// read "{"
                reader.Read();// read "property name"
                result = ParseSwitch(ref reader, ts, id);
            }


        }

        return result;
    }


    private static DateTimeOffset ReaderTimestampProperty(ref Utf8JsonReader reader)
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

    private static byte ParseSwitch(ref Utf8JsonReader reader, DateTimeOffset timestamp, byte id)
    {
        // reader is positioned at:  "switch:0": {
        //                                       ^ StartObject
        while (reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) ThrowInvalidJson();

            if (reader.ValueSpan.IsEmpty)
            {
                reader.Skip();
                reader.Read();
            }

            // id
            if (reader.ValueTextEquals("id"u8))
            {
                //ReadNext(ref reader);
                reader.Read();
                reader.Read();
            }

            // source
            if (reader.ValueTextEquals("source"u8))
            {
                reader.Read();
                reader.Read();
            }

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
                ReadNext(ref reader);
                var apower = reader.GetDouble();
                var p3 = new NumericPayload(timestamp, apower, id, ComponentType.Switch, PropertyType.AperentPower);
                _payloads[2] = p3;
                reader.Read();
            }
            // voltage
            if (reader.ValueTextEquals("voltage"u8))
            {
                ReadNext(ref reader);
                var voltage = reader.GetDouble();
                var p2 = new NumericPayload(timestamp, voltage, id, ComponentType.Switch, PropertyType.Voltage);
                _payloads[1] = p2;
                reader.Read();
            }
            // temperature
            if (reader.ValueTextEquals("temperature"u8))
            {
                ReadNext(ref reader);
                if (reader.TokenType != JsonTokenType.StartObject) ThrowInvalidJson();

                var temperatureC = ParseTemperature(ref reader);
                var p4 = new NumericPayload(timestamp, temperatureC, id, ComponentType.Switch, PropertyType.Temperature);
                _payloads[3] = p4;
                //reader.Read();
            }
            // source and any other field we do not care about.
            if (reader.ValueSpan == "}"u8) break;
        }

        return (byte)(_payloads[0].Id + _payloads[1].Id + _payloads[2].Id + _payloads[3].Id);
    }


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


    private static void ReadNext(ref Utf8JsonReader reader)
    {
        if (!reader.Read()) ThrowInvalidJson();
    }


    private static void ThrowInvalidJson()
    {
        throw new JsonException();
        return;
    }


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



    public readonly partial struct NumericPayload
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
        public double Value { get; }
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

}
