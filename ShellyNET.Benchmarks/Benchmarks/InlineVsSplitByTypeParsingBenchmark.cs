using ShellyNET.Client;
using ShellyNET.Client.Model;
using System.Runtime.CompilerServices;
using System.Text.Json;

// This benchmark was created to test if refactoring to smaller functions will not throw performance away due to multiple function calls.
// There is marginal degradation at single property read, but it is defiinitely not worth the size of the code and readability loss.
// Most likely JIT is inlining all the functions and resuting machine code looks simillar?

//BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9445/25H2/2025Update/HudsonValley2)
//12th Gen Intel Core i7-1280P 2.00GHz, 1 CPU, 20 logical and 14 physical cores
//.NET SDK 11.0.100-preview.3.26207.106
//  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
//  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


//| Method                        | Variant    | Mean     | Error    | StdDev   | Allocated |
//|------------------------------ |----------- |---------:|---------:|---------:|----------:|
//| ParseInline                   | Full       | 889.9 ns | 13.17 ns | 12.32 ns |         - |
//| ParseUsingInlinedFunctions    | Full       | 893.7 ns | 11.99 ns | 10.63 ns |         - |
//| ParseUsingNotInlinedFunctions | Full       | 883.3 ns |  7.15 ns |  6.68 ns |         - |
//| ParseInline                   | SingleProp | 125.9 ns |  1.81 ns |  1.69 ns |         - |
//| ParseUsingInlinedFunctions    | SingleProp | 130.2 ns |  2.58 ns |  4.38 ns |         - |
//| ParseUsingNotInlinedFunctions | SingleProp | 133.6 ns |  1.37 ns |  1.28 ns |         - |

namespace ShellyNET.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class InlineVsSplitByTypeParsingBenchmark
{

    public enum JsonVariant { Full, SingleProp }

    [Params(JsonVariant.Full, JsonVariant.SingleProp)]
    public JsonVariant Variant { get; set; }

    private static DateTimeOffset timestamp = DateTimeOffset.UtcNow;
    private static InlineSwitchParamsParser inlineParser = new();
    private static SplitByFunctionSwitchParamsParserInlined byFunctionParseInlined = new();
    private static SplitByFunctionSwitchParamsParserNoInlining byFunctionParseNoInliniing = new();

    private byte[] _json;

    [GlobalSetup]
    public void Setup()
    {
        var fileName = Variant switch
        {
            JsonVariant.Full => "switch-object-only.json",
            JsonVariant.SingleProp => "switch-object-only-single-prop.json",
            _ => throw new ArgumentOutOfRangeException()
        };

        var path = Path.Combine(AppContext.BaseDirectory, "TestData", fileName);
        _json = File.ReadAllBytes(path);
    }

    [Benchmark]
    public bool? ParseInline()
    {
        Utf8JsonReader reader = new(_json.AsSpan());
        reader.Read();
        reader.Read();
        return inlineParser.ReadSwitchChanges(ref reader, timestamp);
    }

    [Benchmark]
    public bool? ParseUsingInlinedFunctions()
    {
        Utf8JsonReader reader = new(_json.AsSpan());
        reader.Read();
        reader.Read();
        return byFunctionParseInlined.ReadSwitchChanges(ref reader, timestamp);
    }

    [Benchmark]
    public bool? ParseUsingNotInlinedFunctions()
    {
        Utf8JsonReader reader = new(_json.AsSpan());
        reader.Read();
        reader.Read();
        return byFunctionParseNoInliniing.ReadSwitchChanges(ref reader, timestamp);
    }
}


internal class InlineSwitchParamsParser
{
    private readonly byte[] _errorsToReport = new byte[128]; 

    private static readonly ErrorDefinition[] _switchErrors = [
                            new("overtemp"u8.ToArray(), (byte)SwitchError.Overtemp),
                            new("overpower"u8.ToArray(), (byte)SwitchError.Overpower),
                            new("overvoltage"u8.ToArray(), (byte)SwitchError.Overvoltage),
                            new("undervoltage"u8.ToArray(), (byte)SwitchError.Undervoltage),
                            ];

