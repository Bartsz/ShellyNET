using ShellyNET.Client.Model;
using ShellyNET.Client.Model.Enums;
using System.Buffers;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Net;
using System.Security.Cryptography;

namespace ShellyNET.Client.Parsers;

public readonly struct ShellyNotifyStatusParseResult : IDisposable
{
    private readonly ArrayPool<ShellyNotifyStatusMessage>? _messagePool;
    private readonly ArrayPool<ShellyNotifyStatusStringMessage>? _stringMessagePool;
    private readonly ShellyNotifyStatusMessage[] _messages { get; }
    private readonly ShellyNotifyStatusStringMessage[] _stringMessages { get; }

    public ShellyNotifyStatusParseResult()
    {
        _messages = [];
        _stringMessages = [];
    }

    public ShellyNotifyStatusParseResult(ShellyNotifyStatusMessage[] messages,
                                         ShellyNotifyStatusStringMessage[] stringMessages,
                                         ArrayPool<ShellyNotifyStatusMessage> messagesPool,
                                         ArrayPool<ShellyNotifyStatusStringMessage> stringMessagesPool,
                                         ushort messageCount,
                                         ushort stringMessageCount)
    {
        _messages = messages;
        _stringMessages = stringMessages;
        _messagePool = messagesPool;
        _stringMessagePool = stringMessagesPool;
        MessageCount = messageCount;
        StringMessageCount = stringMessageCount;
    }

    public ushort MessageCount { get; }

    public ushort StringMessageCount { get; }

    public readonly ReadOnlySpan<ShellyNotifyStatusMessage> MessageSpan => _messages.AsSpan(0, MessageCount);

    public readonly ReadOnlySpan<ShellyNotifyStatusStringMessage> StringMessageSpan => _stringMessages.AsSpan(0, StringMessageCount);


    public void Dispose()
    {
        _messagePool?.Return(_messages);
        _stringMessagePool?.Return(_stringMessages, clearArray: true);
    }
}

public enum StatesContainingObject
{
    Params,
    Results,
}

public sealed partial class ShellyNotifyStatusParser
{
    private readonly byte[] _errorsToReportBuffer = new byte[128];
    private readonly byte[] _flagsToReportBuffer = new byte[128];

    private readonly ArrayPool<ShellyNotifyStatusMessage> _messagesPool = ArrayPool<ShellyNotifyStatusMessage>.Shared;
    private readonly ArrayPool<ShellyNotifyStatusStringMessage> _stringMessagesPool = ArrayPool<ShellyNotifyStatusStringMessage>.Shared;
    private ShellyNotifyStatusMessage[] _messages = [];
    private ShellyNotifyStatusStringMessage[] _stringMessages = [];

    private ushort _messageCount = 0;
    private ushort _stringMessageCount = 0;

