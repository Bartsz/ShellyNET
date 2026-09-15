using ShellyNET.Client.Model;
using System.Threading.Channels;
using System.Xml.Schema;

namespace ShellyNET.Client;

public partial class ShellyClient
{
    public async ValueTask<ShellyStateChangeMsg> GetShellyStatusMessage(CancellationToken ct)
    {
        while (true)
        {
            return await _stateChangesChannel.Reader.ReadAsync(ct).ConfigureAwait(false);
        }
    }
    // for testing purpose
    internal int MessagesInChannel => _stateChangesChannel.Reader.Count;
    private readonly Channel<ShellyStateChangeMsg> _stateChangesChannel = Channel.CreateBounded<ShellyStateChangeMsg>(new BoundedChannelOptions(128)
    {
        AllowSynchronousContinuations = false,
        SingleReader = true,
        SingleWriter = true,
        FullMode = BoundedChannelFullMode.Wait
    });

    private readonly byte[] _errorsToReport = new byte[128];
    private readonly ShellyStateChangeMsg[] _pendingStateUpdates = new ShellyStateChangeMsg[128];
    private byte _pendingMessagesCount = 0;

    private static readonly ErrorDefinition[] _switchErrors = [
                            new("overtemp"u8.ToArray(), (byte)SwitchError.Overtemp),
                            new("overpower"u8.ToArray(), (byte)SwitchError.Overpower),
                            new("overvoltage"u8.ToArray(), (byte)SwitchError.Overvoltage),
                            new("undervoltage"u8.ToArray(), (byte)SwitchError.Undervoltage),
                            ];

    private static readonly ErrorDefinition[] _coverErrors = [
                            new("overtemp"u8.ToArray(), (byte)CoverError.Overtemp),
                            new("overpower"u8.ToArray(), (byte)CoverError.Overpower),
                            new("overvoltage"u8.ToArray(), (byte)CoverError.Overvoltage),
                            new("undervoltage"u8.ToArray(), (byte)CoverError.Undervoltage),
                            new("overcurrent"u8.ToArray(), (byte)CoverError.Overcurrent),
                            new("obstruction"u8.ToArray(), (byte)CoverError.Obstruction),
                            new("safety_switch"u8.ToArray(), (byte)CoverError.SafetySwitch),
                            new("bad_feedback:rotating_in_wrong_direction"u8.ToArray(),(byte)CoverError.BadFeedbackRotatingInWrongDirection),
                            new("bad_feedback:both_directions_active"u8.ToArray(),(byte)CoverError.BadFeedbackBothDirectionsActive),
                            new("bad_feedback:failed_to_halt"u8.ToArray(),(byte)CoverError.BadFeedbackFailedToHalt),
                            new("cal_abort:timeout_open"u8.ToArray(), (byte)CoverError.CalAbortTimeoutOpen),
                            new("cal_abort:timeout_close"u8.ToArray(), (byte)CoverError.CalAbortTimeoutClose),
                            new("cal_abort:safety"u8.ToArray(), (byte)CoverError.CalAbortSafety),
                            new("cal_abort:ext_command"u8.ToArray(), (byte)CoverError.CalAbortExtCommand),
                            new("cal_abort:bad_feedback"u8.ToArray(), (byte)CoverError.CalAbortBadFeedback),
                            new("cal_abort:implausible_time_to_fully_close"u8.ToArray(),(byte)CoverError.CalAbortImplausibleTimeToFullyClose),
                            new("cal_abort:implausible_time_to_fully_open"u8.ToArray(),(byte)CoverError.CalAbortImplausibleTimeToFullyOpen),
                            new("cal_abort:implausible_power_consumption_in_close_dir"u8.ToArray(), (byte)CoverError.CalAbortImplausiblePowerConsumptionInCloseDir),
                            new("cal_abort:implausible_power_consumption_in_open_dir"u8.ToArray(), (byte)CoverError.CalAbortImplausiblePowerConsumptionInOpenDir),
                            new("cal_abort:too_many_steps_to_close"u8.ToArray(), (byte)CoverError.CalAbortTooManyStepsToClose),
                            new("cal_abort:too_few_steps_to_close"u8.ToArray(), (byte)CoverError.CalAbortTooFewStepsToClose),
                            new("cal_abort:implausible_time_to_fully_close_w_steps"u8.ToArray(), (byte)CoverError.CalAbortImplausibleTimeToFullyCloseWithSteps),
                            new("cal_abort:implausible_step_duration_in_open_dir"u8.ToArray(), (byte)CoverError.CalAbortImplausibleStepDurationInOpenDir),
                            new("cal_abort:too_many_steps_to_open"u8.ToArray(), (byte)CoverError.CalAbortTooManyStepsToOpen),
                            new("cal_abort:too_few_steps_to_open"u8.ToArray(), (byte)CoverError.CalAbortTooFewStepsToOpen),
                            new("cal_abort:implausible_time_to_fully_open_w_steps"u8.ToArray(), (byte)CoverError.CalAbortImplausibleTimeToFullyOpenWithSteps)
                        ];