    public bool? ReadSwitchChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        var idSpan = reader.ValueSpan[7..];
        var componentId = byte.Parse(idSpan);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                reader.Read();
                continue;
            }
            if (reader.ValueSpan.SequenceEqual("id"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            if (reader.ValueSpan.SequenceEqual("source"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            if (reader.ValueSpan.SequenceEqual("tag"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            if (reader.ValueSpan.SequenceEqual("output"u8))
            {
                reader.Read();
                EnqueueStatusMessage(new(timestamp, reader.GetBoolean(), componentId, ComponentType.Switch, PropertyName.Output));
                reader.Read();
                readProperty = true;
            }

            if (reader.ValueSpan.SequenceEqual("timer_started_at"u8))
            {
                reader.Read();
                EnqueueStatusMessage(new(timestamp, reader.GetInt64(), componentId, ComponentType.Switch, PropertyName.TimerStatedAt));
                reader.Read();
                readProperty = true;
            }
            if (reader.ValueSpan.SequenceEqual("timer_duration"u8))
            {
                reader.Read();
                EnqueueStatusMessage(new(timestamp, reader.GetInt64(), componentId, ComponentType.Switch, PropertyName.TimerStatedAt));
                reader.Read();
                readProperty = true;
            }
            if (reader.ValueSpan.SequenceEqual("apower"u8))
            {
                reader.Read();
                EnqueueStatusMessage(new(timestamp, reader.GetDouble(), componentId, ComponentType.Switch, PropertyName.ActivePower));
                reader.Read();
                readProperty = true;
            }
            if (reader.ValueSpan.SequenceEqual("voltage"u8))
            {
                reader.Read();
                EnqueueStatusMessage(new(timestamp, reader.GetDouble(), componentId, ComponentType.Switch, PropertyName.Voltage));
                reader.Read();
                readProperty = true;
            }
            if (reader.ValueSpan.SequenceEqual("current"u8))
            {
                reader.Read();
                EnqueueStatusMessage(new(timestamp, reader.GetDouble(), componentId, ComponentType.Switch, PropertyName.Voltage));
                reader.Read();
                readProperty = true;
            }
            if (reader.ValueSpan.SequenceEqual("pf"u8))
            {
                reader.Read();
                EnqueueStatusMessage(new(timestamp, reader.GetDouble(), componentId, ComponentType.Switch, PropertyName.PowerFactor));
                reader.Read();
                readProperty = true;
            }
            if (reader.ValueSpan.SequenceEqual("freq"u8))
            {
                reader.Read();
                EnqueueStatusMessage(new(timestamp, reader.GetDouble(), componentId, ComponentType.Switch, PropertyName.Frequency));
                reader.Read();
                readProperty = true;
            }
            if (reader.ValueSpan.SequenceEqual("aenergy"u8))
            {
                ReadActiveEnergyData(ref reader, out var total, out var lastMinute_mWh, out var minuteTs);
                if (total.HasValue) EnqueueStatusMessage(new(timestamp, total.Value, componentId, ComponentType.Switch, PropertyName.TotalEnergyConsumedWh));
                if (lastMinute_mWh.HasValue && minuteTs.HasValue)
                    EnqueueStatusMessage(new(DateTimeOffset.FromUnixTimeMilliseconds(minuteTs.Value), lastMinute_mWh.Value, componentId, ComponentType.Switch, PropertyName.LastMinuteEnergyConsumed_mWh));
                readProperty = true;
            }
            if (reader.ValueSpan.SequenceEqual("ret_aenergy"u8))
            {
                ReadActiveEnergyData(ref reader, out var total, out var lastMinute_mWh, out var minuteTs);
                if (total.HasValue) EnqueueStatusMessage(new(timestamp, total.Value, componentId, ComponentType.Switch, PropertyName.TotalEnergyReturnedWh));
                if (lastMinute_mWh.HasValue && minuteTs.HasValue)
                    EnqueueStatusMessage(new(DateTimeOffset.FromUnixTimeMilliseconds(minuteTs.Value), lastMinute_mWh.Value, componentId, ComponentType.Switch, PropertyName.LastMinuteEnergyReturned_mWh));
                readProperty = true;
            }

            if (reader.ValueSpan.SequenceEqual("counts"u8))
            {
                reader.Read(); // open object "{";
                while (true)
                {
                    bool readCountProperty = false;
                    if (reader.TokenType == JsonTokenType.EndObject) break;
                    if (reader.TokenType != JsonTokenType.PropertyName)
                    {
                        reader.Read();
                        continue;
                    }

                    if (reader.ValueSpan.SequenceEqual("on_time"u8))
                    {
                        reader.Read();
                        EnqueueStatusMessage(new(timestamp, reader.GetDouble(), componentId, ComponentType.Switch, PropertyName.TotalOnTimeMinutes));
                        reader.Read();
                        readCountProperty = true;
                    }
                    if (reader.ValueSpan.SequenceEqual("on_time_rst_ts"u8))
                    {
                        reader.Read();
                        var ts = ParseUnixTimestamp(reader.ValueSpan);
                        EnqueueStatusMessage(new(timestamp, ts, componentId, ComponentType.Switch, PropertyName.TotalOnTimeMinutes));
                        reader.Read();
                        readCountProperty = true;
                    }
                    if (reader.ValueSpan.SequenceEqual("switch_on"u8))
                    {
                        reader.Read();
                        EnqueueStatusMessage(new(timestamp, reader.GetDouble(), componentId, ComponentType.Switch, PropertyName.OffToOnCylesCount));
                        reader.Read();
                        readCountProperty = true;
                    }
                    if (reader.ValueSpan.SequenceEqual("switch_on_rst_ts"u8))
                    {
                        reader.Read();
                        var ts = ParseUnixTimestamp(reader.ValueSpan);
                        EnqueueStatusMessage(new(timestamp, ts, componentId, ComponentType.Switch, PropertyName.OffToOnCyckesCountLastResetTime));
                        reader.Read();
                        readCountProperty = true;
                    }
                    if (reader.ValueSpan.SequenceEqual("on_above_thr"u8))
                    {
                        reader.Read();
                        EnqueueStatusMessage(new(timestamp, reader.GetDouble(), componentId, ComponentType.Switch, PropertyName.TotalMinutesAbovePowerThreshold));
                        reader.Read();
                        readCountProperty = true;
                    }
                    if (reader.ValueSpan.SequenceEqual("on_above_thr_rst_ts"u8))
                    {
                        reader.Read();
                        var ts = ParseUnixTimestamp(reader.ValueSpan);
                        EnqueueStatusMessage(new(timestamp, ts, componentId, ComponentType.Switch, PropertyName.TotalMinutesAbovePowerThrLastResetTime));
                        reader.Read();
                        readCountProperty = true;
                    }
                    if (!readCountProperty)
                    {
                        if (!reader.Read()) return true;
                    }
                }
                reader.Read();
                readProperty = true;
            }
            if (reader.ValueSpan.SequenceEqual("temperature"u8))
            {
                if (TryParseTemperature(ref reader, out var tempC))
                {
                    EnqueueStatusMessage(new(timestamp, tempC, componentId, ComponentType.Switch, PropertyName.TemperatureC));
                }
                readProperty = true;
            }
            if (reader.ValueSpan.SequenceEqual("errors"u8))
            {
                var count = ReadErrors(ref reader, _switchErrors, _errorsToReport);

                for (int i = 0; i < count; i++)
                    EnqueueStatusMessage(new(timestamp, _errorsToReport[i], componentId, ComponentType.Switch, PropertyName.Error));
                readProperty = true;
            }

            if (!readProperty)
            {
                if (!reader.Read()) return true;
            }
        }

        reader.Read(); // was at "}"
        return null;
    }


    private void EnqueueStatusMessage(ShellyNotifyStatusMessage stateChange)
    {
    }

    /// <summary> Reads active energy values and moves reader outside the object. </summary>
    private static void ReadActiveEnergyData(ref Utf8JsonReader reader, out double? total, out double? lastMinute_mWh, out long? minuteTs)
    {
        total = null;
        lastMinute_mWh = null;
        minuteTs = null;

        reader.Read(); // now at "{"

        while (true)
        {
            bool readProperty = false;
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                reader.Read();
                continue;
            }

            if (reader.ValueSpan.SequenceEqual("total"u8))
            {
                reader.Read();
                total = reader.GetDouble();
                reader.Read();
                readProperty = true;
            }
            if (reader.ValueSpan.SequenceEqual("by_minute"u8))
            {
                reader.Read(); // opened array "[";
                reader.Read();
                if (reader.TokenType == JsonTokenType.Number)
                {
                    lastMinute_mWh = reader.GetDouble();
                    while (reader.TokenType != JsonTokenType.EndArray) { reader.Read(); }
                    reader.Read();
                }
                readProperty = true;

            }
            if (reader.ValueSpan.SequenceEqual("minute_ts"u8))
            {
                reader.Read();
                minuteTs = ParseUnixTimestamp(reader.ValueSpan);
                reader.Read();
                readProperty = true;
            }

            if (!readProperty)
            {
                if (!reader.Read()) return;
            }

        }

        reader.Read(); // reads pass the "}" to next property name
    }

    /// <summary>Converts a Shelly Unix timestamp to millisecond precision without temporary strings.</summary>
    private static long ParseUnixTimestamp(ReadOnlySpan<byte> value)
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
            ms = int.Parse(fractionSpan) * (fractionLength == 2 ? 10 : fractionLength == 1 ? 100 : 1);
        }

        long epochMs = (long)seconds * 1000 + ms;

        return epochMs;
    }


    /// <summary>Reads the Celsius temperature and consumes the complete temperature object before returning.</summary>
    private static bool TryParseTemperature(ref Utf8JsonReader reader, out double tempC)
    {
        var hasTemperature = false;
        tempC = default;

        while (reader.Read()) // move to "{" or to propertny name if caller has opened the object already
        {
            if (reader.TokenType == JsonTokenType.EndObject) return hasTemperature;
            if (reader.TokenType != JsonTokenType.PropertyName) continue;

            // Expected first: tC
            if (reader.ValueSpan.SequenceEqual("tC"u8))
            {
                reader.Read();

                if (reader.TokenType == JsonTokenType.Number)
                {
                    tempC = reader.GetDouble();
                }

                while (reader.TokenType != JsonTokenType.EndObject) reader.Read();
                reader.Read();// move pass "}";
                return true;
            }

            // Expected second: tF
            if (reader.TokenType == JsonTokenType.PropertyName && reader.ValueSpan.SequenceEqual("tF"u8))
            {
                reader.Read();// move to temp value
                var tempF = reader.GetDouble();
                tempC = (tempF - 32) * 5 / 9;
                hasTemperature = true; // prefere to read C so if it was out of order, let's try to find tC;

            }

            if (reader.TokenType == JsonTokenType.EndObject) return hasTemperature;
        }

        return hasTemperature;
    }


    private static byte ReadErrors(ref Utf8JsonReader reader, ErrorDefinition[] errorDefinitions, byte[] errors)
    {
        byte count = 0;
        reader.Read();
        while (reader.TokenType != JsonTokenType.EndArray)
        {
            var found = false;
            if (reader.TokenType == JsonTokenType.EndArray) break;

            foreach (var def in errorDefinitions)
            {
                if (reader.ValueSpan.SequenceEqual(def.TextSpan))
                {
                    errors[count] = def.ErrorEnum;
                    count++;
                    reader.Read();
                    found = true;
                }
            }

            if (!found)
            {
                errors[count] = 0; // has to be defined as Unknown
                count++;
                reader.Read();
            }

        }

        if (reader.TokenType == JsonTokenType.EndArray) reader.Read(); // read pass "]"

        return count;
    }


    private readonly struct ErrorDefinition(byte[] ErrorText, byte ErrorEnum)
    {
        public byte[] ErrorText { get; } = ErrorText;
        public byte ErrorEnum { get; } = ErrorEnum;
        public ReadOnlySpan<byte> TextSpan => ErrorText.AsSpan();
    }
}