    private static readonly ErrorDefinition[] _switchErrors = [
                            new("overtemp"u8.ToArray(), (byte)SwitchError.Overtemp),
                            new("overpower"u8.ToArray(), (byte)SwitchError.Overpower),
                            new("overvoltage"u8.ToArray(), (byte)SwitchError.Overvoltage),
                            new("undervoltage"u8.ToArray(), (byte)SwitchError.Undervoltage),
                            ];
    private static readonly ErrorDefinition[] _cameraErrors = [
                        new("streamer_fs_bad"u8.ToArray(), (byte)CameraError.StreamerFileSystemBad),
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


    private static readonly ErrorDefinition[] _inputErrors = [
                            new("read"u8.ToArray(), (byte)InputError.Read),
                            new("out_of_range"u8.ToArray(), (byte)InputError.OutOfRange),
                            ];

    private static readonly ErrorDefinition[] _pm1Errors = [
                            new("out_of_range:voltage"u8.ToArray(), (byte)Pm1Error.OutOfRangeVoltage),
                            new("out_of_range:current"u8.ToArray(), (byte)Pm1Error.OutOfRangeCurrent),
                            new("power_meter_failure"u8.ToArray(), (byte)Pm1Error.PowerMeterFailure),
                            new("out_of_range:aprtpower"u8.ToArray(), (byte)Pm1Error.OutOfRangeApparentPower),
                            new("out_of_range:apower"u8.ToArray(), (byte)Pm1Error.OutOfRangeActivePower),
                            ];

    private static readonly ErrorDefinition[] _lightErrors = [
                            new("overcurrent"u8.ToArray(), (byte)LightError.Overcurrent),
                            new("overvoltage"u8.ToArray(), (byte)LightError.Overvoltage),
                            new("overpower"u8.ToArray(), (byte)LightError.Overpower),
                            new("undervoltage"u8.ToArray(), (byte)LightError.Undervoltage),
                            new("overtemp"u8.ToArray(), (byte)LightError.Overtemp),
                            new("unsupported_load"u8.ToArray(), (byte)LightError.UnsupportedLoad),
                            new("cal_abort:interrupted"u8.ToArray(), (byte)LightError.CalAbortInterrupted),
                            new("cal_abort:power_read"u8.ToArray(), (byte)LightError.CalAbortPowerRead),
                            new("cal_abort:no_load"u8.ToArray(), (byte)LightError.CalAbortNoLoad),
                            new("cal_abort:no_synchro"u8.ToArray(), (byte)LightError.CalAbortNoSynchro),
                            new("cal_abort:non_dimmable"u8.ToArray(), (byte)LightError.CalAbortNonDimmable),
                            new("cal_abort:overpower"u8.ToArray(), (byte)LightError.CalAbortOverpower),
                            new("cal_abort:unsupported_load"u8.ToArray(), (byte)LightError.CalAbortUnsupportedLoad),
                            ];

    private static readonly FlagDefinition[] _pm1Flags = [
                            new("overcurrent"u8.ToArray(), (byte)Pm1Flag.Overcurrent),
                            new("overvoltage"u8.ToArray(), (byte)Pm1Flag.Overvoltage),
                            new("overpower"u8.ToArray(), (byte)Pm1Flag.Overpower),
                            new("undervoltage"u8.ToArray(), (byte)Pm1Flag.Undervoltage),
                            new("undercurrent"u8.ToArray(), (byte)Pm1Flag.Undercurrent),
                            new("underpower"u8.ToArray(), (byte)Pm1Flag.Underpower),
                            ];

    private static readonly FlagDefinition[] _lightFlags = [
                            new("no_load"u8.ToArray(), (byte)LightFlag.NoLoad),
                            new("uncalibrated"u8.ToArray(), (byte)LightFlag.Uncalibrated),
                            ];


    private static readonly ErrorDefinition[] _rgbErrors = [
                            new("overtemp"u8.ToArray(), (byte)RgbError.Overtemp),
                        ];

    private static readonly ErrorDefinition[] _rgbwErrors = [
                                new("overtemp"u8.ToArray(), (byte)RgbwError.Overtemp),
                        ];


    private static readonly ErrorDefinition[] _storageErrors = [
                            new("needs_formatting"u8.ToArray(), (byte)StorageError.NeedsFormatting),
                        new("mounting"u8.ToArray(), (byte)StorageError.Mounting),
                        new("mount_failed"u8.ToArray(), (byte)StorageError.MountFailed),
                        new("mounting_nfs"u8.ToArray(), (byte)StorageError.MountingNfs),
                        new("unmounting_nfs"u8.ToArray(), (byte)StorageError.UnmountingNfs),
                        new("unmount_failed"u8.ToArray(), (byte)StorageError.UnmountFailed),
                        new("resolving_nfs_host"u8.ToArray(), (byte)StorageError.ResolvingNfsHost),
                        new("filesystem_error"u8.ToArray(), (byte)StorageError.FileSystemError),
                        new("formatting"u8.ToArray(), (byte)StorageError.Formatting),
                        new("format_failed"u8.ToArray(), (byte)StorageError.FormatFailed),
                        new("ejected"u8.ToArray(), (byte)StorageError.Ejected),
                        new("eject_failed"u8.ToArray(), (byte)StorageError.EjectFailed),
                    ];

    private static readonly FlagDefinition[] _bleFlags = [
                            new("scanning"u8.ToArray(), (byte)BleFlag.Scanning),
                        new("advertising"u8.ToArray(), (byte)BleFlag.Advertising),
                        new("connected"u8.ToArray(), (byte)BleFlag.Connected),
                    ];

    private static readonly ErrorDefinition[] _cctErrors = [
                            new("overtemp"u8.ToArray(), (byte)CctError.Overtemp),
                    ];

    private static readonly ErrorDefinition[] _devicePowerErrors = [
                            new("read"u8.ToArray(), (byte)DevicePowerError.Read),
                    ];

    private static readonly ErrorDefinition[] _emErrors = [
                            new("out_of_range:active_power"u8.ToArray(), (byte)EmError.OutOfRangeActivePower),
                        new("out_of_range:apparent_power"u8.ToArray(), (byte)EmError.OutOfRangeApparentPower),
                        new("out_of_range:voltage"u8.ToArray(), (byte)EmError.OutOfRangeVoltage),
                        new("out_of_range:current"u8.ToArray(), (byte)EmError.OutOfRangeCurrent),
                        new("power_meter_failure"u8.ToArray(), (byte)EmError.PowerMeterFailure),
                        new("phase_sequence"u8.ToArray(), (byte)EmError.PhaseSequence),
                        new("ct_type_not_set"u8.ToArray(), (byte)EmError.CtTypeNotSet),
                    ];

    private static readonly FlagDefinition[] _emFlags = [
                            new("overcurrent"u8.ToArray(), (byte)EmFlag.Overcurrent),
                        new("overvoltage"u8.ToArray(), (byte)EmFlag.Overvoltage),
                        new("overpower"u8.ToArray(), (byte)EmFlag.Overpower),
                        new("undervoltage"u8.ToArray(), (byte)EmFlag.Undervoltage),
                        new("undercurrent"u8.ToArray(), (byte)EmFlag.Undercurrent),
                        new("underpower"u8.ToArray(), (byte)EmFlag.Underpower),
                    ];

    private static readonly ErrorDefinition[] _em1Errors = [
                            new("out_of_range:voltage"u8.ToArray(), (byte)Em1Error.OutOfRangeVoltage),
                        new("out_of_range:current"u8.ToArray(), (byte)Em1Error.OutOfRangeCurrent),
                        new("power_meter_failure"u8.ToArray(), (byte)Em1Error.PowerMeterFailure),
                        new("ct_type_not_set"u8.ToArray(), (byte)Em1Error.CtTypeNotSet),
                        new("out_of_range:act_power"u8.ToArray(), (byte)Em1Error.OutOfRangeActivePower),
                        new("out_of_range:aprt_power"u8.ToArray(), (byte)Em1Error.OutOfRangeApparentPower),
                    ];

    private static readonly FlagDefinition[] _em1Flags = [
                            new("overcurrent"u8.ToArray(), (byte)Em1Flag.Overcurrent),
                        new("overvoltage"u8.ToArray(), (byte)Em1Flag.Overvoltage),
                        new("overpower"u8.ToArray(), (byte)Em1Flag.Overpower),
                        new("undervoltage"u8.ToArray(), (byte)Em1Flag.Undervoltage),
                        new("undercurrent"u8.ToArray(), (byte)Em1Flag.Undercurrent),
                        new("underpower"u8.ToArray(), (byte)Em1Flag.Underpower),
                        new("count_disabled"u8.ToArray(), (byte)Em1Flag.CountDisabled),
                    ];

    private static readonly FlagDefinition[] _sysBootTypes = [
                        new("poweron"u8.ToArray(), (byte)SysBootType.PowerOn),
                        new("software_restart"u8.ToArray(), (byte)SysBootType.SoftwareRestart),
                        new("deepsleep_wake"u8.ToArray(), (byte)SysBootType.DeepSleepWake),
                        new("internal"u8.ToArray(), (byte)SysBootType.Internal),
                        new("unknown"u8.ToArray(), (byte)SysBootType.Unknown),
                    ];

    private static readonly FlagDefinition[] _sysWakeupCauses = [
                        new("button"u8.ToArray(), (byte)SysWakeupCause.Button),
                        new("usb"u8.ToArray(), (byte)SysWakeupCause.Usb),
                        new("periodic"u8.ToArray(), (byte)SysWakeupCause.Periodic),
                        new("status_update"u8.ToArray(), (byte)SysWakeupCause.StatusUpdate),
                        new("alarm"u8.ToArray(), (byte)SysWakeupCause.Alarm),
                        new("alarm_test"u8.ToArray(), (byte)SysWakeupCause.AlarmTest),
                        new("undefined"u8.ToArray(), (byte)SysWakeupCause.Undefined),
                    ];

    private static readonly FlagDefinition[] _sysProvisioningStages = [
                        new("pending"u8.ToArray(), (byte)SysProvisioningStage.Pending),
                        new("confirmed"u8.ToArray(), (byte)SysProvisioningStage.Confirmed),
                    ];

    private static readonly FlagDefinition[] _wifiConnectionStatuses = [
                        new("disconnected"u8.ToArray(), (byte)WifiConnectionStatus.Disconnected),
                        new("connecting"u8.ToArray(), (byte)WifiConnectionStatus.Connecting),
                        new("connected"u8.ToArray(), (byte)WifiConnectionStatus.Connected),
                        new("got ip"u8.ToArray(), (byte)WifiConnectionStatus.GotIp),
                    ];

    private static readonly ErrorDefinition[] _em1DataErrors = [
                            new("ct_type_not_set"u8.ToArray(), (byte)Em1DataError.CtTypeNotSet),
                        new("database_error"u8.ToArray(), (byte)Em1DataError.DatabaseError),
                    ];

    private static readonly ErrorDefinition[] _emDataErrors = [
                            new("ct_type_not_set"u8.ToArray(), (byte)EmDataError.CtTypeNotSet),
                        new("database_error"u8.ToArray(), (byte)EmDataError.DatabaseError),
                    ];

    private static readonly ErrorDefinition[] _floodErrors = [
                            new("cable_unplugged"u8.ToArray(), (byte)FloodError.CableUnplugged),
                    ];

    private static readonly ErrorDefinition[] _humidityErrors = [
                            new("read"u8.ToArray(), (byte)HumidityError.Read),
                        new("out_of_range"u8.ToArray(), (byte)HumidityError.OutOfRange),
                    ];

    private static readonly ErrorDefinition[] _illuminanceErrors = [
                            new("read"u8.ToArray(), (byte)IlluminanceError.Read),
                        new("out_of_range"u8.ToArray(), (byte)IlluminanceError.OutOfRange),
                    ];

    private static readonly ErrorDefinition[] _temperatureErrors = [
                            new("read"u8.ToArray(), (byte)TemperatureError.Read),
                        new("out_of_range"u8.ToArray(), (byte)TemperatureError.OutOfRange),
                    ];

    private static readonly ErrorDefinition[] _voltmeterErrors = [
                        new("read"u8.ToArray(), (byte)VoltmeterError.Read),
                        new("out_of_range"u8.ToArray(), (byte)VoltmeterError.OutOfRange),
                    ];

    private static readonly FlagDefinition[] _zigbeeNetworkStates = [
                        new("disabled"u8.ToArray(), (byte)ZigbeeNetworkState.Disabled),
                        new("initializing"u8.ToArray(), (byte)ZigbeeNetworkState.Initializing),
                        new("steering"u8.ToArray(), (byte)ZigbeeNetworkState.Steering),
                        new("joined"u8.ToArray(), (byte)ZigbeeNetworkState.Joined),
                        new("failed"u8.ToArray(), (byte)ZigbeeNetworkState.Failed),
                    ];

    /// <summary>Processes a Shelly NotifyStatus frame using the expected property order as the fast path.</summary>
    public ShellyNotifyStatusParseResult ProcessNotifyStatusMessage(Memory<byte> message, StatesContainingObject container, CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return new();

        var reader = new Utf8JsonReader(message.Span);
        DateTimeOffset timestamp = default;

        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return new();
        if (!reader.Read()) return new();

        _messageCount = 0;
        _stringMessageCount = 0;
        _messages = _messagesPool.Rent(8);
        _stringMessages = _stringMessagesPool.Rent(8);

        // when DeviceStatus responds, there is no time stamp so we assume current timestamp
        if (container == StatesContainingObject.Results) timestamp = DateTimeOffset.UtcNow;

        var containingObjectKey = container == StatesContainingObject.Results ? "result"u8 : "params"u8;
        OpenContainingObject(ref reader, containingObjectKey); // after function reader is at "{" of the params which works with while loop
        ParseMessages(ref reader, timestamp);

        return new ShellyNotifyStatusParseResult(_messages, _stringMessages, _messagesPool, _stringMessagesPool, _messageCount, _stringMessageCount);
    }

    private void ParseMessages(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        try
        {
            while (true)
            {
                bool readProperty = false;

                if (reader.TokenType == JsonTokenType.EndObject) break;
                if (reader.TokenType != JsonTokenType.PropertyName) { if (!reader.Read()) return; continue; }

                if (timestamp == default)
                {
                    if (reader.ValueSpan.SequenceEqual("ts"u8))
                    {
                        reader.Read();
                        var unixEpochMs = ParseUnixTimestamp(reader.ValueSpan);
                        timestamp = DateTimeOffset.FromUnixTimeMilliseconds(unixEpochMs);
                        reader.Read();
                        readProperty = true;
                    }
                    else
                    {
                        // it will internally make a copy and leave original reader intact.
                        timestamp = FindAndParseTimestamp(ref reader);
                    }
                }

                // ordered in expected frequency of occurence - we don't care about rare case of DeviceInfo with ordered inputs
                // this needs investigation and monitoring on a complex system

                while (reader.ValueSpan.StartsWith("input:"u8))
                {
                    var flowControl = ReadInputChanges(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }

                while (reader.ValueSpan.StartsWith("switch:"u8))
                {
                    var flowControl = ReadSwitchChanges(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }

                while (reader.ValueSpan.StartsWith("pm1:"u8))
                {
                    var flowControl = ReadPm1Changes(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }

                while (reader.ValueSpan.StartsWith("cover:"u8))
                {
                    var flowControl = ReadCoverMessages(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }

                while (reader.ValueSpan.StartsWith("light:"u8))
                {
                    var flowControl = ReadLightChanges(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }

                while (reader.ValueSpan.StartsWith("rgb:"u8))
                {
                    var flowControl = ReadRgbChanges(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }

                while (reader.ValueSpan.StartsWith("rgbcct:"u8))
                {
                    var flowControl = ReadRgbcctChanges(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }

                while (reader.ValueSpan.StartsWith("rgbw:"u8))
                {
                    var flowControl = ReadRgbwChanges(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }

                while (reader.ValueSpan.StartsWith("humidity:"u8))
                {
                    var flowControl = ReadHumidityChanges(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }

                while (reader.ValueSpan.StartsWith("illuminance:"u8))
                {
                    var flowControl = ReadIlluminanceChanges(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }

                while (reader.ValueSpan.StartsWith("cct:"u8))
                {
                    var flowControl = ReadCctChanges(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }

                while (reader.ValueSpan.StartsWith("devicepower:"u8))
                {
                    var flowControl = ReadDevicePowerChanges(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }

                while (reader.ValueSpan.StartsWith("em:"u8))
                {
                    var flowControl = ReadEmChanges(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }

                while (reader.ValueSpan.StartsWith("em1:"u8))
                {
                    var flowControl = ReadEm1Changes(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }

                while (reader.ValueSpan.StartsWith("em1data:"u8))
                {
                    var flowControl = ReadEm1DataChanges(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }

                while (reader.ValueSpan.StartsWith("emdata:"u8))
                {
                    var flowControl = ReadEmDataChanges(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }

                if (reader.ValueSpan.StartsWith("eth"u8))
                {
                    var flowControl = ReadEthernetChanges(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }

                while (reader.ValueSpan.StartsWith("flood:"u8))
                {
                    var flowControl = ReadFloodChanges(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }

                if (reader.ValueSpan.StartsWith("matter"u8))
                {
                    var flowControl = ReadMatterChanges(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }

                if (reader.ValueSpan.StartsWith("modbus"u8))
                {
                    var flowControl = ReadModbusChanges(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }

                if (reader.ValueSpan.StartsWith("mqtt"u8))
                {
                    var flowControl = ReadMqttChanges(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }

                if (reader.ValueSpan.StartsWith("presence"u8) && !reader.ValueSpan.StartsWith("presencezone:"u8))
                {
                    var flowControl = ReadPresenceChanges(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }

                while (reader.ValueSpan.StartsWith("presencezone:"u8))
                {
                    var flowControl = ReadPresenceZoneChanges(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }

                while (reader.ValueSpan.StartsWith("smoke:"u8))
                {
                    var flowControl = ReadSmokeChanges(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }

                while (reader.ValueSpan.StartsWith("temperature:"u8))
                {
                    var flowControl = ReadTemperatureChanges(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }

                while (reader.ValueSpan.StartsWith("voltmeter:"u8))
                {
                    var flowControl = ReadVoltmeterChanges(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }


                while (reader.ValueSpan.StartsWith("camera:"u8))
                {
                    var flowControl = ReadCameraChanges(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }

                while (reader.ValueSpan.StartsWith("camerazone:"u8))
                {
                    var flowControl = ReadCameraZoneChanges(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }

                if (reader.ValueSpan.StartsWith("cloud"u8))
                {
                    var flowControl = ReadCloudMessages(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }

                while (reader.ValueSpan.StartsWith("storage:"u8))
                {
                    var flowControl = ReadStorageChanges(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }

                if (reader.ValueSpan.StartsWith("ble"u8))
                {
                    var flowControl = ReadBleChanges(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }
                while (reader.ValueSpan.StartsWith("cb:"u8))
                {
                    var flowControl = ReadCbChanges(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }

                if (reader.ValueSpan.StartsWith("sys"u8))
                {
                    var flowControl = ReadSysChanges(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }

                if (reader.ValueSpan.StartsWith("zigbee"u8))
                {
                    var flowControl = ReadZigbeeChanges(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }

                if (reader.ValueSpan.StartsWith("wifi"u8))
                {
                    var flowControl = ReadWifiChanges(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }

                if (reader.ValueSpan.StartsWith("ws"u8))
                {
                    var flowControl = ReadWebSocketChanges(ref reader, timestamp);
                    if (flowControl == true) return;
                    if (flowControl == false) continue;
                    readProperty = true;
                }


                if (!readProperty) { if (!reader.Read()) return; }
            }
        }
        catch
        {
            return;
        }
    }

    private bool? ReadCameraChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        var idSpan = reader.ValueSpan[7..];
        var componentId = byte.Parse(idSpan);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            if (reader.ValueSpan.SequenceEqual("id"u8)) { reader.Read(); reader.Read(); readProperty = true; }

            readProperty |= ReadBoolProperty(ref reader, "arm"u8, timestamp, componentId, ComponentType.Camera, PropertyName.CameraArmed);
            readProperty |= ReadBoolProperty(ref reader, "privacy"u8, timestamp, componentId, ComponentType.Camera, PropertyName.CameraPrivacy);

            if (reader.ValueSpan.SequenceEqual("streamer"u8))
            {
                reader.Read();

                var streamerState = reader.ValueSpan.SequenceEqual("stopped"u8) ? CameraStreamerState.Stopped :
                                    reader.ValueSpan.SequenceEqual("starting"u8) ? CameraStreamerState.Starting :
                                    reader.ValueSpan.SequenceEqual("running"u8) ? CameraStreamerState.Running :
                                    reader.ValueSpan.SequenceEqual("stopping"u8) ? CameraStreamerState.Stopping : CameraStreamerState.Unknown;

                AddMessage(new(timestamp, (long)streamerState, componentId, ComponentType.Camera, PropertyName.CameraStreamerState));
                reader.Read();
                readProperty = true;
            }

            readProperty |= ReadStringProperty(ref reader, "streamer_version"u8, timestamp, componentId, ComponentType.Camera, PropertyName.CameraStreamerVersion);
            readProperty |= ReadBoolProperty(ref reader, "motion"u8, timestamp, componentId, ComponentType.Camera, PropertyName.CameraMotion);
            readProperty |= ReadInt64Property(ref reader, "streams"u8, timestamp, componentId, ComponentType.Camera, PropertyName.CameraActiveStreams);

            if (reader.ValueSpan.SequenceEqual("recording_encryption"u8))
            {
                if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return true;

                while (true)
                {
                    bool readEncryptionProperty = false;

                    if (reader.TokenType == JsonTokenType.EndObject) break;
                    if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

                    readEncryptionProperty |= ReadBoolProperty(ref reader, "configured"u8, timestamp, componentId, ComponentType.Camera, PropertyName.CameraRecordingEncryptionConfigured);

                    if (!readEncryptionProperty) { if (!reader.Read()) return true; }
                }

                reader.Read();
                readProperty = true;
            }

            readProperty |= ReadJsonStringProperty(ref reader, "recordings"u8, timestamp, componentId, ComponentType.Camera, PropertyName.CameraRecordings);
            readProperty |= ReadErrors(ref reader, _cameraErrors, timestamp, componentId, ComponentType.Camera);

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read();
        return null;
    }

    private bool? ReadCloudMessages(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }


            readProperty |= ReadBoolProperty(ref reader, "connected"u8, timestamp, 0, ComponentType.Cloud, PropertyName.Connected);

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read(); // was at "}"
        return null;
    }

    private bool? ReadCoverMessages(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        var idSpan = reader.ValueSpan[7..];
        var componentId = byte.Parse(idSpan);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            if (reader.ValueSpan.SequenceEqual("id"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            if (reader.ValueSpan.SequenceEqual("source"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            if (reader.ValueSpan.SequenceEqual("tag"u8)) { reader.Read(); reader.Read(); readProperty = true; }

            readProperty |= ReadBoolProperty(ref reader, "output"u8, timestamp, componentId, ComponentType.Cover, PropertyName.Output);
            readProperty |= ReadDoubleProperty(ref reader, "apower"u8, timestamp, componentId, ComponentType.Cover, PropertyName.ActivePower);
            readProperty |= ReadDoubleProperty(ref reader, "voltage"u8, timestamp, componentId, ComponentType.Cover, PropertyName.Voltage);
            readProperty |= ReadDoubleProperty(ref reader, "current"u8, timestamp, componentId, ComponentType.Cover, PropertyName.Current);
            readProperty |= ReadDoubleProperty(ref reader, "pf"u8, timestamp, componentId, ComponentType.Cover, PropertyName.PowerFactor);
            readProperty |= ReadDoubleProperty(ref reader, "freq"u8, timestamp, componentId, ComponentType.Cover, PropertyName.Frequency);
            readProperty |= ReadActiveEnergyDataObject(ref reader, "aenergy"u8, timestamp, componentId, ComponentType.Cover, true);
            readProperty |= ReadNullableDoubleProperty(ref reader, "current_pos"u8, timestamp, componentId, ComponentType.Cover, PropertyName.CoverCurrentPosition);
            readProperty |= ReadNullableDoubleProperty(ref reader, "target_pos"u8, timestamp, componentId, ComponentType.Cover, PropertyName.CoverTargetPosition);
            readProperty |= ReadDoubleProperty(ref reader, "move_timeout"u8, timestamp, componentId, ComponentType.Cover, PropertyName.CoverMoveTimeoutSeconds);
            readProperty |= ReadTimeStampProperty(ref reader, "move_started_at"u8, timestamp, componentId, ComponentType.Cover, PropertyName.CoverMoveStartedAUnixMs);
            readProperty |= ReadBoolProperty(ref reader, "pos_control"u8, timestamp, componentId, ComponentType.Cover, PropertyName.CoverHasPositionControl);

            if (reader.ValueSpan.SequenceEqual("last_direction"u8))
            {
                reader.Read();
                if (reader.TokenType == JsonTokenType.Null)
                    AddMessage(new(timestamp, (long)CoverLastDirection.Unkown, componentId, ComponentType.Cover, PropertyName.CoverLastDirection));
                else
                {
                    var lastDirection = reader.ValueSpan.SequenceEqual("open"u8) ? CoverLastDirection.Open : CoverLastDirection.Close;
                    AddMessage(new(timestamp, (long)lastDirection, componentId, ComponentType.Cover, PropertyName.CoverLastDirection));
                }
                reader.Read();
                readProperty = true;
            }
            readProperty |= TryParseTemperatureObject(ref reader, "temperature"u8, timestamp, componentId, ComponentType.Cover);

            if (reader.ValueSpan.SequenceEqual("slat_pos"u8))
            {
                reader.Read();
                if (reader.TokenType == JsonTokenType.Null)
                    AddMessage(new(timestamp, (double?)null, componentId, ComponentType.Cover, PropertyName.CoverSlatPosition));
                else
                    AddMessage(new(timestamp, reader.GetDouble(), componentId, ComponentType.Cover, PropertyName.CoverSlatPosition));
                reader.Read();
                readProperty = true;
            }

            readProperty |= ReadErrors(ref reader, _coverErrors, timestamp, componentId, ComponentType.Cover);

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read(); // was at "}"
        return null;
    }

    private bool? ReadSwitchChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        var idSpan = reader.ValueSpan[7..];
        var componentId = byte.Parse(idSpan);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            if (reader.ValueSpan.SequenceEqual("id"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            if (reader.ValueSpan.SequenceEqual("source"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            if (reader.ValueSpan.SequenceEqual("tag"u8)) { reader.Read(); reader.Read(); readProperty = true; }

            readProperty |= ReadBoolProperty(ref reader, "output"u8, timestamp, componentId, ComponentType.Switch, PropertyName.Output);
            readProperty |= ReadTimeStampProperty(ref reader, "timer_started_at"u8, timestamp, componentId, ComponentType.Switch, PropertyName.TimerStatedAt);
            readProperty |= ReadInt64Property(ref reader, "timer_duration"u8, timestamp, componentId, ComponentType.Switch, PropertyName.TimerDuration);
            readProperty |= ReadDoubleProperty(ref reader, "apower"u8, timestamp, componentId, ComponentType.Switch, PropertyName.ActivePower);
            readProperty |= ReadDoubleProperty(ref reader, "voltage"u8, timestamp, componentId, ComponentType.Switch, PropertyName.Voltage);
            readProperty |= ReadDoubleProperty(ref reader, "current"u8, timestamp, componentId, ComponentType.Switch, PropertyName.Current);
            readProperty |= ReadDoubleProperty(ref reader, "pf"u8, timestamp, componentId, ComponentType.Switch, PropertyName.PowerFactor);
            readProperty |= ReadDoubleProperty(ref reader, "freq"u8, timestamp, componentId, ComponentType.Switch, PropertyName.Frequency);

            readProperty |= ReadActiveEnergyDataObject(ref reader, "aenergy"u8, timestamp, componentId, ComponentType.Switch, true);
            readProperty |= ReadActiveEnergyDataObject(ref reader, "ret_aenergy"u8, timestamp, componentId, ComponentType.Switch, false);


            if (reader.ValueSpan.SequenceEqual("counts"u8))
            {
                reader.Read(); // open object "{";
                while (true)
                {
                    bool readCountProperty = false;
                    if (reader.TokenType == JsonTokenType.EndObject) break;
                    if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

                    readCountProperty |= ReadDoubleProperty(ref reader, "on_time"u8, timestamp, componentId, ComponentType.Switch, PropertyName.TotalOnTimeMinutes);
                    readCountProperty |= ReadTimeStampProperty(ref reader, "on_time_rst_ts"u8, timestamp, componentId, ComponentType.Switch, PropertyName.TotalOnTimeCounterLastResetTime);
                    readCountProperty |= ReadInt64Property(ref reader, "switch_on"u8, timestamp, componentId, ComponentType.Switch, PropertyName.OffToOnCylesCount);
                    readCountProperty |= ReadTimeStampProperty(ref reader, "switch_on_rst_ts"u8, timestamp, componentId, ComponentType.Switch, PropertyName.OffToOnCyckesCountLastResetTime);
                    readCountProperty |= ReadDoubleProperty(ref reader, "on_above_thr"u8, timestamp, componentId, ComponentType.Switch, PropertyName.TotalMinutesAbovePowerThreshold);
                    readCountProperty |= ReadTimeStampProperty(ref reader, "on_above_thr_rst_ts"u8, timestamp, componentId, ComponentType.Switch, PropertyName.TotalMinutesAbovePowerThrLastResetTime);

                    if (!readCountProperty) { if (!reader.Read()) return true; }
                }
                reader.Read();
                readProperty = true;
            }
            readProperty |= TryParseTemperatureObject(ref reader, "temperature"u8, timestamp, componentId, ComponentType.Switch);

            readProperty |= ReadErrors(ref reader, _switchErrors, timestamp, componentId, ComponentType.Switch);

            if (!readProperty) { if (!reader.Read()) return true; }

        }

        reader.Read(); // was at "}"
        return null;
    }

    private bool? ReadInputChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        var idSpan = reader.ValueSpan[6..];
        var componentId = byte.Parse(idSpan);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            if (reader.ValueSpan.SequenceEqual("id"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            if (reader.ValueSpan.SequenceEqual("source"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            if (reader.ValueSpan.SequenceEqual("tag"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            readProperty |= ReadNullableBoolProperty(ref reader, "state"u8, timestamp, componentId, ComponentType.Input, PropertyName.State);
            readProperty |= ReadNullableDoubleProperty(ref reader, "percent"u8, timestamp, componentId, ComponentType.Input, PropertyName.Percent);
            readProperty |= ReadNullableDoubleProperty(ref reader, "xpercent"u8, timestamp, componentId, ComponentType.Input, PropertyName.PercentTransformed);

            if (reader.ValueSpan.SequenceEqual("counts"u8))
            {
                ReadInputCountData(ref reader, timestamp, componentId);
                readProperty = true;
            }
            readProperty |= ReadDoubleProperty(ref reader, "freq"u8, timestamp, componentId, ComponentType.Input, PropertyName.Frequency);
            readProperty |= ReadDoubleProperty(ref reader, "xfreq"u8, timestamp, componentId, ComponentType.Input, PropertyName.FrequencyTransformed);

            readProperty |= ReadErrors(ref reader, _inputErrors, timestamp, componentId, ComponentType.Input);

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read(); // was at "}"
        return null;
    }

    private bool? ReadPm1Changes(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        var idSpan = reader.ValueSpan[4..];
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
            readProperty |= ReadDoubleProperty(ref reader, "voltage"u8, timestamp, componentId, ComponentType.PM1, PropertyName.Voltage);
            readProperty |= ReadDoubleProperty(ref reader, "current"u8, timestamp, componentId, ComponentType.PM1, PropertyName.Current);
            readProperty |= ReadDoubleProperty(ref reader, "apower"u8, timestamp, componentId, ComponentType.PM1, PropertyName.ActivePower);
            readProperty |= ReadDoubleProperty(ref reader, "aprtpower"u8, timestamp, componentId, ComponentType.PM1, PropertyName.ApperentPower);
            readProperty |= ReadDoubleProperty(ref reader, "pf"u8, timestamp, componentId, ComponentType.PM1, PropertyName.PowerFactor);
            readProperty |= ReadDoubleProperty(ref reader, "freq"u8, timestamp, componentId, ComponentType.PM1, PropertyName.Frequency);
            readProperty |= ReadActiveEnergyDataObject(ref reader, "aenergy"u8, timestamp, componentId, ComponentType.PM1, true);
            readProperty |= ReadActiveEnergyDataObject(ref reader, "ret_aenergy"u8, timestamp, componentId, ComponentType.PM1, activeEnergy: false);

            readProperty |= ReadErrors(ref reader, _pm1Errors, timestamp, componentId, ComponentType.PM1);

            if (reader.ValueSpan.SequenceEqual("flags"u8))
            {
                var count = ReadFlags(ref reader, _pm1Flags, _flagsToReportBuffer);

                for (int i = 0; i < count; i++)
                    AddMessage(new(timestamp, (long)_flagsToReportBuffer[i], componentId, ComponentType.PM1, PropertyName.Flag));
                readProperty = true;
            }

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read(); // was at "}"
        return null;
    }

    private bool? ReadLightChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        var idSpan = reader.ValueSpan[6..];
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
            readProperty |= ReadBoolProperty(ref reader, "output"u8, timestamp, componentId, ComponentType.Light, PropertyName.Output);
            readProperty |= ReadDoubleProperty(ref reader, "brightness"u8, timestamp, componentId, ComponentType.Light, PropertyName.Brightness);
            readProperty |= ReadTimeStampProperty(ref reader, "timer_started_at"u8, timestamp, componentId, ComponentType.Light, PropertyName.TimerStatedAt);
            readProperty |= ReadDoubleProperty(ref reader, "timer_duration"u8, timestamp, componentId, ComponentType.Light, PropertyName.TimerDuration);
            readProperty |= ReadLightSourceTransitionObject(ref reader, "transition"u8, timestamp, componentId, ComponentType.Light);
            readProperty |= TryParseTemperatureObject(ref reader, "temperature"u8, timestamp, componentId, ComponentType.Light);
            readProperty |= ReadActiveEnergyDataObject(ref reader, "aenergy"u8, timestamp, componentId, ComponentType.Light, true);
            readProperty |= ReadDoubleProperty(ref reader, "apower"u8, timestamp, componentId, ComponentType.Light, PropertyName.ActivePower);
            readProperty |= ReadDoubleProperty(ref reader, "voltage"u8, timestamp, componentId, ComponentType.Light, PropertyName.Voltage);
            readProperty |= ReadDoubleProperty(ref reader, "current"u8, timestamp, componentId, ComponentType.Light, PropertyName.Current);

            if (reader.ValueSpan.SequenceEqual("calibration"u8))
            {
                reader.Read(); // open object "{";
                while (true)
                {
                    bool readCalibrationProperty = false;
                    if (reader.TokenType == JsonTokenType.EndObject) break;
                    if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

                    readCalibrationProperty |= ReadDoubleProperty(ref reader, "progress"u8, timestamp, componentId, ComponentType.Light, PropertyName.CalibrationProgress);

                    if (!readCalibrationProperty) { if (!reader.Read()) return true; }
                }
                reader.Read();
                readProperty = true;
            }


            readProperty |= ReadErrors(ref reader, _lightErrors, timestamp, componentId, ComponentType.Light);

            if (reader.ValueSpan.SequenceEqual("flags"u8))
            {
                var count = ReadFlags(ref reader, _lightFlags, _flagsToReportBuffer);

                for (int i = 0; i < count; i++)
                    AddMessage(new(timestamp, (long)_flagsToReportBuffer[i], componentId, ComponentType.Light, PropertyName.Flag));
                readProperty = true;
            }
            if (!readProperty) { if (!reader.Read()) return true; }

        }

        reader.Read(); // was at "}"
        return null;
    }


    private bool? ReadRgbChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        var idSpan = reader.ValueSpan[4..];
        var componentId = byte.Parse(idSpan);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            if (reader.ValueSpan.SequenceEqual("id"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            if (reader.ValueSpan.SequenceEqual("source"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            if (reader.ValueSpan.SequenceEqual("tag"u8)) { reader.Read(); reader.Read(); readProperty = true; }

            readProperty |= ReadBoolProperty(ref reader, "output"u8, timestamp, componentId, ComponentType.RGB, PropertyName.Output);
            readProperty |= ReadRGBProperty(ref reader, "rgb"u8, timestamp, componentId, ComponentType.RGB, PropertyName.CurrentRgb);
            readProperty |= ReadDoubleProperty(ref reader, "brightness"u8, timestamp, componentId, ComponentType.RGB, PropertyName.Brightness);
            readProperty |= ReadTimeStampProperty(ref reader, "timer_started_at"u8, timestamp, componentId, ComponentType.RGB, PropertyName.TimerStatedAt);
            readProperty |= ReadDoubleProperty(ref reader, "timer_duration"u8, timestamp, componentId, ComponentType.RGB, PropertyName.TimerDuration);
            readProperty |= ReadLightSourceTransitionObject(ref reader, "transition"u8, timestamp, componentId, ComponentType.RGB);
            readProperty |= TryParseTemperatureObject(ref reader, "temperature"u8, timestamp, componentId, ComponentType.RGB);
            readProperty |= ReadActiveEnergyDataObject(ref reader, "aenergy"u8, timestamp, componentId, ComponentType.RGB, true);
            readProperty |= ReadDoubleProperty(ref reader, "apower"u8, timestamp, componentId, ComponentType.RGB, PropertyName.ActivePower);
            readProperty |= ReadDoubleProperty(ref reader, "voltage"u8, timestamp, componentId, ComponentType.RGB, PropertyName.Voltage);
            readProperty |= ReadDoubleProperty(ref reader, "current"u8, timestamp, componentId, ComponentType.RGB, PropertyName.Current);

            readProperty |= ReadErrors(ref reader, _rgbErrors, timestamp, componentId, ComponentType.RGB);

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read();
        return null;
    }

    private bool? ReadRgbcctChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        var idSpan = reader.ValueSpan[7..];
        var componentId = byte.Parse(idSpan);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            if (reader.ValueSpan.SequenceEqual("id"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            if (reader.ValueSpan.SequenceEqual("source"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            if (reader.ValueSpan.SequenceEqual("tag"u8)) { reader.Read(); reader.Read(); readProperty = true; }

            if (reader.ValueSpan.SequenceEqual("mode"u8))
            {
                reader.Read();
                var mode = reader.ValueSpan.SequenceEqual("rgb"u8) ? RgbCctMode.Rgb : reader.ValueSpan.SequenceEqual("cct"u8) ? RgbCctMode.Cct : RgbCctMode.Unknown;
                AddMessage(new(timestamp, (long)mode, componentId, ComponentType.RGBCCT, PropertyName.RGBCCTMode));
                reader.Read();
                readProperty = true;
            }
            readProperty |= ReadBoolProperty(ref reader, "output"u8, timestamp, componentId, ComponentType.RGBCCT, PropertyName.Output);
            readProperty |= ReadRGBProperty(ref reader, "rgb"u8, timestamp, componentId, ComponentType.RGBCCT, PropertyName.CurrentRgb);
            readProperty |= ReadDoubleProperty(ref reader, "ct"u8, timestamp, componentId, ComponentType.RGBCCT, PropertyName.ColorTemperatureK);
            readProperty |= ReadDoubleProperty(ref reader, "brightness"u8, timestamp, componentId, ComponentType.RGBCCT, PropertyName.Brightness);
            readProperty |= ReadTimeStampProperty(ref reader, "timer_started_at"u8, timestamp, componentId, ComponentType.RGBCCT, PropertyName.TimerStatedAt);
            readProperty |= ReadDoubleProperty(ref reader, "timer_duration"u8, timestamp, componentId, ComponentType.RGBCCT, PropertyName.TimerDuration);
            readProperty |= ReadLightSourceTransitionObject(ref reader, "transition"u8, timestamp, componentId, ComponentType.RGBCCT);
            readProperty |= ReadActiveEnergyDataObject(ref reader, "aenergy"u8, timestamp, componentId, ComponentType.RGBCCT, true);
            readProperty |= ReadDoubleProperty(ref reader, "apower"u8, timestamp, componentId, ComponentType.RGBCCT, PropertyName.ActivePower);

            //readProperty |= ReadErrors(ref reader, _rgbwErrors, _errorsToReport, timestamp, componentId, ComponentType.RGBCCT);

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read();
        return null;
    }


    private bool? ReadRgbwChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        var idSpan = reader.ValueSpan[5..];
        var componentId = byte.Parse(idSpan);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            if (reader.ValueSpan.SequenceEqual("id"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            if (reader.ValueSpan.SequenceEqual("source"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            if (reader.ValueSpan.SequenceEqual("tag"u8)) { reader.Read(); reader.Read(); readProperty = true; }

            readProperty |= ReadBoolProperty(ref reader, "output"u8, timestamp, componentId, ComponentType.RGBW, PropertyName.Output);
            readProperty |= ReadRGBProperty(ref reader, "rgb"u8, timestamp, componentId, ComponentType.RGBW, PropertyName.CurrentRgb);
            readProperty |= ReadDoubleProperty(ref reader, "brightness"u8, timestamp, componentId, ComponentType.RGBW, PropertyName.Brightness);
            readProperty |= ReadDoubleProperty(ref reader, "white"u8, timestamp, componentId, ComponentType.RGBW, PropertyName.White);
            readProperty |= ReadTimeStampProperty(ref reader, "timer_started_at"u8, timestamp, componentId, ComponentType.RGBW, PropertyName.TimerStatedAt);
            readProperty |= ReadDoubleProperty(ref reader, "timer_duration"u8, timestamp, componentId, ComponentType.RGBW, PropertyName.TimerDuration);
            readProperty |= ReadLightSourceTransitionObject(ref reader, "transition"u8, timestamp, componentId, ComponentType.RGBW);
            readProperty |= TryParseTemperatureObject(ref reader, "temperature"u8, timestamp, componentId, ComponentType.RGBW);
            readProperty |= ReadActiveEnergyDataObject(ref reader, "aenergy"u8, timestamp, componentId, ComponentType.RGBW, true);
            readProperty |= ReadDoubleProperty(ref reader, "apower"u8, timestamp, componentId, ComponentType.RGBW, PropertyName.ActivePower);
            readProperty |= ReadDoubleProperty(ref reader, "voltage"u8, timestamp, componentId, ComponentType.RGBW, PropertyName.Voltage);
            readProperty |= ReadDoubleProperty(ref reader, "current"u8, timestamp, componentId, ComponentType.RGBW, PropertyName.Current);

            readProperty |= ReadErrors(ref reader, _rgbwErrors, timestamp, componentId, ComponentType.RGBW);

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read();
        return null;
    }

    private bool? ReadCameraZoneChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        var idSpan = reader.ValueSpan[11..];
        var componentId = byte.Parse(idSpan);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            if (reader.ValueSpan.SequenceEqual("id"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            readProperty |= ReadBoolProperty(ref reader, "motion"u8, timestamp, componentId, ComponentType.CameraZone, PropertyName.CameraZoneMotion);

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read();
        return null;
    }

    private bool? ReadStorageChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        var idSpan = reader.ValueSpan[8..];
        var componentId = byte.Parse(idSpan);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            if (reader.ValueSpan.SequenceEqual("id"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            readProperty |= ReadInt64Property(ref reader, "rev"u8, timestamp, componentId, ComponentType.Storage, PropertyName.StorageRevision);
            readProperty |= ReadBoolProperty(ref reader, "present"u8, timestamp, componentId, ComponentType.Storage, PropertyName.StoragePresent);
            readProperty |= ReadBoolProperty(ref reader, "active"u8, timestamp, componentId, ComponentType.Storage, PropertyName.StorageActive);
            readProperty |= ReadInt64Property(ref reader, "fs_free"u8, timestamp, componentId, ComponentType.Storage, PropertyName.StorageFileSystemFreeBytes);
            readProperty |= ReadInt64Property(ref reader, "fs_size"u8, timestamp, componentId, ComponentType.Storage, PropertyName.StorageFileSystemSizeBytes);
            readProperty |= ReadErrors(ref reader, _storageErrors, timestamp, componentId, ComponentType.Storage);

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read();
        return null;
    }

    private bool? ReadBleChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        const byte componentId = 0;
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            readProperty |= ReadBluetoothAddressProperty(ref reader, "addr"u8, timestamp, componentId, ComponentType.BluetoothLowEnergy);

            readProperty |= ReadFlagsWithProperty(ref reader, "flags"u8, _bleFlags, timestamp, componentId, ComponentType.BluetoothLowEnergy, PropertyName.Flag);

            if (reader.ValueSpan.SequenceEqual("pairing"u8))
            {
                reader.Read();
                while (true)
                {
                    bool readPairingProperty = false;
                    if (reader.TokenType == JsonTokenType.EndObject) break;
                    if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

                    readPairingProperty |= ReadTimeStampProperty(ref reader, "started_at"u8, timestamp, componentId, ComponentType.BluetoothLowEnergy, PropertyName.BlePairingStartedAt);
                    readPairingProperty |= ReadInt64Property(ref reader, "duration"u8, timestamp, componentId, ComponentType.BluetoothLowEnergy, PropertyName.BlePairingDurationSeconds);

                    if (!readPairingProperty) { if (!reader.Read()) return true; }
                }
                reader.Read();
                readProperty = true;
            }

            if (reader.ValueSpan.SequenceEqual("blutrv_assoc"u8))
            {
                reader.Read();
                while (true)
                {
                    bool readAssociationProperty = false;
                    if (reader.TokenType == JsonTokenType.EndObject) break;
                    if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

                    readAssociationProperty |= ReadTimeStampProperty(ref reader, "started_at"u8, timestamp, componentId, ComponentType.BluetoothLowEnergy, PropertyName.BleAssociationStartedAt);
                    readAssociationProperty |= ReadInt64Property(ref reader, "duration"u8, timestamp, componentId, ComponentType.BluetoothLowEnergy, PropertyName.BleAssociationDurationSeconds);

                    if (!readAssociationProperty) { if (!reader.Read()) return true; }
                }
                reader.Read();
                readProperty = true;
            }

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read();
        return null;
    }

    private bool? ReadCbChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        var idSpan = reader.ValueSpan[3..];
        var componentId = byte.Parse(idSpan);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            if (reader.ValueSpan.SequenceEqual("id"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            if (reader.ValueSpan.SequenceEqual("source"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            readProperty |= ReadBoolProperty(ref reader, "output"u8, timestamp, componentId, ComponentType.CB, PropertyName.Output);
            readProperty |= ReadInt64Property(ref reader, "total_cycles"u8, timestamp, componentId, ComponentType.CB, PropertyName.CbTotalCycles);
            readProperty |= ReadBoolProperty(ref reader, "safety"u8, timestamp, componentId, ComponentType.CB, PropertyName.CbSafetyLocked);
            readProperty |= TryParseNullableTemperatureObject(ref reader, "temperature"u8, timestamp, componentId, ComponentType.CB);
            readProperty |= ReadCbErrors(ref reader, timestamp, componentId);

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read();
        return null;
    }

    private bool? ReadCctChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        var idSpan = reader.ValueSpan[4..];
        var componentId = byte.Parse(idSpan);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            if (reader.ValueSpan.SequenceEqual("id"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            if (reader.ValueSpan.SequenceEqual("source"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            if (reader.ValueSpan.SequenceEqual("tag"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            readProperty |= ReadBoolProperty(ref reader, "output"u8, timestamp, componentId, ComponentType.CCT, PropertyName.Output);
            readProperty |= ReadDoubleProperty(ref reader, "brightness"u8, timestamp, componentId, ComponentType.CCT, PropertyName.Brightness);
            readProperty |= ReadDoubleProperty(ref reader, "ct"u8, timestamp, componentId, ComponentType.CCT, PropertyName.ColorTemperatureK);
            readProperty |= ReadTimeStampProperty(ref reader, "timer_started_at"u8, timestamp, componentId, ComponentType.CCT, PropertyName.TimerStatedAt);
            readProperty |= ReadDoubleProperty(ref reader, "timer_duration"u8, timestamp, componentId, ComponentType.CCT, PropertyName.TimerDuration);
            readProperty |= ReadLightSourceTransitionObject(ref reader, "transition"u8, timestamp, componentId, ComponentType.CCT);
            readProperty |= TryParseNullableTemperatureObject(ref reader, "temperature"u8, timestamp, componentId, ComponentType.CCT);
            readProperty |= ReadDoubleProperty(ref reader, "apower"u8, timestamp, componentId, ComponentType.CCT, PropertyName.ActivePower);
            readProperty |= ReadDoubleProperty(ref reader, "voltage"u8, timestamp, componentId, ComponentType.CCT, PropertyName.Voltage);
            readProperty |= ReadDoubleProperty(ref reader, "current"u8, timestamp, componentId, ComponentType.CCT, PropertyName.Current);
            readProperty |= ReadErrors(ref reader, _cctErrors, timestamp, componentId, ComponentType.CCT);

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read();
        return null;
    }

    private bool? ReadDevicePowerChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        var idSpan = reader.ValueSpan[12..];
        var componentId = byte.Parse(idSpan);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            if (reader.ValueSpan.SequenceEqual("id"u8)) { reader.Read(); reader.Read(); readProperty = true; }

            if (reader.ValueSpan.SequenceEqual("battery"u8))
            {
                reader.Read();
                while (true)
                {
                    bool readBatteryProperty = false;
                    if (reader.TokenType == JsonTokenType.EndObject) break;
                    if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

                    readBatteryProperty |= ReadNullableDoubleProperty(ref reader, "V"u8, timestamp, componentId, ComponentType.DevicePower, PropertyName.DeviceBatteryVoltage);
                    readBatteryProperty |= ReadNullableDoubleProperty(ref reader, "percent"u8, timestamp, componentId, ComponentType.DevicePower, PropertyName.DeviceBatteryPercent);

                    if (!readBatteryProperty) { if (!reader.Read()) return true; }
                }
                reader.Read();
                readProperty = true;
            }

            if (reader.ValueSpan.SequenceEqual("external"u8))
            {
                reader.Read();
                while (true)
                {
                    bool readExternalProperty = false;
                    if (reader.TokenType == JsonTokenType.EndObject) break;
                    if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

                    readExternalProperty |= ReadBoolProperty(ref reader, "present"u8, timestamp, componentId, ComponentType.DevicePower, PropertyName.ExternalPowerPresent);

                    if (!readExternalProperty) { if (!reader.Read()) return true; }
                }
                reader.Read();
                readProperty = true;
            }

            readProperty |= ReadErrors(ref reader, _devicePowerErrors, timestamp, componentId, ComponentType.DevicePower);

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read();
        return null;
    }

    private bool? ReadEmChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        var idSpan = reader.ValueSpan[3..];
        var componentId = byte.Parse(idSpan);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            if (reader.ValueSpan.SequenceEqual("id"u8)) { reader.Read(); reader.Read(); readProperty = true; }

            readProperty |= ReadNullableDoubleProperty(ref reader, "a_current"u8, timestamp, componentId, ComponentType.EM, PropertyName.EmPhaseACurrent);
            readProperty |= ReadNullableDoubleProperty(ref reader, "a_voltage"u8, timestamp, componentId, ComponentType.EM, PropertyName.EmPhaseAVoltage);
            readProperty |= ReadNullableDoubleProperty(ref reader, "a_act_power"u8, timestamp, componentId, ComponentType.EM, PropertyName.EmPhaseAActivePower);
            readProperty |= ReadNullableDoubleProperty(ref reader, "a_aprt_power"u8, timestamp, componentId, ComponentType.EM, PropertyName.EmPhaseAApparentPower);
            readProperty |= ReadNullableDoubleProperty(ref reader, "a_pf"u8, timestamp, componentId, ComponentType.EM, PropertyName.EmPhaseAPowerFactor);
            readProperty |= ReadNullableDoubleProperty(ref reader, "a_freq"u8, timestamp, componentId, ComponentType.EM, PropertyName.EmPhaseAFrequency);
            readProperty |= ReadErrorsWithProperty(ref reader, "a_errors"u8, _emErrors, timestamp, componentId, ComponentType.EM, PropertyName.EmPhaseAError);
            readProperty |= ReadFlagsWithProperty(ref reader, "a_flags"u8, _emFlags, timestamp, componentId, ComponentType.EM, PropertyName.EmPhaseAFlag);

            readProperty |= ReadNullableDoubleProperty(ref reader, "b_current"u8, timestamp, componentId, ComponentType.EM, PropertyName.EmPhaseBCurrent);
            readProperty |= ReadNullableDoubleProperty(ref reader, "b_voltage"u8, timestamp, componentId, ComponentType.EM, PropertyName.EmPhaseBVoltage);
            readProperty |= ReadNullableDoubleProperty(ref reader, "b_act_power"u8, timestamp, componentId, ComponentType.EM, PropertyName.EmPhaseBActivePower);
            readProperty |= ReadNullableDoubleProperty(ref reader, "b_aprt_power"u8, timestamp, componentId, ComponentType.EM, PropertyName.EmPhaseBApparentPower);
            readProperty |= ReadNullableDoubleProperty(ref reader, "b_pf"u8, timestamp, componentId, ComponentType.EM, PropertyName.EmPhaseBPowerFactor);
            readProperty |= ReadNullableDoubleProperty(ref reader, "b_freq"u8, timestamp, componentId, ComponentType.EM, PropertyName.EmPhaseBFrequency);
            readProperty |= ReadErrorsWithProperty(ref reader, "b_errors"u8, _emErrors, timestamp, componentId, ComponentType.EM, PropertyName.EmPhaseBError);
            readProperty |= ReadFlagsWithProperty(ref reader, "b_flags"u8, _emFlags, timestamp, componentId, ComponentType.EM, PropertyName.EmPhaseBFlag);

            readProperty |= ReadNullableDoubleProperty(ref reader, "c_current"u8, timestamp, componentId, ComponentType.EM, PropertyName.EmPhaseCCurrent);
            readProperty |= ReadNullableDoubleProperty(ref reader, "c_voltage"u8, timestamp, componentId, ComponentType.EM, PropertyName.EmPhaseCVoltage);
            readProperty |= ReadNullableDoubleProperty(ref reader, "c_act_power"u8, timestamp, componentId, ComponentType.EM, PropertyName.EmPhaseCActivePower);
            readProperty |= ReadNullableDoubleProperty(ref reader, "c_aprt_power"u8, timestamp, componentId, ComponentType.EM, PropertyName.EmPhaseCApparentPower);
            readProperty |= ReadNullableDoubleProperty(ref reader, "c_pf"u8, timestamp, componentId, ComponentType.EM, PropertyName.EmPhaseCPowerFactor);
            readProperty |= ReadNullableDoubleProperty(ref reader, "c_freq"u8, timestamp, componentId, ComponentType.EM, PropertyName.EmPhaseCFrequency);
            readProperty |= ReadErrorsWithProperty(ref reader, "c_errors"u8, _emErrors, timestamp, componentId, ComponentType.EM, PropertyName.EmPhaseCError);
            readProperty |= ReadFlagsWithProperty(ref reader, "c_flags"u8, _emFlags, timestamp, componentId, ComponentType.EM, PropertyName.EmPhaseCFlag);

            readProperty |= ReadNullableDoubleProperty(ref reader, "n_current"u8, timestamp, componentId, ComponentType.EM, PropertyName.EmNeutralCurrent);
            readProperty |= ReadErrorsWithProperty(ref reader, "n_errors"u8, _emErrors, timestamp, componentId, ComponentType.EM, PropertyName.EmNeutralError);
            readProperty |= ReadNullableDoubleProperty(ref reader, "total_current"u8, timestamp, componentId, ComponentType.EM, PropertyName.EmTotalCurrent);
            readProperty |= ReadNullableDoubleProperty(ref reader, "total_act_power"u8, timestamp, componentId, ComponentType.EM, PropertyName.EmTotalActivePower);
            readProperty |= ReadNullableDoubleProperty(ref reader, "total_aprt_power"u8, timestamp, componentId, ComponentType.EM, PropertyName.EmTotalApparentPower);
            readProperty |= ReadEmUserCalibratedPhases(ref reader, timestamp, componentId);
            readProperty |= ReadErrorsWithProperty(ref reader, "errors"u8, _emErrors, timestamp, componentId, ComponentType.EM, PropertyName.Error);

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read();
        return null;
    }

    private bool? ReadEm1Changes(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        var idSpan = reader.ValueSpan[4..];
        var componentId = byte.Parse(idSpan);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            if (reader.ValueSpan.SequenceEqual("id"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            readProperty |= ReadNullableDoubleProperty(ref reader, "current"u8, timestamp, componentId, ComponentType.EM1, PropertyName.Current);
            readProperty |= ReadNullableDoubleProperty(ref reader, "voltage"u8, timestamp, componentId, ComponentType.EM1, PropertyName.Voltage);
            readProperty |= ReadNullableDoubleProperty(ref reader, "act_power"u8, timestamp, componentId, ComponentType.EM1, PropertyName.ActivePower);
            readProperty |= ReadNullableDoubleProperty(ref reader, "aprt_power"u8, timestamp, componentId, ComponentType.EM1, PropertyName.ApperentPower);
            readProperty |= ReadNullableDoubleProperty(ref reader, "pf"u8, timestamp, componentId, ComponentType.EM1, PropertyName.PowerFactor);
            readProperty |= ReadNullableDoubleProperty(ref reader, "freq"u8, timestamp, componentId, ComponentType.EM1, PropertyName.Frequency);
            readProperty |= ReadEm1Calibration(ref reader, timestamp, componentId);
            readProperty |= ReadErrors(ref reader, _em1Errors, timestamp, componentId, ComponentType.EM1);

            readProperty |= ReadFlagsWithProperty(ref reader, "flags"u8, _em1Flags, timestamp, componentId, ComponentType.EM1, PropertyName.Flag);

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read();
        return null;
    }

    private bool? ReadEm1DataChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        var idSpan = reader.ValueSpan[8..];
        var componentId = byte.Parse(idSpan);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            if (reader.ValueSpan.SequenceEqual("id"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            readProperty |= ReadDoubleProperty(ref reader, "total_act_energy"u8, timestamp, componentId, ComponentType.EM1Data, PropertyName.TotalEnergyConsumedWh);
            readProperty |= ReadDoubleProperty(ref reader, "total_act_ret_energy"u8, timestamp, componentId, ComponentType.EM1Data, PropertyName.TotalEnergyReturnedWh);
            readProperty |= ReadErrors(ref reader, _em1DataErrors, timestamp, componentId, ComponentType.EM1Data);

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read();
        return null;
    }

    private bool? ReadEmDataChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        var idSpan = reader.ValueSpan[7..];
        var componentId = byte.Parse(idSpan);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            if (reader.ValueSpan.SequenceEqual("id"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            readProperty |= ReadDoubleProperty(ref reader, "a_total_act_energy"u8, timestamp, componentId, ComponentType.EMData, PropertyName.EmPhaseATotalActiveEnergyWh);
            readProperty |= ReadDoubleProperty(ref reader, "a_total_act_ret_energy"u8, timestamp, componentId, ComponentType.EMData, PropertyName.EmPhaseATotalActiveReturnedEnergyWh);
            readProperty |= ReadDoubleProperty(ref reader, "b_total_act_energy"u8, timestamp, componentId, ComponentType.EMData, PropertyName.EmPhaseBTotalActiveEnergyWh);
            readProperty |= ReadDoubleProperty(ref reader, "b_total_act_ret_energy"u8, timestamp, componentId, ComponentType.EMData, PropertyName.EmPhaseBTotalActiveReturnedEnergyWh);
            readProperty |= ReadDoubleProperty(ref reader, "c_total_act_energy"u8, timestamp, componentId, ComponentType.EMData, PropertyName.EmPhaseCTotalActiveEnergyWh);
            readProperty |= ReadDoubleProperty(ref reader, "c_total_act_ret_energy"u8, timestamp, componentId, ComponentType.EMData, PropertyName.EmPhaseCTotalActiveReturnedEnergyWh);
            readProperty |= ReadDoubleProperty(ref reader, "total_act"u8, timestamp, componentId, ComponentType.EMData, PropertyName.EmTotalActiveEnergyWh);
            readProperty |= ReadDoubleProperty(ref reader, "total_act_ret"u8, timestamp, componentId, ComponentType.EMData, PropertyName.EmTotalActiveReturnedEnergyWh);
            readProperty |= ReadErrors(ref reader, _emDataErrors, timestamp, componentId, ComponentType.EMData);

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read();
        return null;
    }

    private bool? ReadEthernetChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        const byte componentId = 0;
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            readProperty |= ReadIPv4Property(ref reader, "ip"u8, timestamp, componentId, ComponentType.Ethernet, PropertyName.IpAddress);

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read();
        return null;
    }

    private bool? ReadFloodChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        var idSpan = reader.ValueSpan[6..];
        var componentId = byte.Parse(idSpan);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            if (reader.ValueSpan.SequenceEqual("id"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            readProperty |= ReadBoolProperty(ref reader, "alarm"u8, timestamp, componentId, ComponentType.Flood, PropertyName.Alarm);
            readProperty |= ReadBoolProperty(ref reader, "mute"u8, timestamp, componentId, ComponentType.Flood, PropertyName.Mute);
            readProperty |= ReadErrors(ref reader, _floodErrors, timestamp, componentId, ComponentType.Flood);

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read();
        return null;
    }

    private bool? ReadHumidityChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        var idSpan = reader.ValueSpan[9..];
        var componentId = byte.Parse(idSpan);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            if (reader.ValueSpan.SequenceEqual("id"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            readProperty |= ReadNullableDoubleProperty(ref reader, "rh"u8, timestamp, componentId, ComponentType.Humidity, PropertyName.RelativeHumidityPercent);
            readProperty |= ReadErrors(ref reader, _humidityErrors, timestamp, componentId, ComponentType.Humidity);

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read();
        return null;
    }

    private bool? ReadIlluminanceChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        var idSpan = reader.ValueSpan[12..];
        var componentId = byte.Parse(idSpan);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            if (reader.ValueSpan.SequenceEqual("id"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            readProperty |= ReadNullableDoubleProperty(ref reader, "lux"u8, timestamp, componentId, ComponentType.Illuminance, PropertyName.IlluminanceLux);
            readProperty |= ReadIlluminationProperty(ref reader, timestamp, componentId);
            readProperty |= ReadErrors(ref reader, _illuminanceErrors, timestamp, componentId, ComponentType.Illuminance);

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read();
        return null;
    }

    private bool? ReadMatterChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        const byte componentId = 0;
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            readProperty |= ReadInt64Property(ref reader, "num_fabrics"u8, timestamp, componentId, ComponentType.Matter, PropertyName.MatterFabricCount);
            readProperty |= ReadBoolProperty(ref reader, "commissionable"u8, timestamp, componentId, ComponentType.Matter, PropertyName.MatterCommissionable);

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read();
        return null;
    }

    private bool? ReadModbusChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        const byte componentId = 0;
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            readProperty |= ReadBoolProperty(ref reader, "enabled"u8, timestamp, componentId, ComponentType.Modbus, PropertyName.Enabled);

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read();
        return null;
    }

    private bool? ReadMqttChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        const byte componentId = 0;
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            readProperty |= ReadBoolProperty(ref reader, "connected"u8, timestamp, componentId, ComponentType.MQTT, PropertyName.Connected);

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read();
        return null;
    }

    private bool? ReadPresenceChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        const byte componentId = 0;
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            if (reader.ValueSpan.SequenceEqual("live_track"u8))
            {
                reader.Read();
                while (true)
                {
                    bool readLiveTrackProperty = false;
                    if (reader.TokenType == JsonTokenType.EndObject) break;
                    if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

                    readLiveTrackProperty |= ReadTimeStampProperty(ref reader, "timer_started_at"u8, timestamp, componentId, ComponentType.Presence, PropertyName.PresenceLiveTrackStartedAt);
                    readLiveTrackProperty |= ReadDoubleProperty(ref reader, "timer_duration"u8, timestamp, componentId, ComponentType.Presence, PropertyName.PresenceLiveTrackDurationSeconds);
                    readLiveTrackProperty |= ReadDoubleProperty(ref reader, "interval"u8, timestamp, componentId, ComponentType.Presence, PropertyName.PresenceLiveTrackIntervalSeconds);

                    if (!readLiveTrackProperty) { if (!reader.Read()) return true; }
                }
                reader.Read();
                readProperty = true;
            }

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read();
        return null;
    }

    private bool? ReadPresenceZoneChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        var idSpan = reader.ValueSpan[13..];
        var componentId = byte.Parse(idSpan);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            if (reader.ValueSpan.SequenceEqual("id"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            readProperty |= ReadBoolProperty(ref reader, "value"u8, timestamp, componentId, ComponentType.PresenceZone, PropertyName.PresenceZoneState);
            readProperty |= ReadBoolProperty(ref reader, "state"u8, timestamp, componentId, ComponentType.PresenceZone, PropertyName.PresenceZoneState);
            readProperty |= ReadInt64Property(ref reader, "num_objects"u8, timestamp, componentId, ComponentType.PresenceZone, PropertyName.PresenceZoneObjectCount);

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read();
        return null;
    }

    private bool? ReadSmokeChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        var idSpan = reader.ValueSpan[6..];
        var componentId = byte.Parse(idSpan);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            if (reader.ValueSpan.SequenceEqual("id"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            readProperty |= ReadBoolProperty(ref reader, "alarm"u8, timestamp, componentId, ComponentType.Smoke, PropertyName.Alarm);
            readProperty |= ReadBoolProperty(ref reader, "mute"u8, timestamp, componentId, ComponentType.Smoke, PropertyName.Mute);

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read();
        return null;
    }

    private bool? ReadTemperatureChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        var idSpan = reader.ValueSpan[12..];
        var componentId = byte.Parse(idSpan);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            if (reader.ValueSpan.SequenceEqual("id"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            readProperty |= ReadNullableDoubleProperty(ref reader, "tC"u8, timestamp, componentId, ComponentType.Temperature, PropertyName.TemperatureC);
            readProperty |= ReadNullableDoubleProperty(ref reader, "tF"u8, timestamp, componentId, ComponentType.Temperature, PropertyName.TemperatureF);
            readProperty |= ReadErrors(ref reader, _temperatureErrors, timestamp, componentId, ComponentType.Temperature);

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read();
        return null;
    }

    private bool? ReadVoltmeterChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        var idSpan = reader.ValueSpan[10..];
        var componentId = byte.Parse(idSpan);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            if (reader.ValueSpan.SequenceEqual("id"u8)) { reader.Read(); reader.Read(); readProperty = true; }
            readProperty |= ReadNullableDoubleProperty(ref reader, "voltage"u8, timestamp, componentId, ComponentType.Voltmeter, PropertyName.Voltage);
            readProperty |= ReadNullableDoubleProperty(ref reader, "xvoltage"u8, timestamp, componentId, ComponentType.Voltmeter, PropertyName.TransformedVoltage);
            readProperty |= ReadErrors(ref reader, _voltmeterErrors, timestamp, componentId, ComponentType.Voltmeter);

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read();
        return null;
    }

    private bool? ReadWifiChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        const byte componentId = 0;
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            readProperty |= ReadIPv4Property(ref reader, "sta_ip"u8, timestamp, componentId, ComponentType.WiFi, PropertyName.IpAddress);
            readProperty |= ReadStringAsEnum(ref reader, "status"u8, _wifiConnectionStatuses, timestamp, componentId, ComponentType.WiFi, PropertyName.WifiConnectionStatus);

            readProperty |= ReadStringProperty(ref reader, "ssid"u8, timestamp, componentId, ComponentType.WiFi, PropertyName.WifiSsid);
            readProperty |= ReadStringProperty(ref reader, "bssid"u8, timestamp, componentId, ComponentType.WiFi, PropertyName.WifiBssid);
            readProperty |= ReadInt64Property(ref reader, "channel"u8, timestamp, componentId, ComponentType.WiFi, PropertyName.WifiChannel);
            readProperty |= ReadDoubleProperty(ref reader, "rssi"u8, timestamp, componentId, ComponentType.WiFi, PropertyName.WifiRssi);
            readProperty |= ReadInt64Property(ref reader, "ap_client_count"u8, timestamp, componentId, ComponentType.WiFi, PropertyName.WifiAccessPointClientCount);

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read();
        return null;
    }

    private bool? ReadZigbeeChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        const byte componentId = 0;
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            ReadStringAsEnum(ref reader, "network_state"u8, _zigbeeNetworkStates, timestamp, 0, ComponentType.Zigbee, PropertyName.ZigbeeNetworkState);

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read();
        return null;
    }

    private bool? ReadWebSocketChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            readProperty |= ReadBoolProperty(ref reader, "connected"u8, timestamp, 0, ComponentType.OutboundWebsocket, PropertyName.Connected);

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read();
        return null;
    }

    private bool? ReadSysChanges(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        const byte componentId = 0;
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            readProperty |= ReadMacAddressProperty(ref reader, "mac"u8, timestamp, componentId, ComponentType.System, PropertyName.MacAddress);
            readProperty |= ReadBoolProperty(ref reader, "restart_required"u8, timestamp, componentId, ComponentType.System, PropertyName.RestartRequired);

            readProperty |= ReadNullableStringProperty(ref reader, "time"u8, timestamp, componentId, ComponentType.System, PropertyName.Time);
            readProperty |= ReadNullableTimeStampProperty(ref reader, "unixtime"u8, timestamp, componentId, ComponentType.System, PropertyName.UnixTime);
            readProperty |= ReadNullableTimeStampProperty(ref reader, "last_sync_ts"u8, timestamp, componentId, ComponentType.System, PropertyName.LastSyncTimestamp);

            readProperty |= ReadInt64Property(ref reader, "uptime"u8, timestamp, componentId, ComponentType.System, PropertyName.Uptime);
            readProperty |= ReadInt64Property(ref reader, "ram_size"u8, timestamp, componentId, ComponentType.System, PropertyName.RamSize);
            readProperty |= ReadInt64Property(ref reader, "ram_free"u8, timestamp, componentId, ComponentType.System, PropertyName.RamFree);
            readProperty |= ReadInt64Property(ref reader, "fs_size"u8, timestamp, componentId, ComponentType.System, PropertyName.FileSystemSize);
            readProperty |= ReadInt64Property(ref reader, "fs_free"u8, timestamp, componentId, ComponentType.System, PropertyName.FileSystemFree);

            readProperty |= ReadInt64Property(ref reader, "cfg_rev"u8, timestamp, componentId, ComponentType.System, PropertyName.ConfigRevision);
            readProperty |= ReadInt64Property(ref reader, "kvs_rev"u8, timestamp, componentId, ComponentType.System, PropertyName.KvsRevision);

            readProperty |= ReadInt64Property(ref reader, "schedule_rev"u8, timestamp, componentId, ComponentType.System, PropertyName.ScheduleRevision);
            readProperty |= ReadInt64Property(ref reader, "webhook_rev"u8, timestamp, componentId, ComponentType.System, PropertyName.WebhookRevision);
            readProperty |= ReadInt64Property(ref reader, "knx_rev"u8, timestamp, componentId, ComponentType.System, PropertyName.KnxRevision);
            readProperty |= ReadInt64Property(ref reader, "btrelay_rev"u8, timestamp, componentId, ComponentType.System, PropertyName.BluetoothRelayRevision);
            readProperty |= ReadInt64Property(ref reader, "bthc_rev"u8, timestamp, componentId, ComponentType.System, PropertyName.BluetoothHomeControlRevision);

            readProperty |= ReadSysAvailableUpdates(ref reader, timestamp, componentId);
            readProperty |= ReadSysWakeupReason(ref reader, timestamp, componentId);

            readProperty |= ReadInt64Property(ref reader, "wakeup_period"u8, timestamp, componentId, ComponentType.System, PropertyName.WakeupPeriod);
            readProperty |= ReadInt64Property(ref reader, "utc_offset"u8, timestamp, componentId, ComponentType.System, PropertyName.UtcOffset);

            readProperty |= ReadSysProvisioning(ref reader, timestamp, componentId);

            if (!readProperty) { if (!reader.Read()) return true; }
        }

        reader.Read();
        return null;
    }

    private bool ReadSysAvailableUpdates(ref Utf8JsonReader reader, DateTimeOffset timestamp, byte componentId)
    {
        if (!reader.ValueSpan.SequenceEqual("available_updates"u8)) return false;

        reader.Read();

        if (reader.TokenType == JsonTokenType.Null) { reader.Read(); return true; }

        reader.Read();

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            readProperty |= ReadSysAvailableUpdate(ref reader, "beta"u8, timestamp, componentId, PropertyName.BetaUpdateVersion, PropertyName.BetaUpdateSteps);
            readProperty |= ReadSysAvailableUpdate(ref reader, "stable"u8, timestamp, componentId, PropertyName.StableUpdateVersion, PropertyName.StableUpdateSteps);

            if (!readProperty) reader.Read();
        }

        reader.Read();
        return true;
    }

    private bool ReadSysAvailableUpdate(ref Utf8JsonReader reader, ReadOnlySpan<byte> propertyName, DateTimeOffset timestamp, byte componentId, PropertyName versionProperty, PropertyName stepsProperty)
    {
        if (!reader.ValueSpan.SequenceEqual(propertyName)) return false;

        reader.Read();

        if (reader.TokenType == JsonTokenType.Null) { reader.Read(); return true; }

        reader.Read();

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            readProperty |= ReadNullableStringProperty(ref reader, "version"u8, timestamp, componentId, ComponentType.System, versionProperty);
            readProperty |= ReadNullableInt64Property(ref reader, "nsteps"u8, timestamp, componentId, ComponentType.System, stepsProperty);

            if (!readProperty) reader.Read();
        }

        reader.Read();
        return true;
    }

    private bool ReadSysWakeupReason(ref Utf8JsonReader reader, DateTimeOffset timestamp, byte componentId)
    {
        if (!reader.ValueSpan.SequenceEqual("wakeup_reason"u8)) return false;

        reader.Read();

        if (reader.TokenType == JsonTokenType.Null) { reader.Read(); return true; }

        reader.Read();

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            readProperty |= ReadFlagsWithProperty(ref reader, "boot"u8, _sysBootTypes, timestamp, componentId, ComponentType.System, PropertyName.BootType);
            readProperty |= ReadFlagsWithProperty(ref reader, "cause"u8, _sysWakeupCauses, timestamp, componentId, ComponentType.System, PropertyName.WakeupCause);

            if (!readProperty) reader.Read();
        }

        reader.Read();
        return true;
    }

    private bool ReadSysProvisioning(ref Utf8JsonReader reader, DateTimeOffset timestamp, byte componentId)
    {
        if (!reader.ValueSpan.SequenceEqual("provisioning"u8)) return false;

        reader.Read();

        if (reader.TokenType == JsonTokenType.Null) { reader.Read(); return true; }

        reader.Read();

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            readProperty |= ReadFlagsWithProperty(ref reader, "stage"u8, _sysProvisioningStages, timestamp, componentId, ComponentType.System, PropertyName.ProvisioningStage);
            readProperty |= ReadInt64Property(ref reader, "started_at"u8, timestamp, componentId, ComponentType.System, PropertyName.ProvisioningStartedAt);
            readProperty |= ReadInt64Property(ref reader, "started_ut"u8, timestamp, componentId, ComponentType.System, PropertyName.ProvisioningStartedUptime);
            readProperty |= ReadInt64Property(ref reader, "duration"u8, timestamp, componentId, ComponentType.System, PropertyName.ProvisioningDuration);

            if (!readProperty) reader.Read();
        }

        reader.Read();
        return true;
    }

    private bool ReadCbErrors(ref Utf8JsonReader reader, DateTimeOffset timestamp, byte componentId)
    {
        if (!reader.ValueSpan.SequenceEqual("errors"u8)) return false;

        reader.Read();
        reader.Read();

        while (reader.TokenType != JsonTokenType.EndArray)
        {
            CbError error = CbError.Unknown;
            byte? voltmeterId = null;

            if (reader.ValueSpan.SequenceEqual("overtemp"u8)) error = CbError.Overtemp;
            else if (reader.ValueSpan.SequenceEqual("lever"u8)) error = CbError.Lever;
            else if (reader.ValueSpan.StartsWith("overvoltage:"u8))
            {
                error = CbError.Overvoltage;
                if (byte.TryParse(reader.ValueSpan[12..], out var id)) voltmeterId = id;
            }
            else if (reader.ValueSpan.StartsWith("undervoltage:"u8))
            {
                error = CbError.Undervoltage;
                if (byte.TryParse(reader.ValueSpan[13..], out var id)) voltmeterId = id;
            }

            AddMessage(new(timestamp, (long)error, componentId, ComponentType.CB, PropertyName.Error));
            if (voltmeterId.HasValue)
                AddMessage(new(timestamp, (long)voltmeterId.Value, componentId, ComponentType.CB, PropertyName.CbErrorVoltmeterId));

            reader.Read();
        }

        reader.Read();
        return true;
    }

    private bool ReadErrorsWithProperty(ref Utf8JsonReader reader, ReadOnlySpan<byte> propertyName, ErrorDefinition[] errorDefinitions, DateTimeOffset timestamp, byte componentId, ComponentType componentType, PropertyName property)
    {
        if (!reader.ValueSpan.SequenceEqual(propertyName)) return false;

        byte count = 0;
        reader.Read();
        reader.Read();

        while (reader.TokenType != JsonTokenType.EndArray)
        {
            var found = false;

            foreach (var def in errorDefinitions)
            {
                if (reader.ValueSpan.SequenceEqual(def.TextSpan))
                {
                    _errorsToReportBuffer[count] = def.ErrorEnum;
                    count++;
                    reader.Read();
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                _errorsToReportBuffer[count] = 0;
                count++;
                reader.Read();
            }
        }

        reader.Read();

        for (int i = 0; i < count; i++)
            AddMessage(new(timestamp, (long)_errorsToReportBuffer[i], componentId, componentType, property));

        return true;
    }

    private bool ReadFlagsWithProperty(ref Utf8JsonReader reader, ReadOnlySpan<byte> propertyName, FlagDefinition[] flagDefinitions, DateTimeOffset timestamp, byte componentId, ComponentType componentType, PropertyName property)
    {
        if (!reader.ValueSpan.SequenceEqual(propertyName)) return false;

        byte count = 0;
        reader.Read();
        if (reader.TokenType == JsonTokenType.Null)
        {
            reader.Read();
            return true;
        }

        reader.Read();
        while (reader.TokenType != JsonTokenType.EndArray)
        {
            var found = false;

            foreach (var def in flagDefinitions)
            {
                if (reader.ValueSpan.SequenceEqual(def.TextSpan))
                {
                    _flagsToReportBuffer[count] = def.FlagEnum;
                    count++;
                    reader.Read();
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                _flagsToReportBuffer[count] = 0;
                count++;
                reader.Read();
            }
        }

        reader.Read();

        for (int i = 0; i < count; i++)
            AddMessage(new(timestamp, (long)_flagsToReportBuffer[i], componentId, componentType, property));

        return true;
    }

    private bool ReadStringAsEnum(ref Utf8JsonReader reader, ReadOnlySpan<byte> propertyName, FlagDefinition[] flagDefinitions, DateTimeOffset timestamp, byte componentId, ComponentType componentType, PropertyName property)
    {
        if (!reader.ValueSpan.SequenceEqual(propertyName)) return false;

        reader.Read();
        if (reader.TokenType == JsonTokenType.Null)
        {
            AddMessage(new(timestamp, 0L, componentId, componentType, property)); // 0 Should allways be unknown, unrecognized, etc.

            reader.Read();
            return true;
        }

        foreach (var def in flagDefinitions)
        {
            if (reader.ValueSpan.SequenceEqual(def.TextSpan))
            {
                AddMessage(new(timestamp, (long)def.FlagEnum, componentId, componentType, property)); // 0 Should allways be unknown, unrecognized, etc.
                break;
            }
        }

        reader.Read();

        return true;
    }

    private bool ReadEmUserCalibratedPhases(ref Utf8JsonReader reader, DateTimeOffset timestamp, byte componentId)
    {
        if (!reader.ValueSpan.SequenceEqual("user_calibrated_phase"u8)) return false;

        reader.Read();
        reader.Read();

        while (reader.TokenType != JsonTokenType.EndArray)
        {
            var phase = reader.ValueSpan.SequenceEqual("a"u8) ? EmPhase.A :
                        reader.ValueSpan.SequenceEqual("b"u8) ? EmPhase.B :
                        reader.ValueSpan.SequenceEqual("c"u8) ? EmPhase.C : EmPhase.Unknown;
            AddMessage(new(timestamp, (long)phase, componentId, ComponentType.EM, PropertyName.EmUserCalibratedPhase));
            reader.Read();
        }

        reader.Read();
        return true;
    }

    private bool ReadEm1Calibration(ref Utf8JsonReader reader, DateTimeOffset timestamp, byte componentId)
    {
        if (!reader.ValueSpan.SequenceEqual("calibration"u8)) return false;

        reader.Read();

        if (reader.ValueSpan.SequenceEqual("factory"u8))
        {
            AddMessage(new(timestamp, true, componentId, ComponentType.EM1, PropertyName.Em1FactoryCalibration));
        }
        else if (reader.ValueSpan.StartsWith("em1:"u8) && byte.TryParse(reader.ValueSpan[4..], out var sourceId))
        {
            AddMessage(new(timestamp, false, componentId, ComponentType.EM1, PropertyName.Em1FactoryCalibration));
            AddMessage(new(timestamp, (long)sourceId, componentId, ComponentType.EM1, PropertyName.Em1CalibrationSourceComponentId));
        }

        reader.Read();
        return true;
    }

    private bool ReadIlluminationProperty(ref Utf8JsonReader reader, DateTimeOffset timestamp, byte componentId)
    {
        if (!reader.ValueSpan.SequenceEqual("illumination"u8)) return false;

        reader.Read();
        if (reader.TokenType == JsonTokenType.Null)
            AddMessage(new(timestamp, (long)IlluminationLevel.Unknown, componentId, ComponentType.Illuminance, PropertyName.IlluminationLevel));
        else
        {
            var illumination = reader.ValueSpan.SequenceEqual("dark"u8) ? IlluminationLevel.Dark :
                               reader.ValueSpan.SequenceEqual("twilight"u8) ? IlluminationLevel.Twilight :
                               reader.ValueSpan.SequenceEqual("bright"u8) ? IlluminationLevel.Bright : IlluminationLevel.Unknown;
            AddMessage(new(timestamp, (long)illumination, componentId, ComponentType.Illuminance, PropertyName.IlluminationLevel));
        }
        reader.Read();
        return true;
    }

    private bool TryParseNullableTemperatureObject(ref Utf8JsonReader reader, ReadOnlySpan<byte> keyName, DateTimeOffset timestamp, byte componentId, ComponentType componentType)
    {
        if (!reader.ValueSpan.SequenceEqual(keyName)) return false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) continue;

            if (reader.ValueSpan.SequenceEqual("tC"u8))
            {
                reader.Read();
                if (reader.TokenType == JsonTokenType.Null)
                    AddMessage(new(timestamp, (double?)null, componentId, componentType, PropertyName.TemperatureC));
                else
                    AddMessage(new(timestamp, reader.GetDouble(), componentId, componentType, PropertyName.TemperatureC));
                while (reader.TokenType != JsonTokenType.EndObject) reader.Read();
                reader.Read();
                return true;
            }

            if (reader.ValueSpan.SequenceEqual("tF"u8))
            {
                reader.Read();
                if (reader.TokenType == JsonTokenType.Null)
                    AddMessage(new(timestamp, (double?)null, componentId, componentType, PropertyName.TemperatureC));
                else
                {
                    var tempC = (reader.GetDouble() - 32) * 5 / 9;
                    AddMessage(new(timestamp, tempC, componentId, componentType, PropertyName.TemperatureC));
                }
                while (reader.TokenType != JsonTokenType.EndObject) reader.Read();
                reader.Read();
                return true;
            }
        }

        reader.Read();
        return true;
    }

    private bool ReadIPv4Property(ref Utf8JsonReader reader, ReadOnlySpan<byte> propertyName, DateTimeOffset timestamp, byte componentId, ComponentType componentType, PropertyName property)
    {
        if (!reader.ValueSpan.SequenceEqual(propertyName)) return false;

        reader.Read();
        if (reader.TokenType == JsonTokenType.Null)
            AddMessage(new(timestamp, (double?)null, componentId, componentType, property));
        else if (TryPackIPv4(reader.ValueSpan, out var packedIp))
            AddMessage(new(timestamp, packedIp, componentId, componentType, property));

        reader.Read();
        return true;
    }

    private static bool TryPackIPv4(ReadOnlySpan<byte> value, out long packedIp)
    {
        packedIp = 0;
        var address = IPAddress.Parse(value);

        Span<byte> bytes = stackalloc byte[4];
        if (!address.TryWriteBytes(bytes, out _)) return false;

        packedIp = BinaryPrimitives.ReadUInt32BigEndian(bytes);
        return true;
    }

    private static bool TryPackMacAddress(ReadOnlySpan<byte> value, out long packedAddress)
    {
        packedAddress = 0;
        var digits = 0;

        foreach (byte c in value)
        {
            if (c is (byte)':' or (byte)'-')
                continue;

            int hex = c switch
            {
                >= (byte)'0' and <= (byte)'9' => c - '0',
                >= (byte)'A' and <= (byte)'F' => c - 'A' + 10,
                >= (byte)'a' and <= (byte)'f' => c - 'a' + 10,
                _ => -1
            };

            if (hex < 0 || ++digits > 12)
            {
                packedAddress = 0;
                return false;
            }

            packedAddress = (packedAddress << 4) | (uint)hex;
        }

        return digits == 12;
    }

    private void ReadInputCountData(ref Utf8JsonReader reader, DateTimeOffset timestamp, byte componentId)
    {
        int? lastMinute = null;
        double? xLastMinute = null;
        long? minuteTs = null;

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
            readProperty |= ReadDoubleProperty(ref reader, "total"u8, timestamp, componentId, ComponentType.Input, PropertyName.InputTotalCount);
            readProperty |= ReadNullableDoubleProperty(ref reader, "xtotal"u8, timestamp, componentId, ComponentType.Input, PropertyName.InputTotalCountTransformed);

            if (reader.ValueSpan.SequenceEqual("by_minute"u8))
            {
                reader.Read(); // opened array "[";
                reader.Read();
                if (reader.TokenType == JsonTokenType.Number) lastMinute = reader.GetInt32();
                while (reader.TokenType != JsonTokenType.EndArray) { reader.Read(); }
                reader.Read();
                readProperty = true;
            }
            if (reader.ValueSpan.SequenceEqual("xby_minute"u8))
            {
                // Maybe this should be allowed to read nulls (result of failed transformation?)
                reader.Read(); // opened array "[";
                if (reader.TokenType == JsonTokenType.Null)
                {
                    reader.Read(); // move to next property name
                }
                else
                {
                    reader.Read();
                    if (reader.TokenType == JsonTokenType.Number) xLastMinute = reader.GetInt32();
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

            if (lastMinute.HasValue && minuteTs.HasValue)
                AddMessage(new(DateTimeOffset.FromUnixTimeMilliseconds(minuteTs.Value), lastMinute.Value, componentId, ComponentType.Input, PropertyName.InputLastMinuteCount));

            if (xLastMinute.HasValue && minuteTs.HasValue)
                AddMessage(new(DateTimeOffset.FromUnixTimeMilliseconds(minuteTs.Value), xLastMinute.Value, componentId, ComponentType.Input, PropertyName.InputTransformedLastMinuteCount));

            if (!readProperty)
            {
                if (!reader.Read()) return;
            }
        }

        reader.Read(); // reads pass the "}" to next property name
    }


    private bool ReadErrors(ref Utf8JsonReader reader, ErrorDefinition[] errorDefinitions, DateTimeOffset timestamp, byte componentId, ComponentType componentType)
    {
        if (!reader.ValueSpan.SequenceEqual("errors"u8)) return false;

        byte count = 0;
        reader.Read(); // opened array "["
        reader.Read(); // moved to first error string

        while (reader.TokenType != JsonTokenType.EndArray)
        {
            var found = false;
            if (reader.TokenType == JsonTokenType.EndArray) break;

            foreach (var def in errorDefinitions)
            {
                if (reader.ValueSpan.SequenceEqual(def.TextSpan))
                {
                    _errorsToReportBuffer[count] = def.ErrorEnum;
                    count++;
                    reader.Read();
                    found = true;
                }
            }

            if (!found)
            {
                _errorsToReportBuffer[count] = 0; // has to be defined as Unknown
                count++;
                reader.Read();
            }

        }

        if (reader.TokenType == JsonTokenType.EndArray) reader.Read(); // read pass "]"

        for (int i = 0; i < count; i++)
            AddMessage(new(timestamp, (long)_errorsToReportBuffer[i], componentId, componentType, PropertyName.Error));

        return true;
    }

    private static byte ReadFlags(ref Utf8JsonReader reader, FlagDefinition[] flagDefinitions, byte[] flags)
    {
        byte count = 0;
        reader.Read();
        if (reader.TokenType == JsonTokenType.Null)
        {
            reader.Read();
            return 0;
        }

        while (reader.TokenType != JsonTokenType.EndArray)
        {
            var found = false;
            if (reader.TokenType == JsonTokenType.EndArray) break;

            foreach (var def in flagDefinitions)
            {
                if (reader.ValueSpan.SequenceEqual(def.TextSpan))
                {
                    flags[count] = def.FlagEnum;
                    count++;
                    reader.Read();
                    found = true;
                }
            }

            if (!found)
            {
                flags[count] = 0; // has to be defined as Unknown
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

    private readonly struct FlagDefinition(byte[] FlagText, byte FlagEnum)
    {
        public byte[] FlagText { get; } = FlagText;
        public byte FlagEnum { get; } = FlagEnum;
        public ReadOnlySpan<byte> TextSpan => FlagText.AsSpan();
    }

    /// <summary> Reads active energy values and moves reader outside the object. </summary>
    private bool ReadActiveEnergyDataObject(ref Utf8JsonReader reader, ReadOnlySpan<byte> keyName, DateTimeOffset timestamp, byte componentId, ComponentType componentType, bool activeEnergy)
    {
        if (!reader.ValueSpan.SequenceEqual(keyName)) return false;

        long? minuteTs = null;
        double? lastMinute_mWh = null;
        reader.Read(); // opening object now at "{"

        while (true)
        {
            bool readProperty = false;
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }
            readProperty |= ReadDoubleProperty(ref reader, "total"u8, timestamp, componentId, componentType, activeEnergy ? PropertyName.TotalEnergyConsumedWh : PropertyName.TotalEnergyReturnedWh);

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

            if (lastMinute_mWh.HasValue && minuteTs.HasValue)
                AddMessage(new(DateTimeOffset.FromUnixTimeMilliseconds(minuteTs.Value), lastMinute_mWh.Value, componentId, componentType, activeEnergy ? PropertyName.LastMinuteEnergyConsumed_mWh : PropertyName.LastMinuteEnergyReturned_mWh));

            if (!readProperty)
            {
                if (!reader.Read()) return true;
            }

        }

        reader.Read(); // reads pass the "}" to next property name
        return true;
    }

    /// <summary> Moves reader to the opening bracket of a params oraz result object</summary>
    private static bool OpenContainingObject(ref Utf8JsonReader reader, ReadOnlySpan<byte> objectNameToOpen)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                if (reader.ValueTextEquals(objectNameToOpen))
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
            startOfParamsReader.Read();
        }

        startOfParamsReader.Read(); // read unix epoch
        var tsUnix = ParseUnixTimestamp(startOfParamsReader.ValueSpan);
        return DateTimeOffset.FromUnixTimeMilliseconds(tsUnix);
    }


    private bool ReadLightSourceTransitionObject(ref Utf8JsonReader reader, ReadOnlySpan<byte> keyName, DateTimeOffset timestamp, byte componentId, ComponentType componentType)
    {

        if (!reader.ValueSpan.SequenceEqual(keyName)) return false;

        reader.Read(); // open object "{";
        while (true)
        {
            bool readTransitionProperty = false;
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

            if (reader.ValueSpan.SequenceEqual("target"u8))
            {
                reader.Read(); // open object "{";
                while (true)
                {
                    bool readTargetProperty = false;
                    if (reader.TokenType == JsonTokenType.EndObject) break;
                    if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

                    readTargetProperty |= ReadBoolProperty(ref reader, "output"u8, timestamp, componentId, componentType, PropertyName.TransitionTargetOutput);
                    readTargetProperty |= ReadDoubleProperty(ref reader, "brightness"u8, timestamp, componentId, componentType, PropertyName.TransitionTargetBrightness);
                    readTargetProperty |= ReadDoubleProperty(ref reader, "ct"u8, timestamp, componentId, componentType, PropertyName.ColorTemperatureKTarget);
                    readTargetProperty |= ReadRGBProperty(ref reader, "rgb"u8, timestamp, componentId, componentType, PropertyName.RgbTransitionTarget);
                    readTargetProperty |= ReadInt64Property(ref reader, "white"u8, timestamp, componentId, componentType, PropertyName.WhiteTransitionTarget);

                    // some components have it inside target
                    readTargetProperty |= ReadTimeStampProperty(ref reader, "started_at"u8, timestamp, componentId, componentType, PropertyName.TransitionStartedAt);
                    readTargetProperty |= ReadDoubleProperty(ref reader, "duration"u8, timestamp, componentId, componentType, PropertyName.TransitionDuration);

                    if (!readTargetProperty) { if (!reader.Read()) return true; }
                }
                reader.Read();
                readTransitionProperty = true;
            }

            readTransitionProperty |= ReadTimeStampProperty(ref reader, "started_at"u8, timestamp, componentId, componentType, PropertyName.TransitionStartedAt);
            readTransitionProperty |= ReadDoubleProperty(ref reader, "duration"u8, timestamp, componentId, componentType, PropertyName.TransitionDuration);

            if (!readTransitionProperty) { if (!reader.Read()) return true; }
        }
        reader.Read();

        return true;
    }

    /// <summary>Reads the Celsius temperature and consumes the complete temperature object before returning.</summary>
    private bool TryParseTemperatureObject(ref Utf8JsonReader reader, ReadOnlySpan<byte> keyName, DateTimeOffset timestamp, byte componentId, ComponentType componentType)
    {
        if (!reader.ValueSpan.SequenceEqual(keyName)) return false;

        double? tempC = null;

        while (reader.Read()) // move to "{" or to propertny name if caller has opened the object already
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) continue;

            var readTempC = ReadDoubleProperty(ref reader, "tC"u8, timestamp, componentId, componentType, PropertyName.TemperatureC);
            if (readTempC)
            {
                while (reader.TokenType != JsonTokenType.EndObject) reader.Read();
                reader.Read();// move pass "}";
                break;
            }

            if (reader.TokenType == JsonTokenType.PropertyName && reader.ValueSpan.SequenceEqual("tF"u8))
            {
                reader.Read();// move to temp value
                var tempF = reader.GetDouble();
                tempC = (tempF - 32) * 5 / 9;
                AddMessage(new(timestamp, tempC, componentId, componentType, PropertyName.TemperatureC));
                while (reader.TokenType != JsonTokenType.EndObject) reader.Read();
                reader.Read();
                break;
            }

            if (reader.TokenType == JsonTokenType.EndObject) return true;
        }

        return true;
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
    private void AddMessage(ShellyNotifyStatusMessage stateChange)
    {
        if (_messageCount < _messages.Length)
        {
            _messages[_messageCount++] = stateChange;
            return;
        }

        var old = _messages;
        var next = _messagesPool.Rent(old.Length * 2);

        old.AsSpan(0, _messageCount).CopyTo(next);

        _messagesPool.Return(old);
        _messages = next;
        _messages[_messageCount++] = stateChange;
    }

    /// <summary>Writes a string status change immediately when channel capacity is available and delegates only backpressure to the asynchronous path.</summary>
    private void AddStringStatusMessage(ShellyNotifyStatusStringMessage stateChange)
    {
        if (_stringMessageCount < _stringMessages.Length)
        {
            _stringMessages[_stringMessageCount++] = stateChange;
            return;
        }

        var old = _stringMessages;
        var newSize = old.Length == 0 ? 2 : old.Length * 2;
        var next = _stringMessagesPool.Rent(newSize);

        old.AsSpan(0, _stringMessageCount).CopyTo(next);

        _stringMessagesPool.Return(old);
        _stringMessages = next;
        _stringMessages[_stringMessageCount++] = stateChange;
    }
}