    /// <summary>Processes a Shelly NotifyStatus frame using the expected property order as the fast path.</summary>
    internal void ProcessNotifyStatusMessage(Memory<byte> message, CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return;

        var reader = new Utf8JsonReader(message.Span);
        DateTimeOffset timestamp = default;

        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return;
        if (!reader.Read()) return;
        OpenParamsObject(ref reader); // after function reader is at "{" of the params which works with while loop
        try
        {

            while (reader.Read())
            {
                if (timestamp == default)
                {
                    if (reader.ValueSpan.SequenceEqual("ts"u8))
                    {
                        reader.Read();
                        var unixEpochMs = ParseUnixTimestamp(reader.ValueSpan);
                        timestamp = DateTimeOffset.FromUnixTimeMilliseconds(unixEpochMs);
                        reader.Read();
                    }
                    else
                    {
                        // it will internally make a copy and leave original reader intact.
                        timestamp = FindAndParseTimestamp(ref reader);
                    }
                }


                while (reader.ValueSpan.StartsWith("cover:"u8))
                {
                    var idSpan = reader.ValueSpan[7..];
                    var componentId = byte.Parse(idSpan);
                    if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) continue;

                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndObject) break;
                        if (reader.TokenType != JsonTokenType.PropertyName) continue;
                        if (reader.ValueSpan.SequenceEqual("id"u8)) reader.Skip();
                        if (reader.ValueSpan.SequenceEqual("source"u8)) reader.Skip();
                        if (reader.ValueSpan.SequenceEqual("tag"u8)) reader.Skip();

                        if (reader.ValueSpan.SequenceEqual("output"u8))
                        {
                            reader.Read();
                            EnqueueStatusMessage(new(timestamp, reader.GetBoolean() ? 1 : 0, componentId, ComponentType.Cover, PropertyName.Output));
                            reader.Read();
                        }
                        if (reader.ValueSpan.SequenceEqual("apower"u8))
                        {
                            reader.Read();
                            EnqueueStatusMessage(new(timestamp, reader.GetDouble(), componentId, ComponentType.Cover, PropertyName.AperentPower));
                            reader.Read();
                        }
                        if (reader.ValueSpan.SequenceEqual("voltage"u8))
                        {
                            reader.Read();
                            EnqueueStatusMessage(new(timestamp, reader.GetDouble(), componentId, ComponentType.Cover, PropertyName.Voltage));
                            reader.Read();
                        }
                        if (reader.ValueSpan.SequenceEqual("current"u8))
                        {
                            reader.Read();
                            EnqueueStatusMessage(new(timestamp, reader.GetDouble(), componentId, ComponentType.Cover, PropertyName.Current));
                            reader.Read();
                        }
                        if (reader.ValueSpan.SequenceEqual("pf"u8))
                        {
                            reader.Read();
                            EnqueueStatusMessage(new(timestamp, reader.GetDouble(), componentId, ComponentType.Cover, PropertyName.PowerFactor));
                            reader.Read();
                        }
                        if (reader.ValueSpan.SequenceEqual("freq"u8))
                        {
                            reader.Read();
                            EnqueueStatusMessage(new(timestamp, reader.GetDouble(), componentId, ComponentType.Cover, PropertyName.Frequency));
                            reader.Read();
                        }
                        if (reader.ValueSpan.SequenceEqual("aenergy"u8))
                        {
                            reader.Read();
                            ReadActiveEnergyData(ref reader, out var total, out var lastMinute_mWh, out var minuteTs);
                            if (total.HasValue) EnqueueStatusMessage(new(timestamp, total.Value, componentId, ComponentType.Cover, PropertyName.TotalEnergyConsumedWh));
                            if (lastMinute_mWh.HasValue && minuteTs.HasValue)
                                EnqueueStatusMessage(new(DateTimeOffset.FromFileTime(minuteTs.Value), lastMinute_mWh.Value, componentId, ComponentType.Cover, PropertyName.TotalEnergyConsumedWh));
                        }
                        if (reader.ValueSpan.SequenceEqual("current_pos"u8))
                        {
                            reader.Read();
                            if (reader.TokenType == JsonTokenType.Null)
                                EnqueueStatusMessage(new(timestamp, null, componentId, ComponentType.Cover, PropertyName.CoverCurrentPosition));
                            else
                                EnqueueStatusMessage(new(timestamp, reader.GetDouble(), componentId, ComponentType.Cover, PropertyName.CoverCurrentPosition));
                            reader.Read();
                        }
                        if (reader.ValueSpan.SequenceEqual("target_pos"u8))
                        {
                            reader.Read();
                            if (reader.TokenType == JsonTokenType.Null)
                                EnqueueStatusMessage(new(timestamp, null, componentId, ComponentType.Cover, PropertyName.CoverTargetPosition));
                            else
                                EnqueueStatusMessage(new(timestamp, reader.GetDouble(), componentId, ComponentType.Cover, PropertyName.CoverTargetPosition));
                            reader.Read();
                        }
                        if (reader.ValueSpan.SequenceEqual("move_timeout"u8))
                        {
                            reader.Read();
                            EnqueueStatusMessage(new(timestamp, reader.GetInt32(), componentId, ComponentType.Cover, PropertyName.CoverMoveTimeoutSeconds));
                            reader.Read();
                        }
                        if (reader.ValueSpan.SequenceEqual("move_started_at"u8))
                        {
                            reader.Read();
                            var ts = ParseUnixTimestamp(reader.ValueSpan);
                            EnqueueStatusMessage(new(timestamp, ts, componentId, ComponentType.Cover, PropertyName.CoverMoveStartedAUnixMs));
                            reader.Read();
                        }
                        if (reader.ValueSpan.SequenceEqual("pos_control"u8))
                        {
                            reader.Read();
                            var hasControl = reader.GetBoolean() ? 1 : 0;
                            EnqueueStatusMessage(new(timestamp, hasControl, componentId, ComponentType.Cover, PropertyName.CoverHasPositionControl));
                            reader.Read();
                        }
                        if (reader.ValueSpan.SequenceEqual("last_direction"u8))
                        {
                            reader.Read();
                            if (reader.TokenType == JsonTokenType.Null)
                                EnqueueStatusMessage(new(timestamp, (long)CoverLastDirection.Unkown, componentId, ComponentType.Cover, PropertyName.CoverLastDirection));
                            else
                            {
                                var lastDirection = reader.GetString() == "open" ? CoverLastDirection.Open : CoverLastDirection.Close;
                                EnqueueStatusMessage(new(timestamp, (long)lastDirection, componentId, ComponentType.Cover, PropertyName.CoverLastDirection));
                            }
                            reader.Read();
                        }
                        if (reader.ValueSpan.SequenceEqual("temperature"u8))
                        {
                            if (TryParseTemperature(ref reader, out var tempC))
                            {
                                EnqueueStatusMessage(new(timestamp, tempC, componentId, ComponentType.Switch, PropertyName.TemperatureC));
                            }
                        }
                        if (reader.ValueSpan.SequenceEqual("slat_pos"u8))
                        {
                            reader.Read();
                            if (reader.TokenType == JsonTokenType.Null)
                                EnqueueStatusMessage(new(timestamp, null, componentId, ComponentType.Cover, PropertyName.CoverSlatPosition));
                            else
                                EnqueueStatusMessage(new(timestamp, reader.GetDouble(), componentId, ComponentType.Cover, PropertyName.CoverSlatPosition));
                            reader.Read();
                        }
                        if (reader.ValueSpan.SequenceEqual("errors"u8))
                        {
                            var count = ReadErrors(ref reader, _coverErrors, _errorsToReport);

                            for (int i = 0; i < count; i++)
                                EnqueueStatusMessage(new(timestamp, (long)_errorsToReport[i], componentId, ComponentType.Switch, PropertyName.Error));
                        }
                    }

                    reader.Read(); // was at "}"
                }