internal class SplitByFunctionSwitchParamsParserNoInlining
{
    private readonly byte[] _errorsToReport = new byte[128];
    private readonly byte[] _flagsToReport = new byte[128];


    private static readonly ErrorDefinition[] _switchErrors = [
                            new("overtemp"u8.ToArray(), (byte)SwitchError.Overtemp),
                            new("overpower"u8.ToArray(), (byte)SwitchError.Overpower),
                            new("overvoltage"u8.ToArray(), (byte)SwitchError.Overvoltage),
                            new("undervoltage"u8.ToArray(), (byte)SwitchError.Undervoltage),
                            ];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool ReadBoolProperty(ref Utf8JsonReader reader, ReadOnlySpan<byte> proprtyName, DateTimeOffset timestamp, byte componentId, ComponentType componentTYpe, PropertyName property)
    {
        if (reader.ValueSpan.SequenceEqual(proprtyName))
        {
            reader.Read();
            EnqueueStatusMessage(new(timestamp, reader.GetBoolean(), componentId, componentTYpe, property));
            reader.Read();
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool ReadInt64Property(ref Utf8JsonReader reader, ReadOnlySpan<byte> proprtyName, DateTimeOffset timestamp, byte componentId, ComponentType componentTYpe, PropertyName property)
    {
        if (reader.ValueSpan.SequenceEqual(proprtyName))
        {
            reader.Read();
            EnqueueStatusMessage(new(timestamp, reader.GetInt64(), componentId, componentTYpe, property));
            reader.Read();
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool ReadDoubleProperty(ref Utf8JsonReader reader, ReadOnlySpan<byte> proprtyName, DateTimeOffset timestamp, byte componentId, ComponentType componentTYpe, PropertyName property)
    {
        if (reader.ValueSpan.SequenceEqual(proprtyName))
        {
            reader.Read();
            EnqueueStatusMessage(new(timestamp, reader.GetDouble(), componentId, componentTYpe, property));
            reader.Read();
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool ReadTimeStampProperty(ref Utf8JsonReader reader, ReadOnlySpan<byte> proprtyName, DateTimeOffset timestamp, byte componentId, ComponentType componentTYpe, PropertyName property)
    {
        if (reader.ValueSpan.SequenceEqual(proprtyName))
        {
            reader.Read();
            var ts = ParseUnixTimestamp(reader.ValueSpan);
            EnqueueStatusMessage(new(timestamp, ts, componentId, ComponentType.Switch, PropertyName.TotalMinutesAbovePowerThrLastResetTime));
            reader.Read();
            return true;
        }

        return false;
    }

    public bool? ReadSwitchChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        var idSpan = reader.ValueSpan[7..];
        var componentId = byte.Parse(idSpan);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                reader.Read();
                continue;
            }
            if (reader.ValueSpan.SequenceEqual("id"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            if (reader.ValueSpan.SequenceEqual("source"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            if (reader.ValueSpan.SequenceEqual("tag"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            readProperty |= ReadBoolProperty(ref reader, "output"u8, timestamp, componentId, ComponentType.Switch, PropertyName.Output);
            readProperty |= ReadInt64Property(ref reader, "timer_started_at"u8, timestamp, componentId, ComponentType.Switch, PropertyName.TimerStatedAt);
            readProperty |= ReadInt64Property(ref reader, "timer_duration"u8, timestamp, componentId, ComponentType.Switch, PropertyName.TimerDuration);
            readProperty |= ReadDoubleProperty(ref reader, "apower"u8, timestamp, componentId, ComponentType.Switch, PropertyName.ActivePower);
            readProperty |= ReadDoubleProperty(ref reader, "voltage"u8, timestamp, componentId, ComponentType.Switch, PropertyName.Voltage);
            readProperty |= ReadDoubleProperty(ref reader, "current"u8, timestamp, componentId, ComponentType.Switch, PropertyName.Current);
            readProperty |= ReadDoubleProperty(ref reader, "pf"u8, timestamp, componentId, ComponentType.Switch, PropertyName.PowerFactor);
            readProperty |= ReadDoubleProperty(ref reader, "freq"u8, timestamp, componentId, ComponentType.Switch, PropertyName.Frequency);

            if (reader.ValueSpan.SequenceEqual("aenergy"u8))
            {
                ReadActiveEnergyData(ref reader, out var total, out var lastMinute_mWh, out var minuteTs);
                if (total.HasValue) EnqueueStatusMessage(new(timestamp, total.Value, componentId, ComponentType.Switch, PropertyName.TotalEnergyConsumedWh));
                if (lastMinute_mWh.HasValue && minuteTs.HasValue)
                    EnqueueStatusMessage(new(DateTimeOffset.FromUnixTimeMilliseconds(minuteTs.Value), lastMinute_mWh.Value, componentId, ComponentType.Switch, PropertyName.LastMinuteEnergyConsumed_mWh));
                readProperty = true;
            }


            if (reader.ValueSpan.SequenceEqual("ret_aenergy"u8))
            {
                ReadActiveEnergyData(ref reader, out var total, out var lastMinute_mWh, out var minuteTs);
                if (total.HasValue) EnqueueStatusMessage(new(timestamp, total.Value, componentId, ComponentType.Switch, PropertyName.TotalEnergyReturnedWh));
                if (lastMinute_mWh.HasValue && minuteTs.HasValue)
                    EnqueueStatusMessage(new(DateTimeOffset.FromUnixTimeMilliseconds(minuteTs.Value), lastMinute_mWh.Value, componentId, ComponentType.Switch, PropertyName.LastMinuteEnergyReturned_mWh));
                readProperty = true;
            }

            if (reader.ValueSpan.SequenceEqual("counts"u8))
            {
                reader.Read(); // open object "{";
                while (true)
                {
                    bool readCountProperty = false;
                    if (reader.TokenType == JsonTokenType.EndObject) break;
                    if (reader.TokenType != JsonTokenType.PropertyName)
                    {
                        reader.Read();
                        continue;
                    }

                    readCountProperty |= ReadDoubleProperty(ref reader, "on_time"u8, timestamp, componentId, ComponentType.Switch, PropertyName.TotalOnTimeMinutes);
                    readCountProperty |= ReadTimeStampProperty(ref reader, "on_time_rst_ts"u8, timestamp, componentId, ComponentType.Switch, PropertyName.TotalOnTimeMinutes);
                    readCountProperty |= ReadDoubleProperty(ref reader, "switch_on"u8, timestamp, componentId, ComponentType.Switch, PropertyName.OffToOnCylesCount);
                    readCountProperty |= ReadTimeStampProperty(ref reader, "switch_on_rst_ts"u8, timestamp, componentId, ComponentType.Switch, PropertyName.OffToOnCyckesCountLastResetTime);
                    readCountProperty |= ReadDoubleProperty(ref reader, "on_above_thr"u8, timestamp, componentId, ComponentType.Switch, PropertyName.TotalMinutesAbovePowerThreshold);
                    readCountProperty |= ReadTimeStampProperty(ref reader, "on_above_thr_rst_ts"u8, timestamp, componentId, ComponentType.Switch, PropertyName.TotalMinutesAbovePowerThrLastResetTime);

                    if (!readCountProperty)
                    {
                        if (!reader.Read()) return true;
                    }
                }
                reader.Read();
                readProperty = true;
            }
            if (reader.ValueSpan.SequenceEqual("temperature"u8))
            {
                if (TryParseTemperature(ref reader, out var tempC))
                {
                    EnqueueStatusMessage(new(timestamp, tempC, componentId, ComponentType.Switch, PropertyName.TemperatureC));
                }
                readProperty = true;
            }
            if (reader.ValueSpan.SequenceEqual("errors"u8))
            {
                var count = ReadErrors(ref reader, _switchErrors, _errorsToReport);

                for (int i = 0; i < count; i++)
                    EnqueueStatusMessage(new(timestamp, (long)_errorsToReport[i], componentId, ComponentType.Switch, PropertyName.Error));
                readProperty = true;
            }

            if (!readProperty)
            {
                if (!reader.Read()) return true;
            }
        }

        reader.Read(); // was at "}"
        return null;
    }


    private void EnqueueStatusMessage(ShellyNotifyStatusMessage stateChange)
    {
    }

    /// <summary> Reads active energy values and moves reader outside the object. </summary>
    private static void ReadActiveEnergyData(ref Utf8JsonReader reader, out double? total, out double? lastMinute_mWh, out long? minuteTs)
    {
        total = null;
        lastMinute_mWh = null;
        minuteTs = null;

        reader.Read(); // now at "{"

        while (true)
        {
            bool readProperty = false;
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                reader.Read();
                continue;
            }

            if (reader.ValueSpan.SequenceEqual("total"u8))
            {
                reader.Read();
                total = reader.GetDouble();
                reader.Read();
                readProperty = true;
            }
            if (reader.ValueSpan.SequenceEqual("by_minute"u8))
            {
                reader.Read(); // opened array "[";
                reader.Read();
                if (reader.TokenType == JsonTokenType.Number)
                {
                    lastMinute_mWh = reader.GetDouble();
                    while (reader.TokenType != JsonTokenType.EndArray) { reader.Read(); }
                    reader.Read();
                }
                readProperty = true;

            }
            if (reader.ValueSpan.SequenceEqual("minute_ts"u8))
            {
                reader.Read();
                minuteTs = ParseUnixTimestamp(reader.ValueSpan);
                reader.Read();
                readProperty = true;
            }

            if (!readProperty)
            {
                if (!reader.Read()) return;
            }

        }

        reader.Read(); // reads pass the "}" to next property name
    }

    /// <summary>Converts a Shelly Unix timestamp to millisecond precision without temporary strings.</summary>
    private static long ParseUnixTimestamp(ReadOnlySpan<byte> value)
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
            ms = int.Parse(fractionSpan) * (fractionLength == 2 ? 10 : fractionLength == 1 ? 100 : 1);
        }

        long epochMs = (long)seconds * 1000 + ms;

        return epochMs;
    }


    /// <summary>Reads the Celsius temperature and consumes the complete temperature object before returning.</summary>
    private static bool TryParseTemperature(ref Utf8JsonReader reader, out double tempC)
    {
        var hasTemperature = false;
        tempC = default;

        while (reader.Read()) // move to "{" or to propertny name if caller has opened the object already
        {
            if (reader.TokenType == JsonTokenType.EndObject) return hasTemperature;
            if (reader.TokenType != JsonTokenType.PropertyName) continue;

            // Expected first: tC
            if (reader.ValueSpan.SequenceEqual("tC"u8))
            {
                reader.Read();

                if (reader.TokenType == JsonTokenType.Number)
                {
                    tempC = reader.GetDouble();
                }

                while (reader.TokenType != JsonTokenType.EndObject) reader.Read();
                reader.Read();// move pass "}";
                return true;
            }

            // Expected second: tF
            if (reader.TokenType == JsonTokenType.PropertyName && reader.ValueSpan.SequenceEqual("tF"u8))
            {
                reader.Read();// move to temp value
                var tempF = reader.GetDouble();
                tempC = (tempF - 32) * 5 / 9;
                hasTemperature = true; // prefere to read C so if it was out of order, let's try to find tC;

            }

            if (reader.TokenType == JsonTokenType.EndObject) return hasTemperature;
        }

        return hasTemperature;
    }


    private static byte ReadErrors(ref Utf8JsonReader reader, ErrorDefinition[] errorDefinitions, byte[] errors)
    {
        byte count = 0;
        reader.Read();
        while (reader.TokenType != JsonTokenType.EndArray)
        {
            var found = false;
            if (reader.TokenType == JsonTokenType.EndArray) break;

            foreach (var def in errorDefinitions)
            {
                if (reader.ValueSpan.SequenceEqual(def.TextSpan))
                {
                    errors[count] = def.ErrorEnum;
                    count++;
                    reader.Read();
                    found = true;
                }
            }

            if (!found)
            {
                errors[count] = 0; // has to be defined as Unknown
                count++;
                reader.Read();
            }

        }

        if (reader.TokenType == JsonTokenType.EndArray) reader.Read(); // read pass "]"

        return count;
    }


    private readonly struct ErrorDefinition(byte[] ErrorText, byte ErrorEnum)
    {
        public byte[] ErrorText { get; } = ErrorText;
        public byte ErrorEnum { get; } = ErrorEnum;
        public ReadOnlySpan<byte> TextSpan => ErrorText.AsSpan();
    }
}

internal class SplitByFunctionSwitchParamsParserInlined
{
    private readonly byte[] _errorsToReport = new byte[128];
    private readonly byte[] _flagsToReport = new byte[128];


    private static readonly ErrorDefinition[] _switchErrors = [
                            new("overtemp"u8.ToArray(), (byte)SwitchError.Overtemp),
                            new("overpower"u8.ToArray(), (byte)SwitchError.Overpower),
                            new("overvoltage"u8.ToArray(), (byte)SwitchError.Overvoltage),
                            new("undervoltage"u8.ToArray(), (byte)SwitchError.Undervoltage),
                            ];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ReadBoolProperty(ref Utf8JsonReader reader, ReadOnlySpan<byte> proprtyName, DateTimeOffset timestamp, byte componentId, ComponentType componentTYpe, PropertyName property)
    {
        if (reader.ValueSpan.SequenceEqual(proprtyName))
        {
            reader.Read();
            EnqueueStatusMessage(new(timestamp, reader.GetBoolean(), componentId, componentTYpe, property));
            reader.Read();
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ReadInt64Property(ref Utf8JsonReader reader, ReadOnlySpan<byte> proprtyName, DateTimeOffset timestamp, byte componentId, ComponentType componentTYpe, PropertyName property)
    {
        if (reader.ValueSpan.SequenceEqual(proprtyName))
        {
            reader.Read();
            EnqueueStatusMessage(new(timestamp, reader.GetInt64(), componentId, componentTYpe, property));
            reader.Read();
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ReadDoubleProperty(ref Utf8JsonReader reader, ReadOnlySpan<byte> proprtyName, DateTimeOffset timestamp, byte componentId, ComponentType componentTYpe, PropertyName property)
    {
        if (reader.ValueSpan.SequenceEqual(proprtyName))
        {
            reader.Read();
            EnqueueStatusMessage(new(timestamp, reader.GetDouble(), componentId, componentTYpe, property));
            reader.Read();
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ReadTimeStampProperty(ref Utf8JsonReader reader, ReadOnlySpan<byte> proprtyName, DateTimeOffset timestamp, byte componentId, ComponentType componentTYpe, PropertyName property)
    {
        if (reader.ValueSpan.SequenceEqual(proprtyName))
        {
            reader.Read();
            var ts = ParseUnixTimestamp(reader.ValueSpan);
            EnqueueStatusMessage(new(timestamp, ts, componentId, ComponentType.Switch, PropertyName.TotalMinutesAbovePowerThrLastResetTime));
            reader.Read();
            return true;
        }

        return false;
    }

    public bool? ReadSwitchChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        var idSpan = reader.ValueSpan[7..];
        var componentId = byte.Parse(idSpan);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                reader.Read();
                continue;
            }
            if (reader.ValueSpan.SequenceEqual("id"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            if (reader.ValueSpan.SequenceEqual("source"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            if (reader.ValueSpan.SequenceEqual("tag"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            readProperty |= ReadBoolProperty(ref reader, "output"u8, timestamp, componentId, ComponentType.Switch, PropertyName.Output);
            readProperty |= ReadInt64Property(ref reader, "timer_started_at"u8, timestamp, componentId, ComponentType.Switch, PropertyName.TimerStatedAt);
            readProperty |= ReadInt64Property(ref reader, "timer_duration"u8, timestamp, componentId, ComponentType.Switch, PropertyName.TimerDuration);
            readProperty |= ReadDoubleProperty(ref reader, "apower"u8, timestamp, componentId, ComponentType.Switch, PropertyName.ActivePower);
            readProperty |= ReadDoubleProperty(ref reader, "voltage"u8, timestamp, componentId, ComponentType.Switch, PropertyName.Voltage);
            readProperty |= ReadDoubleProperty(ref reader, "current"u8, timestamp, componentId, ComponentType.Switch, PropertyName.Current);
            readProperty |= ReadDoubleProperty(ref reader, "pf"u8, timestamp, componentId, ComponentType.Switch, PropertyName.PowerFactor);
            readProperty |= ReadDoubleProperty(ref reader, "freq"u8, timestamp, componentId, ComponentType.Switch, PropertyName.Frequency);

            if (reader.ValueSpan.SequenceEqual("aenergy"u8))
            {
                ReadActiveEnergyData(ref reader, out var total, out var lastMinute_mWh, out var minuteTs);
                if (total.HasValue) EnqueueStatusMessage(new(timestamp, total.Value, componentId, ComponentType.Switch, PropertyName.TotalEnergyConsumedWh));
                if (lastMinute_mWh.HasValue && minuteTs.HasValue)
                    EnqueueStatusMessage(new(DateTimeOffset.FromUnixTimeMilliseconds(minuteTs.Value), lastMinute_mWh.Value, componentId, ComponentType.Switch, PropertyName.LastMinuteEnergyConsumed_mWh));
                readProperty = true;
            }


            if (reader.ValueSpan.SequenceEqual("ret_aenergy"u8))
            {
                ReadActiveEnergyData(ref reader, out var total, out var lastMinute_mWh, out var minuteTs);
                if (total.HasValue) EnqueueStatusMessage(new(timestamp, total.Value, componentId, ComponentType.Switch, PropertyName.TotalEnergyReturnedWh));
                if (lastMinute_mWh.HasValue && minuteTs.HasValue)
                    EnqueueStatusMessage(new(DateTimeOffset.FromUnixTimeMilliseconds(minuteTs.Value), lastMinute_mWh.Value, componentId, ComponentType.Switch, PropertyName.LastMinuteEnergyReturned_mWh));
                readProperty = true;
            }

            if (reader.ValueSpan.SequenceEqual("counts"u8))
            {
                reader.Read(); // open object "{";
                while (true)
                {
                    bool readCountProperty = false;
                    if (reader.TokenType == JsonTokenType.EndObject) break;
                    if (reader.TokenType != JsonTokenType.PropertyName)
                    {
                        reader.Read();
                        continue;
                    }

                    readCountProperty |= ReadDoubleProperty(ref reader, "on_time"u8, timestamp, componentId, ComponentType.Switch, PropertyName.TotalOnTimeMinutes);
                    readCountProperty |= ReadTimeStampProperty(ref reader, "on_time_rst_ts"u8, timestamp, componentId, ComponentType.Switch, PropertyName.TotalOnTimeMinutes);
                    readCountProperty |= ReadDoubleProperty(ref reader, "switch_on"u8, timestamp, componentId, ComponentType.Switch, PropertyName.OffToOnCylesCount);
                    readCountProperty |= ReadTimeStampProperty(ref reader, "switch_on_rst_ts"u8, timestamp, componentId, ComponentType.Switch, PropertyName.OffToOnCyckesCountLastResetTime);
                    readCountProperty |= ReadDoubleProperty(ref reader, "on_above_thr"u8, timestamp, componentId, ComponentType.Switch, PropertyName.TotalMinutesAbovePowerThreshold);
                    readCountProperty |= ReadTimeStampProperty(ref reader, "on_above_thr_rst_ts"u8, timestamp, componentId, ComponentType.Switch, PropertyName.TotalMinutesAbovePowerThrLastResetTime);

                    if (!readCountProperty)
                    {
                        if (!reader.Read()) return true;
                    }
                }
                reader.Read();
                readProperty = true;
            }
            if (reader.ValueSpan.SequenceEqual("temperature"u8))
            {
                if (TryParseTemperature(ref reader, out var tempC))
                {
                    EnqueueStatusMessage(new(timestamp, tempC, componentId, ComponentType.Switch, PropertyName.TemperatureC));
                }
                readProperty = true;
            }
            if (reader.ValueSpan.SequenceEqual("errors"u8))
            {
                var count = ReadErrors(ref reader, _switchErrors, _errorsToReport);

                for (int i = 0; i < count; i++)
                    EnqueueStatusMessage(new(timestamp, (long)_errorsToReport[i], componentId, ComponentType.Switch, PropertyName.Error));
                readProperty = true;
            }

            if (!readProperty)
            {
                if (!reader.Read()) return true;
            }
        }

        reader.Read(); // was at "}"
        return null;
    }


    private void EnqueueStatusMessage(ShellyNotifyStatusMessage stateChange)
    {
    }

    /// <summary> Reads active energy values and moves reader outside the object. </summary>
    private static void ReadActiveEnergyData(ref Utf8JsonReader reader, out double? total, out double? lastMinute_mWh, out long? minuteTs)
    {
        total = null;
        lastMinute_mWh = null;
        minuteTs = null;

        reader.Read(); // now at "{"

        while (true)
        {
            bool readProperty = false;
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                reader.Read();
                continue;
            }

            if (reader.ValueSpan.SequenceEqual("total"u8))
            {
                reader.Read();
                total = reader.GetDouble();
                reader.Read();
                readProperty = true;
            }
            if (reader.ValueSpan.SequenceEqual("by_minute"u8))
            {
                reader.Read(); // opened array "[";
                reader.Read();
                if (reader.TokenType == JsonTokenType.Number)
                {
                    lastMinute_mWh = reader.GetDouble();
                    while (reader.TokenType != JsonTokenType.EndArray) { reader.Read(); }
                    reader.Read();
                }
                readProperty = true;

            }
            if (reader.ValueSpan.SequenceEqual("minute_ts"u8))
            {
                reader.Read();
                minuteTs = ParseUnixTimestamp(reader.ValueSpan);
                reader.Read();
                readProperty = true;
            }

            if (!readProperty)
            {
                if (!reader.Read()) return;
            }

        }

        reader.Read(); // reads pass the "}" to next property name
    }

    /// <summary>Converts a Shelly Unix timestamp to millisecond precision without temporary strings.</summary>
    private static long ParseUnixTimestamp(ReadOnlySpan<byte> value)
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
            ms = int.Parse(fractionSpan) * (fractionLength == 2 ? 10 : fractionLength == 1 ? 100 : 1);
        }

        long epochMs = (long)seconds * 1000 + ms;

        return epochMs;
    }


    /// <summary>Reads the Celsius temperature and consumes the complete temperature object before returning.</summary>
    private static bool TryParseTemperature(ref Utf8JsonReader reader, out double tempC)
    {
        var hasTemperature = false;
        tempC = default;

        while (reader.Read()) // move to "{" or to propertny name if caller has opened the object already
        {
            if (reader.TokenType == JsonTokenType.EndObject) return hasTemperature;
            if (reader.TokenType != JsonTokenType.PropertyName) continue;

            // Expected first: tC
            if (reader.ValueSpan.SequenceEqual("tC"u8))
            {
                reader.Read();

                if (reader.TokenType == JsonTokenType.Number)
                {
                    tempC = reader.GetDouble();
                }

                while (reader.TokenType != JsonTokenType.EndObject) reader.Read();
                reader.Read();// move pass "}";
                return true;
            }

            // Expected second: tF
            if (reader.TokenType == JsonTokenType.PropertyName && reader.ValueSpan.SequenceEqual("tF"u8))
            {
                reader.Read();// move to temp value
                var tempF = reader.GetDouble();
                tempC = (tempF - 32) * 5 / 9;
                hasTemperature = true; // prefere to read C so if it was out of order, let's try to find tC;

            }

            if (reader.TokenType == JsonTokenType.EndObject) return hasTemperature;
        }

        return hasTemperature;
    }


    private static byte ReadErrors(ref Utf8JsonReader reader, ErrorDefinition[] errorDefinitions, byte[] errors)
    {
        byte count = 0;
        reader.Read();
        while (reader.TokenType != JsonTokenType.EndArray)
        {
            var found = false;
            if (reader.TokenType == JsonTokenType.EndArray) break;

            foreach (var def in errorDefinitions)
            {
                if (reader.ValueSpan.SequenceEqual(def.TextSpan))
                {
                    errors[count] = def.ErrorEnum;
                    count++;
                    reader.Read();
                    found = true;
                }
            }

            if (!found)
            {
                errors[count] = 0; // has to be defined as Unknown
                count++;
                reader.Read();
            }

        }

        if (reader.TokenType == JsonTokenType.EndArray) reader.Read(); // read pass "]"

        return count;
    }


    private readonly struct ErrorDefinition(byte[] ErrorText, byte ErrorEnum)
    {
        public byte[] ErrorText { get; } = ErrorText;
        public byte ErrorEnum { get; } = ErrorEnum;
        public ReadOnlySpan<byte> TextSpan => ErrorText.AsSpan();
    }
}