                while (reader.ValueSpan.StartsWith("switch:"u8))
                {
                    var idSpan = reader.ValueSpan[7..];
                    var componentId = byte.Parse(idSpan);
                    if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) continue;

                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndObject) break;
                        if (reader.TokenType != JsonTokenType.PropertyName) continue;
                        if (reader.ValueSpan.SequenceEqual("id"u8)) reader.Read(); reader.Read();
                        if (reader.ValueSpan.SequenceEqual("source"u8)) reader.Read(); reader.Read();
                        if (reader.ValueSpan.SequenceEqual("tag"u8)) reader.Read(); reader.Read();
                        if (reader.ValueSpan.SequenceEqual("output"u8))
                        {
                            reader.Read();
                            EnqueueStatusMessage(new(timestamp, reader.GetBoolean() ? 1 : 0, componentId, ComponentType.Switch, PropertyName.Output));
                            reader.Read();
                        }
                        if (reader.ValueSpan.SequenceEqual("timer_started_at"u8))
                        {
                            reader.Read();
                            EnqueueStatusMessage(new(timestamp, reader.GetInt64(), componentId, ComponentType.Switch, PropertyName.TimerStatedAt));
                            reader.Read();
                        }
                        if (reader.ValueSpan.SequenceEqual("timer_duration"u8))
                        {
                            reader.Read();
                            EnqueueStatusMessage(new(timestamp, reader.GetInt64(), componentId, ComponentType.Switch, PropertyName.TimerStatedAt));
                            reader.Read();
                        }
                        if (reader.ValueSpan.SequenceEqual("apower"u8))
                        {
                            reader.Read();
                            EnqueueStatusMessage(new(timestamp, reader.GetDouble(), componentId, ComponentType.Switch, PropertyName.AperentPower));
                            reader.Read();
                        }
                        if (reader.ValueSpan.SequenceEqual("voltage"u8))
                        {
                            reader.Read();
                            EnqueueStatusMessage(new(timestamp, reader.GetDouble(), componentId, ComponentType.Switch, PropertyName.Voltage));
                            reader.Read();
                        }
                        if (reader.ValueSpan.SequenceEqual("current"u8))
                        {
                            reader.Read();
                            EnqueueStatusMessage(new(timestamp, reader.GetDouble(), componentId, ComponentType.Switch, PropertyName.Voltage));
                            reader.Read();
                        }
                        if (reader.ValueSpan.SequenceEqual("pf"u8))
                        {
                            reader.Read();
                            EnqueueStatusMessage(new(timestamp, reader.GetDouble(), componentId, ComponentType.Switch, PropertyName.PowerFactor));
                            reader.Read();
                        }
                        if (reader.ValueSpan.SequenceEqual("freq"u8))
                        {
                            reader.Read();
                            EnqueueStatusMessage(new(timestamp, reader.GetDouble(), componentId, ComponentType.Switch, PropertyName.Frequency));
                            reader.Read();
                        }
                        if (reader.ValueSpan.SequenceEqual("aenergy"u8))
                        {
                            //reader.Read();
                            ReadActiveEnergyData(ref reader, out var total, out var lastMinute_mWh, out var minuteTs);
                            if (total.HasValue) EnqueueStatusMessage(new(timestamp, total.Value, componentId, ComponentType.Switch, PropertyName.TotalEnergyConsumedWh));
                            if (lastMinute_mWh.HasValue && minuteTs.HasValue)
                                EnqueueStatusMessage(new(DateTimeOffset.FromFileTime(minuteTs.Value), lastMinute_mWh.Value, componentId, ComponentType.Switch, PropertyName.TotalEnergyConsumedWh));
                        }
                        if (reader.ValueSpan.SequenceEqual("ret_aenergy"u8))
                        {
                            reader.Read();
                            ReadActiveEnergyData(ref reader, out var total, out var lastMinute_mWh, out var minuteTs);
                            if (total.HasValue) EnqueueStatusMessage(new(timestamp, total.Value, componentId, ComponentType.Switch, PropertyName.TotalEnergyReturnedWh));
                            if (lastMinute_mWh.HasValue && minuteTs.HasValue)
                                EnqueueStatusMessage(new(DateTimeOffset.FromFileTime(minuteTs.Value), lastMinute_mWh.Value, componentId, ComponentType.Switch, PropertyName.LastMinuteEnergyReturned_mWh));
                        }

                        if (reader.ValueSpan.SequenceEqual("counts"u8))
                        {
                            reader.Read(); // open object "{";
                            while (true)
                            {
                                bool readProperty = false;
                                if (reader.TokenType == JsonTokenType.EndObject) break;
                                if (reader.TokenType != JsonTokenType.PropertyName)
                                {
                                    reader.Read();
                                    continue;
                                }

                                if (reader.ValueSpan.SequenceEqual("on_time"u8))
                                {
                                    reader.Read();
                                    EnqueueStatusMessage(new(timestamp, reader.GetInt32(), componentId, ComponentType.Switch, PropertyName.TotalOnTimeMinutes));
                                    reader.Read();
                                    readProperty = true;
                                }
                                if (reader.ValueSpan.SequenceEqual("on_time_rst_ts"u8))
                                {
                                    reader.Read();
                                    var ts = ParseUnixTimestamp(reader.ValueSpan);
                                    EnqueueStatusMessage(new(timestamp, ts, componentId, ComponentType.Switch, PropertyName.TotalOnTimeMinutes));
                                    reader.Read();
                                    readProperty = true;
                                }
                                if (reader.ValueSpan.SequenceEqual("switch_on"u8))
                                {
                                    reader.Read();
                                    EnqueueStatusMessage(new(timestamp, reader.GetInt32(), componentId, ComponentType.Switch, PropertyName.OffToOnCylesCount));
                                    reader.Read();
                                    readProperty = true;
                                }
                                if (reader.ValueSpan.SequenceEqual("switch_on_rst_ts"u8))
                                {
                                    reader.Read();
                                    var ts = ParseUnixTimestamp(reader.ValueSpan);
                                    EnqueueStatusMessage(new(timestamp, ts, componentId, ComponentType.Switch, PropertyName.LastOffToOnCyckesCountReset));
                                    reader.Read();
                                    readProperty = true;
                                }
                                if (reader.ValueSpan.SequenceEqual("on_above_thr"u8))
                                {
                                    reader.Read();
                                    EnqueueStatusMessage(new(timestamp, reader.GetInt32(), componentId, ComponentType.Switch, PropertyName.TotalMinutesAbovePowerThreshold));
                                    reader.Read();
                                    readProperty = true;
                                }
                                if (reader.ValueSpan.SequenceEqual("on_above_thr_rst_ts"u8))
                                {
                                    reader.Read();
                                    var ts = ParseUnixTimestamp(reader.ValueSpan);
                                    EnqueueStatusMessage(new(timestamp, ts, componentId, ComponentType.Switch, PropertyName.LastTotalMinutesAbovePowerThrReset));
                                    reader.Read();
                                    readProperty = true;
                                }
                                if (!readProperty)
                                {
                                    if (!reader.Read()) return;
                                }
                            }
                            reader.Read();
                        }
                        if (reader.ValueSpan.SequenceEqual("temperature"u8))
                        {
                            if (TryParseTemperature(ref reader, out var tempC))
                            {
                                EnqueueStatusMessage(new(timestamp, tempC, componentId, ComponentType.Switch, PropertyName.TemperatureC));
                            }
                        }
                        if (reader.ValueSpan.SequenceEqual("errors"u8))
                        {
                            var count = ReadErrors(ref reader, _switchErrors, _errorsToReport);

                            for (int i = 0; i < count; i++)
                                EnqueueStatusMessage(new(timestamp, (long)_errorsToReport[i], componentId, ComponentType.Switch, PropertyName.Error));
                        }
                    }

                    reader.Read(); // was at "}"
                }

                if (reader.ValueSpan.SequenceEqual("cloud"u8))
                {
                    reader.Read();
                    var conntexted = reader.TokenType == JsonTokenType.True ? 1 : 0;
                    EnqueueStatusMessage(new(timestamp, conntexted, 0, ComponentType.Switch, PropertyName.Output));
                }

            }
        }
        catch
        {
            return;
        }
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

    /// <summary> Reads active energy values and moves reader outside the object. </summary>
    private static void ReadActiveEnergyData(ref Utf8JsonReader reader, out double? total, out int? lastMinute_mWh, out long? minuteTs)
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
                    lastMinute_mWh = reader.GetInt32();
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

    /// <summary> Moves reader to the opening bracket of a params object</summary>
    private static bool OpenParamsObject(ref Utf8JsonReader reader)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                if (reader.ValueTextEquals("params"))
                {
                    if (!reader.Read()) return false; // current value "{"
                    if (reader.TokenType != JsonTokenType.StartObject) return false;
                    return true;
                }

                reader.Skip();
            }
        }

        return false;
    }

    private DateTimeOffset FindAndParseTimestamp(ref Utf8JsonReader reader)
    {
        Utf8JsonReader startOfParamsReader = reader;

        while (!startOfParamsReader.ValueSpan.SequenceEqual("ts"u8))
        {
            startOfParamsReader.Skip();
        }

        startOfParamsReader.Read(); // read unix epoch
        var tsUnix = ParseUnixTimestamp(startOfParamsReader.ValueSpan);
        return DateTimeOffset.FromUnixTimeMilliseconds(tsUnix);
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

    /// <summary>Writes a status change immediately when channel capacity is available and delegates only backpressure to the asynchronous path.</summary>
    private void EnqueueStatusMessage(ShellyStateChangeMsg stateChange)
    {
        if (_stateChangesChannel.Writer.TryWrite(stateChange)) ;

        if (_pendingMessagesCount == 128) throw new Exception("Message queue is full");
        //return SendShellyStateChangeMsgAsync(stateChange, CancellationToken.None);
        _pendingStateUpdates[_pendingMessagesCount] = stateChange;
        _pendingMessagesCount++;
    }
}
