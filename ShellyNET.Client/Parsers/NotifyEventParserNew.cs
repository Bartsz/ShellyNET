using ShellyNET.Client.Model;
using System.Buffers;

namespace ShellyNET.Client.Parsers123;

public interface IShellyComponentEvent
{
    ComponentType Source { get; }
    DateTimeOffset Timestamp { get; }
    ushort? Id { get; }
}


public enum CommonEventKind
{
    Unknown = 0,
    ConfigChanged
}

public enum CameraEventKind
{
    Unknown = 0,
    MediaReady,
    UploadComplete,
    UploadFailed
}

public enum BleEventKind
{
    Unknown = 0,
    PairedDeviceAdded,
    PairedDeviceRemoved
}

public enum DaliEventKind
{
    Unknown = 0,
    ScanComplete,
    PingComplete
}

public enum InputEventKind
{
    Unknown = 0,
    BtnDown,
    BtnUp,
    SinglePush,
    DoublePush,
    TriplePush,
    LongPush
}

public enum PresenceEventKind
{
    Unknown = 0,
    Track,
    NoTrack
}

public enum PresenceZoneEventKind
{
    Unknown = 0,
    Presence,
    Counter,
    Enter,
    Leave
}

public enum SystemEventKind
{
    Unknown = 0,
    OtaBegin,
    OtaProgress,
    OtaSuccess,
    OtaError,
    Sleep,
    ScheduledRestart,
    ComponentAdded,
    ComponentRemoved,
    SysBtnDown,
    SysBtnUp,
    SysBtnPush,
    ProvisionPending,
    ProvisionConfirmed,
    ProvisionComplete,
    ProvisionLocked
}

public enum WifiEventKind
{
    Unknown = 0,
    StaConnectFail,
    StaDisconnected,
    StaConnecting,
    StaConnected,
    StaIpAcquired
}

public enum BTHomeEventKind
{
    Unknown = 0,
    DeviceDiscovered,
    DiscoveryDone
}

public enum BTHomeDeviceEventKind
{
    Unknown = 0,
    NewObjects,
    SinglePush,
    DoublePush,
    TriplePush,
    LongPush,
    LongDoublePush,
    LongTriplePush,
    RotateLeft,
    RotateRight
}

public enum EmDataEventKind
{
    Unknown = 0,
    Data
}

public enum Em1DataEventKind
{
    Unknown = 0,
    Data
}

public enum ButtonEventKind
{
    Unknown = 0,
    SinglePush,
    DoublePush,
    TriplePush,
    LongPush
}

public enum ScriptEventKind
{
    Unknown = 0,
    Custom
}

public sealed class BlueScanResultsEvent : IShellyComponentEvent
{
    public ComponentType Source { get; }
    public DateTimeOffset Timestamp { get; }
    public ushort? Id { get; }
    public long MacAddress { get; set; }
    public byte AddressType { get; set; }
    public float Rsii { get; set; }
    public string Name { get; set; }

    public BlueScanResultsEvent(DateTimeOffset timestamp, ComponentType source, long macAddress, byte addressType, float rsii, string name)
    {
        Timestamp = timestamp;
        Source = source;
        MacAddress = macAddress;
        AddressType = addressType;
        Rsii = rsii;
        Name = name;
    }
}

public sealed class ConfigurationChangedEvent : IShellyComponentEvent
{
    private readonly ComponentType _source;

    public ConfigurationChangedEvent(DateTimeOffset timestamp, ComponentType source, ushort? id, bool restartRequired, long? configRevision)
    {
        Timestamp = timestamp;
        _source = source;
        Id = id;
        RestartRequired = restartRequired;
        ConfigRevision = configRevision;
    }

    public ComponentType Source => _source;
    public DateTimeOffset Timestamp { get; }
    public ushort? Id { get; }
    public CommonEventKind EventKind => CommonEventKind.ConfigChanged;
    public bool RestartRequired { get; }
    public long? ConfigRevision { get; }
}

public sealed class CameraEvent : IShellyComponentEvent
{
    public CameraEvent(DateTimeOffset timestamp, ushort id, CameraEventKind eventKind, string? recordingId, string? mediaId, string? mimeType, string? encryptedRecordingKey)
    {
        Timestamp = timestamp;
        Id = id;
        EventKind = eventKind;
        RecordingId = recordingId;
        MediaId = mediaId;
        MimeType = mimeType;
        EncryptedRecordingKey = encryptedRecordingKey;
    }

    public ComponentType Source => ComponentType.Camera;
    public DateTimeOffset Timestamp { get; }
    public ushort? Id { get; }
    public CameraEventKind EventKind { get; }
    public string? RecordingId { get; }
    public string? MediaId { get; }
    public string? MimeType { get; }
    public string? EncryptedRecordingKey { get; }
}

public sealed class BlePairedDeviceInfo
{
    public BlePairedDeviceInfo(string? address, long? creationTimestamp, long? accessTimestamp)
    {
        Address = address;
        CreationTimestamp = creationTimestamp;
        AccessTimestamp = accessTimestamp;
    }

    public string? Address { get; }
    public long? CreationTimestamp { get; }
    public long? AccessTimestamp { get; }
}

public sealed class BleEvent : IShellyComponentEvent
{
    public BleEvent(DateTimeOffset timestamp, BleEventKind eventKind, BlePairedDeviceInfo? info)
    {
        Timestamp = timestamp;
        EventKind = eventKind;
        Info = info;
    }

    public ComponentType Source => ComponentType.BluetoothLowEnergy;
    public DateTimeOffset Timestamp { get; }
    public ushort? Id => null;
    public BleEventKind EventKind { get; }
    public BlePairedDeviceInfo? Info { get; }
}

public sealed class DaliEvent : IShellyComponentEvent
{
    public DaliEvent(DateTimeOffset timestamp, DaliEventKind eventKind, int? controlGearCount)
    {
        Timestamp = timestamp;
        EventKind = eventKind;
        ControlGearCount = controlGearCount;
    }

    public ComponentType Source => ComponentType.DALI;
    public DateTimeOffset Timestamp { get; }
    public ushort? Id => null;
    public DaliEventKind EventKind { get; }
    public int? ControlGearCount { get; }
}

public sealed class InputEvent : IShellyComponentEvent
{
    public InputEvent(DateTimeOffset timestamp, ushort id, InputEventKind eventKind)
    {
        Timestamp = timestamp;
        Id = id;
        EventKind = eventKind;
    }

    public ComponentType Source => ComponentType.Input;
    public DateTimeOffset Timestamp { get; }
    public ushort? Id { get; }
    public InputEventKind EventKind { get; }
}

public sealed class PresenceTrackedObject
{
    public PresenceTrackedObject(int id, double x, double y, double z, double minZ, double maxZ)
    {
        Id = id;
        X = x;
        Y = y;
        Z = z;
        MinZ = minZ;
        MaxZ = maxZ;
    }

    public int Id { get; }
    public double X { get; }
    public double Y { get; }
    public double Z { get; }
    public double MinZ { get; }
    public double MaxZ { get; }
}

public sealed class PresenceEvent : IShellyComponentEvent
{
    public PresenceEvent(DateTimeOffset timestamp, PresenceEventKind eventKind, PresenceTrackedObject[] objects)
    {
        Timestamp = timestamp;
        EventKind = eventKind;
        Objects = objects;
    }

    public ComponentType Source => ComponentType.Presence;
    public DateTimeOffset Timestamp { get; }
    public ushort? Id => null;
    public PresenceEventKind EventKind { get; }
    public PresenceTrackedObject[] Objects { get; }
}

public readonly struct PresenceZoneObject
{
    public PresenceZoneObject(double x, double y)
    {
        X = x;
        Y = y;
    }

    public double X { get; }
    public double Y { get; }
}

public sealed class PresenceZoneEvent : IShellyComponentEvent
{
    public PresenceZoneEvent(DateTimeOffset timestamp, ushort id, PresenceZoneEventKind eventKind,
                             bool? value, int? objectCount, PresenceZoneObject[] objects)
    {
        Timestamp = timestamp;
        Id = id;
        EventKind = eventKind;
        Value = value;
        ObjectCount = objectCount;
        Objects = objects;
    }

    public ComponentType Source => ComponentType.PresenceZone;
    public DateTimeOffset Timestamp { get; }
    public ushort? Id { get; }
    public PresenceZoneEventKind EventKind { get; }
    public bool? Value { get; }
    public int? ObjectCount { get; }
    public PresenceZoneObject[] Objects { get; }
}

public sealed class SystemEvent : IShellyComponentEvent
{
    public SystemEvent(DateTimeOffset timestamp, SystemEventKind eventKind, string? message,
                       int? progressPercent, int? timeMs, string? target,
                       ComponentType targetComponent, ushort? targetId,
                       bool? restartRequired, long? configRevision)
    {
        Timestamp = timestamp;
        EventKind = eventKind;
        Message = message;
        ProgressPercent = progressPercent;
        TimeMs = timeMs;
        Target = target;
        TargetComponent = targetComponent;
        TargetId = targetId;
        RestartRequired = restartRequired;
        ConfigRevision = configRevision;
    }

    public ComponentType Source => ComponentType.System;
    public DateTimeOffset Timestamp { get; }
    public ushort? Id => null;
    public SystemEventKind EventKind { get; }
    public string? Message { get; }
    public int? ProgressPercent { get; }
    public int? TimeMs { get; }
    public string? Target { get; }
    public ComponentType TargetComponent { get; }
    public ushort? TargetId { get; }
    public bool? RestartRequired { get; }
    public long? ConfigRevision { get; }
}

public sealed class WifiEvent : IShellyComponentEvent
{
    public WifiEvent(DateTimeOffset timestamp, WifiEventKind eventKind, int? reason,
                     string? staIp, string? ssid, int? channel, int? rssi)
    {
        Timestamp = timestamp;
        EventKind = eventKind;
        Reason = reason;
        StaIp = staIp;
        Ssid = ssid;
        Channel = channel;
        Rssi = rssi;
    }

    public ComponentType Source => ComponentType.WiFi;
    public DateTimeOffset Timestamp { get; }
    public ushort? Id => null;
    public WifiEventKind EventKind { get; }
    public int? Reason { get; }
    public string? StaIp { get; }
    public string? Ssid { get; }
    public int? Channel { get; }
    public int? Rssi { get; }
}

public sealed class BTHomeShellyManufacturerData
{
    public BTHomeShellyManufacturerData(int? flags, int? modelId, string? mac)
    {
        Flags = flags;
        ModelId = modelId;
        Mac = mac;
    }

    public int? Flags { get; }
    public int? ModelId { get; }
    public string? Mac { get; }
}

public sealed class BTHomeDiscoveredDevice
{
    public BTHomeDiscoveredDevice(string? address, string? localName, int? rssi, bool? encrypted, BTHomeShellyManufacturerData? shellyManufacturerData)
    {
        Address = address;
        LocalName = localName;
        Rssi = rssi;
        Encrypted = encrypted;
        ShellyManufacturerData = shellyManufacturerData;
    }

    public string? Address { get; }
    public string? LocalName { get; }
    public int? Rssi { get; }
    public bool? Encrypted { get; }
    public BTHomeShellyManufacturerData? ShellyManufacturerData { get; }
}

public sealed class BTHomeEvent : IShellyComponentEvent
{
    public BTHomeEvent(DateTimeOffset timestamp, BTHomeEventKind eventKind, BTHomeDiscoveredDevice? device, int? deviceCount)
    {
        Timestamp = timestamp;
        EventKind = eventKind;
        Device = device;
        DeviceCount = deviceCount;
    }

    public ComponentType Source => ComponentType.BTHome;
    public DateTimeOffset Timestamp { get; }
    public ushort? Id => null;
    public BTHomeEventKind EventKind { get; }
    public BTHomeDiscoveredDevice? Device { get; }
    public int? DeviceCount { get; }
}

public sealed class BTHomeDeviceEvent : IShellyComponentEvent
{
    public BTHomeDeviceEvent(DateTimeOffset timestamp, ushort id, BTHomeDeviceEventKind eventKind, int? objectIndex, int? channel, string? sensorsJson)
    {
        Timestamp = timestamp;
        Id = id;
        EventKind = eventKind;
        ObjectIndex = objectIndex;
        Channel = channel;
        SensorsJson = sensorsJson;
    }

    public ComponentType Source => ComponentType.BTHomeDevice;
    public DateTimeOffset Timestamp { get; }
    public ushort? Id { get; }
    public BTHomeDeviceEventKind EventKind { get; }
    public int? ObjectIndex { get; }
    public int? Channel { get; }
    public string? SensorsJson { get; }
}

public sealed class EnergyDataRecord
{
    public EnergyDataRecord(long timestamp, double? period, double[][] values)
    {
        Timestamp = timestamp;
        Period = period;
        Values = values;
    }

    public long Timestamp { get; }
    public double? Period { get; }
    public double[][] Values { get; }
}

public sealed class EmDataEvent : IShellyComponentEvent
{
    public EmDataEvent(DateTimeOffset timestamp, ushort id, EmDataEventKind eventKind, EnergyDataRecord[] data)
    {
        Timestamp = timestamp;
        Id = id;
        EventKind = eventKind;
        Data = data;
    }

    public ComponentType Source => ComponentType.EMData;
    public DateTimeOffset Timestamp { get; }
    public ushort? Id { get; }
    public EmDataEventKind EventKind { get; }
    public EnergyDataRecord[] Data { get; }
}

public sealed class Em1DataEvent : IShellyComponentEvent
{
    public Em1DataEvent(DateTimeOffset timestamp, ushort id, Em1DataEventKind eventKind, EnergyDataRecord[] data)
    {
        Timestamp = timestamp;
        Id = id;
        EventKind = eventKind;
        Data = data;
    }

    public ComponentType Source => ComponentType.EM1Data;
    public DateTimeOffset Timestamp { get; }
    public ushort? Id { get; }
    public Em1DataEventKind EventKind { get; }
    public EnergyDataRecord[] Data { get; }
}

public sealed class ButtonEvent : IShellyComponentEvent
{
    public ButtonEvent(DateTimeOffset timestamp, ushort id, ButtonEventKind eventKind)
    {
        Timestamp = timestamp;
        Id = id;
        EventKind = eventKind;
    }

    public ComponentType Source => ComponentType.Button;
    public DateTimeOffset Timestamp { get; }
    public ushort? Id { get; }
    public ButtonEventKind EventKind { get; }
}

public sealed class ScriptEvent : IShellyComponentEvent
{
    public ScriptEvent(DateTimeOffset timestamp, ushort id, ScriptEventKind eventKind, string eventName, string? dataJson)
    {
        Timestamp = timestamp;
        Id = id;
        EventKind = eventKind;
        EventName = eventName;
        DataJson = dataJson;
    }

    public ComponentType Source => ComponentType.Script;
    public DateTimeOffset Timestamp { get; }
    public ushort? Id { get; }
    public ScriptEventKind EventKind { get; }
    public string EventName { get; }
    public string? DataJson { get; }
}

public readonly struct ShellyNotifyEventParseResult : IDisposable
{
    private readonly ArrayPool<IShellyComponentEvent>? _eventsPool;
    private readonly IShellyComponentEvent[] _events;

    public ShellyNotifyEventParseResult() => _events = [];

    public ShellyNotifyEventParseResult(IShellyComponentEvent[] events, ArrayPool<IShellyComponentEvent> eventsPool, ushort eventsCount)
    {
        _events = events;
        _eventsPool = eventsPool;
        EventsCount = eventsCount;
    }

    public ushort EventsCount { get; }

    public ReadOnlySpan<IShellyComponentEvent> EventsSpan => _events.AsSpan(0, EventsCount);

    public void Dispose()
    {
        _eventsPool?.Return(_events, clearArray: true);
    }
}

public sealed class ShellyNotifyEventParser
{
    private readonly ArrayPool<IShellyComponentEvent> _eventsPool = ArrayPool<IShellyComponentEvent>.Shared;
    private IShellyComponentEvent[] _events = [];
    private ushort _eventsCount = 0;

    public ShellyNotifyEventParseResult ProcessNotifyEventMessage(Memory<byte> message, CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return new();

        var reader = new Utf8JsonReader(message.Span);

        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return new();

        _eventsCount = 0;
        _events = _eventsPool.Rent(4);

        var timestampReader = reader;
        var fallbackTimestamp = FindAndParseTimestamp(ref timestampReader);

        if (!OpenEventsArray(ref reader, out var timestamp))
        {
            _eventsPool.Return(_events, clearArray: true);
            _events = [];
            return new();
        }

        if (timestamp == default) timestamp = fallbackTimestamp;

        ParseEvents(ref reader, timestamp);

        return new(_events, _eventsPool, _eventsCount);
    }

    private void ParseEvents(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        try
        {
            if (!reader.Read()) return; // first event object or EndArray

            while (true)
            {
                if (reader.TokenType == JsonTokenType.EndArray) return;
                if (reader.TokenType != JsonTokenType.StartObject) { if (!reader.Read()) return; continue; }

                var eventReader = reader;
                FindEventRouting(ref eventReader, out var componentType, out var isConfigChanged, out var id);

                IShellyComponentEvent? eventMessage;

                if (isConfigChanged) eventMessage = ReadConfigurationChangedEvent(ref reader, timestamp, componentType);
                else eventMessage = componentType switch
                {
                    ComponentType.Camera => ReadCameraEvent(ref reader, timestamp),
                    ComponentType.BluetoothLowEnergy => ReadBleEvent(ref reader, timestamp),
                    ComponentType.DALI => ReadDaliEvent(ref reader, timestamp),
                    ComponentType.Input => ReadInputEvent(ref reader, timestamp),
                    ComponentType.Presence => ReadPresenceEvent(ref reader, timestamp),
                    ComponentType.PresenceZone => ReadPresenceZoneEvent(ref reader, timestamp),
                    ComponentType.System => ReadSystemEvent(ref reader, timestamp),
                    ComponentType.WiFi => ReadWifiEvent(ref reader, timestamp),
                    ComponentType.BTHome => ReadBTHomeEvent(ref reader, timestamp),
                    ComponentType.BTHomeDevice => ReadBTHomeDeviceEvent(ref reader, timestamp),
                    ComponentType.EMData => ReadEmDataEvent(ref reader, timestamp),
                    ComponentType.EM1Data => ReadEm1DataEvent(ref reader, timestamp),
                    ComponentType.Button => ReadButtonEvent(ref reader, timestamp),
                    ComponentType.Script => ReadScriptEvent(ref reader, timestamp),
                    _ => SkipUnsupportedEvent(ref reader)
                };

                if (eventMessage is not null) AddEvent(eventMessage);
            }
        }
        catch
        {
            return;
        }
    }

    private static void FindEventRouting(ref Utf8JsonReader reader, out ComponentType componentType, out bool isConfigChanged, out byte id)
    {
        componentType = ComponentType.Unknown;
        isConfigChanged = false;
        id = 0;
        bool eventIsConfigChanged = false;
        bool hasConfigChangeMarker = false;

        if (!reader.Read()) return;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject)
            {
                isConfigChanged = eventIsConfigChanged && hasConfigChangeMarker;
                return;
            }
            if (reader.TokenType != JsonTokenType.PropertyName) { if (!reader.Read()) return; continue; }

            if (reader.ValueSpan.SequenceEqual("component"u8))
            {
                if (!reader.Read()) return;
                componentType = GetComponentType(reader.ValueSpan);
                if (!reader.Read()) return;
                readProperty = true;
            }


            if (reader.ValueSpan.SequenceEqual("id"u8))
            {
                reader.Read();
                var ushortId = ushort.Parse(reader.ValueSpan);
                id = (byte)((ushortId > byte.MaxValue) ? ushortId - 200 : ushortId);
                reader.Read();
                readProperty = true;
            }

            if (reader.ValueSpan.SequenceEqual("event"u8))
            {
                if (!reader.Read()) return;
                eventIsConfigChanged = reader.ValueSpan.SequenceEqual("config_changed"u8);
                if (!reader.Read()) return;
                readProperty = true;
            }

            if (reader.ValueSpan.SequenceEqual("cfg_rev"u8) ||
                reader.ValueSpan.SequenceEqual("restart_required"u8) ||
                reader.ValueSpan.SequenceEqual("restart_require"u8))
            {
                hasConfigChangeMarker = true;
            }

            if (!readProperty)
            {
                if (!SkipProperty(ref reader)) return;
            }
        }
    }

    private ConfigurationChangedEvent? ReadConfigurationChangedEvent(ref Utf8JsonReader reader, DateTimeOffset timestamp, ComponentType componentType)
    {
        ushort? id = null;
        bool restartRequired = false;
        long? configRevision = null;
        long eventTimestamp = 0;

        if (!reader.Read()) return null;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { if (!reader.Read()) return null; continue; }

            readProperty |= ReadComponentProperty(ref reader, ref componentType, ref id);
            readProperty |= ReadNullableComponentIdProperty(ref reader, "id"u8, ref id);
            readProperty |= ReadBoolProperty(ref reader, "restart_required"u8, ref restartRequired);
            // "restart_require" is present in one generic documentation example; accepting it is harmless
            // and keeps the parser tolerant to that spelling if emitted by old firmware.
            readProperty |= ReadBoolProperty(ref reader, "restart_require"u8, ref restartRequired);
            readProperty |= ReadNullableInt64Property(ref reader, "cfg_rev"u8, ref configRevision);
            readProperty |= ReadTimestampProperty(ref reader, "ts"u8, ref eventTimestamp);

            if (reader.ValueSpan.SequenceEqual("event"u8))
            {
                reader.Read();
                reader.Read();
                readProperty = true;
            }

            if (!readProperty) { if (!SkipProperty(ref reader)) return null; }
        }

        reader.Read();
        return new(GetEventTimestamp(eventTimestamp, timestamp), componentType, id, restartRequired, configRevision);
    }

    private CameraEvent? ReadCameraEvent(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        ushort? id = null;
        CameraEventKind eventKind = CameraEventKind.Unknown;
        string? recordingId = null;
        string? mediaId = null;
        string? mimeType = null;
        string? encryptedRecordingKey = null;
        long eventTimestamp = 0;
        var componentType = ComponentType.Camera;

        if (!reader.Read()) return null;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { if (!reader.Read()) return null; continue; }

            readProperty |= ReadComponentProperty(ref reader, ref componentType, ref id);
            readProperty |= ReadNullableComponentIdProperty(ref reader, "id"u8, ref id);

            if (reader.ValueSpan.SequenceEqual("event"u8))
            {
                reader.Read();
                eventKind = GetCameraEventKind(reader.ValueSpan);
                reader.Read();
                readProperty = true;
            }

            readProperty |= ReadNullableStringProperty(ref reader, "rec_id"u8, ref recordingId);
            readProperty |= ReadNullableStringProperty(ref reader, "media_id"u8, ref mediaId);
            readProperty |= ReadNullableStringProperty(ref reader, "mime_type"u8, ref mimeType);
            readProperty |= ReadNullableStringProperty(ref reader, "rec_key_enc"u8, ref encryptedRecordingKey);
            readProperty |= ReadTimestampProperty(ref reader, "ts"u8, ref eventTimestamp);

            if (!readProperty) { if (!SkipProperty(ref reader)) return null; }
        }

        reader.Read();
        if (!id.HasValue) return null;

        return new(GetEventTimestamp(eventTimestamp, timestamp), id.Value, eventKind,
                   recordingId, mediaId, mimeType, encryptedRecordingKey);
    }

    private BleEvent? ReadBleEvent(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        BleEventKind eventKind = BleEventKind.Unknown;
        BlePairedDeviceInfo? info = null;
        long eventTimestamp = 0;
        ushort? id = null;
        var componentType = ComponentType.BluetoothLowEnergy;

        if (!reader.Read()) return null;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { if (!reader.Read()) return null; continue; }

            readProperty |= ReadComponentProperty(ref reader, ref componentType, ref id);

            if (reader.ValueSpan.SequenceEqual("event"u8))
            {
                reader.Read();
                eventKind = GetBleEventKind(reader.ValueSpan);
                reader.Read();
                readProperty = true;
            }

            if (reader.ValueSpan.SequenceEqual("info"u8))
            {
                info = ReadBlePairedDeviceInfo(ref reader);
                readProperty = true;
            }

            readProperty |= ReadTimestampProperty(ref reader, "ts"u8, ref eventTimestamp);

            if (!readProperty) { if (!SkipProperty(ref reader)) return null; }
        }

        reader.Read();
        return new(GetEventTimestamp(eventTimestamp, timestamp), eventKind, info);
    }

    private DaliEvent? ReadDaliEvent(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        DaliEventKind eventKind = DaliEventKind.Unknown;
        int? controlGearCount = null;
        long eventTimestamp = 0;
        ushort? id = null;
        var componentType = ComponentType.DALI;

        if (!reader.Read()) return null;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { if (!reader.Read()) return null; continue; }

            readProperty |= ReadComponentProperty(ref reader, ref componentType, ref id);

            if (reader.ValueSpan.SequenceEqual("event"u8))
            {
                reader.Read();
                eventKind = GetDaliEventKind(reader.ValueSpan);
                reader.Read();
                readProperty = true;
            }

            readProperty |= ReadNullableInt32Property(ref reader, "cg_count"u8, ref controlGearCount);
            readProperty |= ReadTimestampProperty(ref reader, "ts"u8, ref eventTimestamp);

            if (!readProperty) { if (!SkipProperty(ref reader)) return null; }
        }

        reader.Read();
        return new(GetEventTimestamp(eventTimestamp, timestamp), eventKind, controlGearCount);
    }

    private InputEvent? ReadInputEvent(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        ushort? id = null;
        InputEventKind eventKind = InputEventKind.Unknown;
        long eventTimestamp = 0;
        var componentType = ComponentType.Input;

        if (!reader.Read()) return null;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { if (!reader.Read()) return null; continue; }

            readProperty |= ReadComponentProperty(ref reader, ref componentType, ref id);
            readProperty |= ReadNullableComponentIdProperty(ref reader, "id"u8, ref id);

            if (reader.ValueSpan.SequenceEqual("event"u8))
            {
                reader.Read();
                eventKind = GetInputEventKind(reader.ValueSpan);
                reader.Read();
                readProperty = true;
            }

            readProperty |= ReadTimestampProperty(ref reader, "ts"u8, ref eventTimestamp);

            if (!readProperty) { if (!SkipProperty(ref reader)) return null; }
        }

        reader.Read();
        if (!id.HasValue) return null;

        return new(GetEventTimestamp(eventTimestamp, timestamp), id.Value, eventKind);
    }

    private PresenceEvent? ReadPresenceEvent(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        PresenceEventKind eventKind = PresenceEventKind.Unknown;
        PresenceTrackedObject[] objects = [];
        long eventTimestamp = 0;
        ushort? id = null;
        var componentType = ComponentType.Presence;

        if (!reader.Read()) return null;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { if (!reader.Read()) return null; continue; }

            readProperty |= ReadComponentProperty(ref reader, ref componentType, ref id);

            if (reader.ValueSpan.SequenceEqual("event"u8))
            {
                reader.Read();
                eventKind = GetPresenceEventKind(reader.ValueSpan);
                reader.Read();
                readProperty = true;
            }

            if (reader.ValueSpan.SequenceEqual("object"u8))
            {
                objects = ReadPresenceObjects(ref reader);
                readProperty = true;
            }

            readProperty |= ReadTimestampProperty(ref reader, "ts"u8, ref eventTimestamp);

            if (!readProperty) { if (!SkipProperty(ref reader)) return null; }
        }

        reader.Read();
        return new(GetEventTimestamp(eventTimestamp, timestamp), eventKind, objects);
    }

    private PresenceZoneEvent? ReadPresenceZoneEvent(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        ushort? id = null;
        PresenceZoneEventKind eventKind = PresenceZoneEventKind.Unknown;
        bool? value = null;
        int? objectCount = null;
        PresenceZoneObject[] objects = [];
        long eventTimestamp = 0;
        var componentType = ComponentType.PresenceZone;

        if (!reader.Read()) return null;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { if (!reader.Read()) return null; continue; }

            readProperty |= ReadComponentProperty(ref reader, ref componentType, ref id);
            readProperty |= ReadNullableComponentIdProperty(ref reader, "id"u8, ref id);

            if (reader.ValueSpan.SequenceEqual("event"u8))
            {
                reader.Read();
                eventKind = GetPresenceZoneEventKind(reader.ValueSpan);
                reader.Read();
                readProperty = true;
            }

            readProperty |= ReadNullableBoolProperty(ref reader, "value"u8, ref value);
            readProperty |= ReadNullableInt32Property(ref reader, "num_objects"u8, ref objectCount);

            if (reader.ValueSpan.SequenceEqual("object"u8))
            {
                objects = ReadPresenceZoneObjects(ref reader);
                readProperty = true;
            }

            readProperty |= ReadTimestampProperty(ref reader, "ts"u8, ref eventTimestamp);

            if (!readProperty) { if (!SkipProperty(ref reader)) return null; }
        }

        reader.Read();
        if (!id.HasValue) return null;

        return new(GetEventTimestamp(eventTimestamp, timestamp), id.Value, eventKind,
                   value, objectCount, objects);
    }

    private SystemEvent? ReadSystemEvent(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        SystemEventKind eventKind = SystemEventKind.Unknown;
        string? message = null;
        int? progressPercent = null;
        int? timeMs = null;
        string? target = null;
        ComponentType targetComponent = ComponentType.Unknown;
        ushort? targetId = null;
        bool? restartRequired = null;
        long? configRevision = null;
        long eventTimestamp = 0;
        ushort? id = null;
        var componentType = ComponentType.System;

        if (!reader.Read()) return null;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { if (!reader.Read()) return null; continue; }

            readProperty |= ReadComponentProperty(ref reader, ref componentType, ref id);

            if (reader.ValueSpan.SequenceEqual("event"u8))
            {
                reader.Read();
                eventKind = GetSystemEventKind(reader.ValueSpan);
                reader.Read();
                readProperty = true;
            }

            readProperty |= ReadNullableStringProperty(ref reader, "msg"u8, ref message);
            readProperty |= ReadNullableInt32Property(ref reader, "progress_percent"u8, ref progressPercent);
            readProperty |= ReadNullableInt32Property(ref reader, "time_ms"u8, ref timeMs);

            if (reader.ValueSpan.SequenceEqual("target"u8))
            {
                reader.Read();

                if (reader.TokenType == JsonTokenType.String)
                {
                    targetComponent = GetComponentType(reader.ValueSpan);
                    targetId = GetComponentId(reader.ValueSpan);
                    target = reader.GetString();
                }

                reader.Read();
                readProperty = true;
            }

            readProperty |= ReadNullableBoolProperty(ref reader, "restart_required"u8, ref restartRequired);
            readProperty |= ReadNullableInt64Property(ref reader, "cfg_rev"u8, ref configRevision);
            readProperty |= ReadTimestampProperty(ref reader, "ts"u8, ref eventTimestamp);

            if (!readProperty) { if (!SkipProperty(ref reader)) return null; }
        }

        reader.Read();
        return new(GetEventTimestamp(eventTimestamp, timestamp), eventKind, message,
                   progressPercent, timeMs, target, targetComponent, targetId,
                   restartRequired, configRevision);
    }

    private WifiEvent? ReadWifiEvent(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        WifiEventKind eventKind = WifiEventKind.Unknown;
        int? reason = null;
        string? staIp = null;
        string? ssid = null;
        int? channel = null;
        int? rssi = null;
        long eventTimestamp = 0;
        ushort? id = null;
        var componentType = ComponentType.WiFi;

        if (!reader.Read()) return null;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { if (!reader.Read()) return null; continue; }

            readProperty |= ReadComponentProperty(ref reader, ref componentType, ref id);

            if (reader.ValueSpan.SequenceEqual("event"u8))
            {
                reader.Read();
                eventKind = GetWifiEventKind(reader.ValueSpan);
                reader.Read();
                readProperty = true;
            }

            readProperty |= ReadNullableInt32Property(ref reader, "reason"u8, ref reason);
            readProperty |= ReadNullableStringProperty(ref reader, "sta_ip"u8, ref staIp);
            readProperty |= ReadNullableStringProperty(ref reader, "ssid"u8, ref ssid);
            readProperty |= ReadNullableInt32Property(ref reader, "channel"u8, ref channel);
            readProperty |= ReadNullableInt32Property(ref reader, "rssi"u8, ref rssi);
            readProperty |= ReadTimestampProperty(ref reader, "ts"u8, ref eventTimestamp);

            if (!readProperty) { if (!SkipProperty(ref reader)) return null; }
        }

        reader.Read();
        return new(GetEventTimestamp(eventTimestamp, timestamp), eventKind, reason, staIp, ssid, channel, rssi);
    }

    private BTHomeEvent? ReadBTHomeEvent(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        BTHomeEventKind eventKind = BTHomeEventKind.Unknown;
        BTHomeDiscoveredDevice? device = null;
        int? deviceCount = null;
        long eventTimestamp = 0;
        ushort? id = null;
        var componentType = ComponentType.BTHome;

        if (!reader.Read()) return null;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { if (!reader.Read()) return null; continue; }

            readProperty |= ReadComponentProperty(ref reader, ref componentType, ref id);

            if (reader.ValueSpan.SequenceEqual("event"u8))
            {
                reader.Read();
                eventKind = GetBTHomeEventKind(reader.ValueSpan);
                reader.Read();
                readProperty = true;
            }

            if (reader.ValueSpan.SequenceEqual("device"u8))
            {
                device = ReadBTHomeDiscoveredDevice(ref reader);
                readProperty = true;
            }

            readProperty |= ReadNullableInt32Property(ref reader, "device_count"u8, ref deviceCount);
            readProperty |= ReadTimestampProperty(ref reader, "ts"u8, ref eventTimestamp);

            if (!readProperty) { if (!SkipProperty(ref reader)) return null; }
        }

        reader.Read();
        return new(GetEventTimestamp(eventTimestamp, timestamp), eventKind, device, deviceCount);
    }

    private BTHomeDeviceEvent? ReadBTHomeDeviceEvent(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        ushort? id = null;
        BTHomeDeviceEventKind eventKind = BTHomeDeviceEventKind.Unknown;
        int? objectIndex = null;
        int? channel = null;
        string? sensorsJson = null;
        long eventTimestamp = 0;
        var componentType = ComponentType.BTHomeDevice;

        if (!reader.Read()) return null;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { if (!reader.Read()) return null; continue; }

            readProperty |= ReadComponentProperty(ref reader, ref componentType, ref id);
            readProperty |= ReadNullableComponentIdProperty(ref reader, "id"u8, ref id);

            if (reader.ValueSpan.SequenceEqual("event"u8))
            {
                reader.Read();
                eventKind = GetBTHomeDeviceEventKind(reader.ValueSpan);
                reader.Read();
                readProperty = true;
            }

            readProperty |= ReadNullableInt32Property(ref reader, "idx"u8, ref objectIndex);
            readProperty |= ReadNullableInt32Property(ref reader, "channel"u8, ref channel);
            readProperty |= ReadJsonProperty(ref reader, "sensors"u8, ref sensorsJson);
            readProperty |= ReadTimestampProperty(ref reader, "ts"u8, ref eventTimestamp);

            if (!readProperty) { if (!SkipProperty(ref reader)) return null; }
        }

        reader.Read();
        if (!id.HasValue) return null;

        return new(GetEventTimestamp(eventTimestamp, timestamp), id.Value, eventKind, objectIndex, channel, sensorsJson);
    }

    private EmDataEvent? ReadEmDataEvent(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        ushort? id = null;
        EmDataEventKind eventKind = EmDataEventKind.Unknown;
        EnergyDataRecord[] data = [];
        long eventTimestamp = 0;
        var componentType = ComponentType.EMData;

        if (!reader.Read()) return null;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { if (!reader.Read()) return null; continue; }

            readProperty |= ReadComponentProperty(ref reader, ref componentType, ref id);
            readProperty |= ReadNullableComponentIdProperty(ref reader, "id"u8, ref id);

            if (reader.ValueSpan.SequenceEqual("event"u8))
            {
                reader.Read();
                eventKind = reader.ValueSpan.SequenceEqual("data"u8) ? EmDataEventKind.Data : EmDataEventKind.Unknown;
                reader.Read();
                readProperty = true;
            }

            if (reader.ValueSpan.SequenceEqual("data"u8))
            {
                data = ReadEnergyData(ref reader);
                readProperty = true;
            }

            readProperty |= ReadTimestampProperty(ref reader, "ts"u8, ref eventTimestamp);

            if (!readProperty) { if (!SkipProperty(ref reader)) return null; }
        }

        reader.Read();
        if (!id.HasValue) return null;

        return new(GetEventTimestamp(eventTimestamp, timestamp), id.Value, eventKind, data);
    }

    private Em1DataEvent? ReadEm1DataEvent(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        ushort? id = null;
        Em1DataEventKind eventKind = Em1DataEventKind.Unknown;
        EnergyDataRecord[] data = [];
        long eventTimestamp = 0;
        var componentType = ComponentType.EM1Data;

        if (!reader.Read()) return null;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { if (!reader.Read()) return null; continue; }

            readProperty |= ReadComponentProperty(ref reader, ref componentType, ref id);
            readProperty |= ReadNullableComponentIdProperty(ref reader, "id"u8, ref id);

            if (reader.ValueSpan.SequenceEqual("event"u8))
            {
                reader.Read();
                eventKind = reader.ValueSpan.SequenceEqual("data"u8) ? Em1DataEventKind.Data : Em1DataEventKind.Unknown;
                reader.Read();
                readProperty = true;
            }

            if (reader.ValueSpan.SequenceEqual("data"u8))
            {
                data = ReadEnergyData(ref reader);
                readProperty = true;
            }

            readProperty |= ReadTimestampProperty(ref reader, "ts"u8, ref eventTimestamp);

            if (!readProperty) { if (!SkipProperty(ref reader)) return null; }
        }

        reader.Read();
        if (!id.HasValue) return null;

        return new(GetEventTimestamp(eventTimestamp, timestamp), id.Value, eventKind, data);
    }

    private ButtonEvent? ReadButtonEvent(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        ushort? id = null;
        ButtonEventKind eventKind = ButtonEventKind.Unknown;
        long eventTimestamp = 0;
        var componentType = ComponentType.Button;

        if (!reader.Read()) return null;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { if (!reader.Read()) return null; continue; }

            readProperty |= ReadComponentProperty(ref reader, ref componentType, ref id);
            readProperty |= ReadNullableComponentIdProperty(ref reader, "id"u8, ref id);

            if (reader.ValueSpan.SequenceEqual("event"u8))
            {
                reader.Read();
                eventKind = GetButtonEventKind(reader.ValueSpan);
                reader.Read();
                readProperty = true;
            }

            readProperty |= ReadTimestampProperty(ref reader, "ts"u8, ref eventTimestamp);

            if (!readProperty) { if (!SkipProperty(ref reader)) return null; }
        }

        reader.Read();
        if (!id.HasValue) return null;

        return new(GetEventTimestamp(eventTimestamp, timestamp), id.Value, eventKind);
    }

    private ScriptEvent? ReadScriptEvent(ref Utf8JsonReader reader, DateTimeOffset timestamp)
    {
        ushort? id = null;
        string? eventName = null;
        string? dataJson = null;
        long eventTimestamp = 0;
        var componentType = ComponentType.Script;

        if (!reader.Read()) return null;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { if (!reader.Read()) return null; continue; }

            readProperty |= ReadComponentProperty(ref reader, ref componentType, ref id);
            readProperty |= ReadNullableComponentIdProperty(ref reader, "id"u8, ref id);
            readProperty |= ReadNullableStringProperty(ref reader, "event"u8, ref eventName);
            readProperty |= ReadJsonProperty(ref reader, "data"u8, ref dataJson);
            readProperty |= ReadTimestampProperty(ref reader, "ts"u8, ref eventTimestamp);

            if (!readProperty) { if (!SkipProperty(ref reader)) return null; }
        }

        reader.Read();
        if (!id.HasValue || string.IsNullOrEmpty(eventName)) return null;

        return new(GetEventTimestamp(eventTimestamp, timestamp), id.Value,
                   ScriptEventKind.Custom, eventName, dataJson);
    }

    private static IShellyComponentEvent? SkipUnsupportedEvent(ref Utf8JsonReader reader)
    {
        reader.Skip();
        reader.Read();
        return null;
    }

    private static BlePairedDeviceInfo? ReadBlePairedDeviceInfo(ref Utf8JsonReader reader)
    {
        if (!reader.Read()) return null;
        if (reader.TokenType == JsonTokenType.Null) { reader.Read(); return null; }
        if (reader.TokenType != JsonTokenType.StartObject) return null;
        if (!reader.Read()) return null;

        string? address = null;
        long? creationTimestamp = null;
        long? accessTimestamp = null;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { if (!reader.Read()) return null; continue; }

            readProperty |= ReadNullableStringProperty(ref reader, "addr"u8, ref address);
            readProperty |= ReadNullableInt64Property(ref reader, "ctime"u8, ref creationTimestamp);
            readProperty |= ReadNullableInt64Property(ref reader, "atime"u8, ref accessTimestamp);

            if (!readProperty) { if (!SkipProperty(ref reader)) return null; }
        }

        reader.Read();
        return new(address, creationTimestamp, accessTimestamp);
    }

    private static PresenceTrackedObject[] ReadPresenceObjects(ref Utf8JsonReader reader)
    {
        if (!reader.Read()) return [];
        if (reader.TokenType == JsonTokenType.Null) { reader.Read(); return []; }
        if (reader.TokenType != JsonTokenType.StartArray) return [];

        List<PresenceTrackedObject> objects = [];

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray) break;
            if (reader.TokenType != JsonTokenType.StartObject) { reader.Skip(); continue; }

            int id = 0;
            double x = 0;
            double y = 0;
            double z = 0;
            double minZ = 0;
            double maxZ = 0;

            if (!reader.Read()) return objects.ToArray();

            while (true)
            {
                bool readProperty = false;

                if (reader.TokenType == JsonTokenType.EndObject) break;
                if (reader.TokenType != JsonTokenType.PropertyName) { if (!reader.Read()) return objects.ToArray(); continue; }

                readProperty |= ReadInt32Property(ref reader, "id"u8, ref id);
                readProperty |= ReadDoubleProperty(ref reader, "x"u8, ref x);
                readProperty |= ReadDoubleProperty(ref reader, "y"u8, ref y);
                readProperty |= ReadDoubleProperty(ref reader, "z"u8, ref z);
                readProperty |= ReadDoubleProperty(ref reader, "minz"u8, ref minZ);
                readProperty |= ReadDoubleProperty(ref reader, "maxz"u8, ref maxZ);

                if (!readProperty) { if (!SkipProperty(ref reader)) return objects.ToArray(); }
            }

            objects.Add(new(id, x, y, z, minZ, maxZ));
        }

        reader.Read();
        return objects.ToArray();
    }

    private static PresenceZoneObject[] ReadPresenceZoneObjects(ref Utf8JsonReader reader)
    {
        if (!reader.Read()) return [];
        if (reader.TokenType == JsonTokenType.Null) { reader.Read(); return []; }
        if (reader.TokenType != JsonTokenType.StartArray) return [];

        List<PresenceZoneObject> objects = [];

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray) break;
            if (reader.TokenType != JsonTokenType.StartArray) { reader.Skip(); continue; }

            double x = 0;
            double y = 0;

            if (reader.Read() && reader.TokenType == JsonTokenType.Number) x = reader.GetDouble();
            if (reader.Read() && reader.TokenType == JsonTokenType.Number) y = reader.GetDouble();

            while (reader.TokenType != JsonTokenType.EndArray && reader.Read()) { }
            objects.Add(new(x, y));
        }

        reader.Read();
        return objects.ToArray();
    }

    private static BTHomeDiscoveredDevice? ReadBTHomeDiscoveredDevice(ref Utf8JsonReader reader)
    {
        if (!reader.Read()) return null;
        if (reader.TokenType == JsonTokenType.Null) { reader.Read(); return null; }
        if (reader.TokenType != JsonTokenType.StartObject) return null;
        if (!reader.Read()) return null;

        string? address = null;
        string? localName = null;
        int? rssi = null;
        bool? encrypted = null;
        BTHomeShellyManufacturerData? shellyManufacturerData = null;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { if (!reader.Read()) return null; continue; }

            readProperty |= ReadNullableStringProperty(ref reader, "addr"u8, ref address);
            readProperty |= ReadNullableStringProperty(ref reader, "local_name"u8, ref localName);
            readProperty |= ReadNullableInt32Property(ref reader, "rssi"u8, ref rssi);
            readProperty |= ReadNullableBoolProperty(ref reader, "encrypted"u8, ref encrypted);

            if (reader.ValueSpan.SequenceEqual("shelly_mfdata"u8))
            {
                shellyManufacturerData = ReadBTHomeShellyManufacturerData(ref reader);
                readProperty = true;
            }

            if (!readProperty) { if (!SkipProperty(ref reader)) return null; }
        }

        reader.Read();
        return new(address, localName, rssi, encrypted, shellyManufacturerData);
    }

    private static BTHomeShellyManufacturerData? ReadBTHomeShellyManufacturerData(ref Utf8JsonReader reader)
    {
        if (!reader.Read()) return null;
        if (reader.TokenType == JsonTokenType.Null) { reader.Read(); return null; }
        if (reader.TokenType != JsonTokenType.StartObject) return null;
        if (!reader.Read()) return null;

        int? flags = null;
        int? modelId = null;
        string? mac = null;

        while (true)
        {
            bool readProperty = false;

            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) { if (!reader.Read()) return null; continue; }

            readProperty |= ReadNullableInt32Property(ref reader, "flags"u8, ref flags);
            readProperty |= ReadNullableInt32Property(ref reader, "model_id"u8, ref modelId);
            readProperty |= ReadNullableStringProperty(ref reader, "mac"u8, ref mac);

            if (!readProperty) { if (!SkipProperty(ref reader)) return null; }
        }

        reader.Read();
        return new(flags, modelId, mac);
    }

    private static EnergyDataRecord[] ReadEnergyData(ref Utf8JsonReader reader)
    {
        if (!reader.Read()) return [];
        if (reader.TokenType == JsonTokenType.Null) { reader.Read(); return []; }
        if (reader.TokenType != JsonTokenType.StartArray) return [];

        List<EnergyDataRecord> data = [];

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray) break;
            if (reader.TokenType != JsonTokenType.StartObject) { reader.Skip(); continue; }

            long recordTimestamp = 0;
            double? period = null;
            double[][] values = [];

            if (!reader.Read()) return data.ToArray();

            while (true)
            {
                bool readProperty = false;

                if (reader.TokenType == JsonTokenType.EndObject) break;
                if (reader.TokenType != JsonTokenType.PropertyName) { if (!reader.Read()) return data.ToArray(); continue; }

                readProperty |= ReadInt64Property(ref reader, "ts"u8, ref recordTimestamp);
                readProperty |= ReadNullableDoubleProperty(ref reader, "period"u8, ref period);

                if (reader.ValueSpan.SequenceEqual("values"u8))
                {
                    values = ReadDoubleMatrix(ref reader);
                    readProperty = true;
                }

                if (!readProperty) { if (!SkipProperty(ref reader)) return data.ToArray(); }
            }

            data.Add(new(recordTimestamp, period, values));
        }

        reader.Read();
        return data.ToArray();
    }

    private static double[][] ReadDoubleMatrix(ref Utf8JsonReader reader)
    {
        if (!reader.Read()) return [];
        if (reader.TokenType == JsonTokenType.Null) { reader.Read(); return []; }
        if (reader.TokenType != JsonTokenType.StartArray) return [];

        List<double[]> rows = [];

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray) break;
            if (reader.TokenType != JsonTokenType.StartArray) { reader.Skip(); continue; }

            List<double> row = [];

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray) break;
                if (reader.TokenType == JsonTokenType.Number) row.Add(reader.GetDouble());
            }

            rows.Add(row.ToArray());
        }

        reader.Read();
        return rows.ToArray();
    }

    private static DateTimeOffset FindAndParseTimestamp(ref Utf8JsonReader reader)
    {
        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.PropertyName ||
                !reader.ValueSpan.SequenceEqual("ts"u8)) continue;

            if (!reader.Read()) return default;
            if (reader.TokenType != JsonTokenType.Number) continue;

            return DateTimeOffset.FromUnixTimeMilliseconds(ParseUnixTimestamp(reader.ValueSpan));
        }

        return default;
    }

    private static bool OpenEventsArray(ref Utf8JsonReader reader, out DateTimeOffset timestamp)
    {
        timestamp = default;

        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.PropertyName) continue;

            if (reader.ValueSpan.SequenceEqual("ts"u8) && timestamp == default)
            {
                if (!reader.Read()) return false;
                timestamp = DateTimeOffset.FromUnixTimeMilliseconds(ParseUnixTimestamp(reader.ValueSpan));
                continue;
            }

            if (reader.ValueSpan.SequenceEqual("events"u8))
            {
                if (!reader.Read()) return false;
                return reader.TokenType == JsonTokenType.StartArray;
            }
        }

        return false;
    }

    /// <summary>Converts component name to event source enum.</summary>
    private static ComponentType GetComponentType(ReadOnlySpan<byte> span)
    {
        // Exact singleton components first.
        if (span.SequenceEqual("ble"u8)) return ComponentType.BluetoothLowEnergy;
        if (span.SequenceEqual("dali"u8)) return ComponentType.DALI;
        if (span.SequenceEqual("presence"u8)) return ComponentType.Presence;
        if (span.SequenceEqual("sys"u8)) return ComponentType.System;
        if (span.SequenceEqual("wifi"u8)) return ComponentType.WiFi;
        if (span.SequenceEqual("bthome"u8)) return ComponentType.BTHome;

        // Prefixes are ordered where one type is a prefix of another.
        if (span.StartsWith("bthomedevice:"u8)) return ComponentType.BTHomeDevice;
        if (span.StartsWith("bthomesensor:"u8)) return ComponentType.BTHomeSensor;
        if (span.StartsWith("bthomecontrol:"u8)) return ComponentType.BTHomeControl;
        if (span.StartsWith("presencezone:"u8)) return ComponentType.PresenceZone;
        if (span.StartsWith("em1data:"u8)) return ComponentType.EM1Data;
        if (span.StartsWith("emdata:"u8)) return ComponentType.EMData;
        if (span.StartsWith("em1:"u8)) return ComponentType.EM1;
        if (span.StartsWith("em:"u8)) return ComponentType.EM;

        if (span.StartsWith("camera:"u8)) return ComponentType.Camera;
        if (span.StartsWith("input:"u8)) return ComponentType.Input;
        if (span.StartsWith("button:"u8)) return ComponentType.Button;
        if (span.StartsWith("script:"u8)) return ComponentType.Script;

        if (span.StartsWith("switch:"u8)) return ComponentType.Switch;
        if (span.StartsWith("camerazone:"u8)) return ComponentType.CameraZone;
        if (span.StartsWith("cb:"u8)) return ComponentType.CB;
        if (span.StartsWith("cct:"u8)) return ComponentType.CCT;
        if (span.SequenceEqual("cloud"u8)) return ComponentType.Cloud;
        if (span.StartsWith("cover:"u8)) return ComponentType.Cover;
        if (span.StartsWith("devicepower:"u8)) return ComponentType.DevicePower;
        if (span.SequenceEqual("eth"u8)) return ComponentType.Ethernet;
        if (span.StartsWith("flood:"u8)) return ComponentType.Flood;
        if (span.StartsWith("humidity:"u8)) return ComponentType.Humidity;
        if (span.StartsWith("illuminance:"u8)) return ComponentType.Illuminance;
        if (span.StartsWith("light:"u8)) return ComponentType.Light;
        if (span.SequenceEqual("matter"u8)) return ComponentType.Matter;
        if (span.SequenceEqual("modbus"u8)) return ComponentType.Modbus;
        if (span.StartsWith("mbrtuclient:"u8)) return ComponentType.ModbusRTUClient;
        if (span.SequenceEqual("mqtt"u8)) return ComponentType.MQTT;
        if (span.StartsWith("pill:"u8)) return ComponentType.Pill;
        if (span.StartsWith("pm1:"u8)) return ComponentType.PM1;
        if (span.StartsWith("rgb:"u8)) return ComponentType.RGB;
        if (span.StartsWith("rgbcct:"u8)) return ComponentType.RGBCCT;
        if (span.StartsWith("rgbw:"u8)) return ComponentType.RGBW;
        if (span.StartsWith("serial:"u8)) return ComponentType.Serial;
        if (span.StartsWith("smoke:"u8)) return ComponentType.Smoke;
        if (span.StartsWith("storage:"u8)) return ComponentType.Storage;
        if (span.StartsWith("temperature:"u8)) return ComponentType.Temperature;
        if (span.StartsWith("voltmeter:"u8)) return ComponentType.Voltmeter;
        if (span.SequenceEqual("zigbee"u8)) return ComponentType.Zigbee;
        if (span.SequenceEqual("ws"u8)) return ComponentType.OutboundWebsocket;

        if (span.StartsWith("boolean:"u8)) return ComponentType.VirtualBoolean;
        if (span.StartsWith("number:"u8)) return ComponentType.VirtualNumber;
        if (span.StartsWith("text:"u8)) return ComponentType.VirtualText;
        if (span.StartsWith("enum:"u8)) return ComponentType.VirtualEnum;
        if (span.StartsWith("group:"u8)) return ComponentType.VirtualGroup;
        if (span.StartsWith("lnm:"u8)) return ComponentType.LNM;

        return ComponentType.Unknown;
    }

    private static CameraEventKind GetCameraEventKind(ReadOnlySpan<byte> eventValue)
    {
        if (eventValue.SequenceEqual("media_ready"u8)) return CameraEventKind.MediaReady;
        if (eventValue.SequenceEqual("upload_complete"u8)) return CameraEventKind.UploadComplete;
        if (eventValue.SequenceEqual("upload_failed"u8)) return CameraEventKind.UploadFailed;
        return CameraEventKind.Unknown;
    }

    private static BleEventKind GetBleEventKind(ReadOnlySpan<byte> eventValue)
    {
        if (eventValue.SequenceEqual("paired_device_added"u8)) return BleEventKind.PairedDeviceAdded;
        if (eventValue.SequenceEqual("paired_device_removed"u8)) return BleEventKind.PairedDeviceRemoved;
        return BleEventKind.Unknown;
    }

    private static DaliEventKind GetDaliEventKind(ReadOnlySpan<byte> eventValue)
    {
        if (eventValue.SequenceEqual("scan_complete"u8)) return DaliEventKind.ScanComplete;
        if (eventValue.SequenceEqual("ping_complete"u8)) return DaliEventKind.PingComplete;
        return DaliEventKind.Unknown;
    }

    private static InputEventKind GetInputEventKind(ReadOnlySpan<byte> eventValue)
    {
        if (eventValue.SequenceEqual("btn_down"u8)) return InputEventKind.BtnDown;
        if (eventValue.SequenceEqual("btn_up"u8)) return InputEventKind.BtnUp;
        if (eventValue.SequenceEqual("single_push"u8)) return InputEventKind.SinglePush;
        if (eventValue.SequenceEqual("double_push"u8)) return InputEventKind.DoublePush;
        if (eventValue.SequenceEqual("triple_push"u8)) return InputEventKind.TriplePush;
        if (eventValue.SequenceEqual("long_push"u8)) return InputEventKind.LongPush;
        return InputEventKind.Unknown;
    }

    private static PresenceEventKind GetPresenceEventKind(ReadOnlySpan<byte> eventValue)
    {
        if (eventValue.SequenceEqual("track"u8)) return PresenceEventKind.Track;
        if (eventValue.SequenceEqual("no_track"u8)) return PresenceEventKind.NoTrack;
        return PresenceEventKind.Unknown;
    }

    private static PresenceZoneEventKind GetPresenceZoneEventKind(ReadOnlySpan<byte> eventValue)
    {
        if (eventValue.SequenceEqual("presence"u8)) return PresenceZoneEventKind.Presence;
        if (eventValue.SequenceEqual("counter"u8)) return PresenceZoneEventKind.Counter;
        if (eventValue.SequenceEqual("enter"u8)) return PresenceZoneEventKind.Enter;
        if (eventValue.SequenceEqual("leave"u8)) return PresenceZoneEventKind.Leave;
        return PresenceZoneEventKind.Unknown;
    }

    private static SystemEventKind GetSystemEventKind(ReadOnlySpan<byte> eventValue)
    {
        if (eventValue.SequenceEqual("ota_begin"u8)) return SystemEventKind.OtaBegin;
        if (eventValue.SequenceEqual("ota_progress"u8)) return SystemEventKind.OtaProgress;
        if (eventValue.SequenceEqual("ota_success"u8)) return SystemEventKind.OtaSuccess;
        if (eventValue.SequenceEqual("ota_error"u8)) return SystemEventKind.OtaError;
        if (eventValue.SequenceEqual("sleep"u8)) return SystemEventKind.Sleep;
        if (eventValue.SequenceEqual("scheduled_restart"u8)) return SystemEventKind.ScheduledRestart;
        if (eventValue.SequenceEqual("component_added"u8)) return SystemEventKind.ComponentAdded;
        if (eventValue.SequenceEqual("component_removed"u8)) return SystemEventKind.ComponentRemoved;
        if (eventValue.SequenceEqual("sys_btn_down"u8)) return SystemEventKind.SysBtnDown;
        if (eventValue.SequenceEqual("sys_btn_up"u8)) return SystemEventKind.SysBtnUp;
        if (eventValue.SequenceEqual("sys_btn_push"u8)) return SystemEventKind.SysBtnPush;
        if (eventValue.SequenceEqual("provision_pending"u8)) return SystemEventKind.ProvisionPending;
        if (eventValue.SequenceEqual("provision_confirmed"u8)) return SystemEventKind.ProvisionConfirmed;
        if (eventValue.SequenceEqual("provision_complete"u8)) return SystemEventKind.ProvisionComplete;
        if (eventValue.SequenceEqual("provision_locked"u8)) return SystemEventKind.ProvisionLocked;
        return SystemEventKind.Unknown;
    }

    private static WifiEventKind GetWifiEventKind(ReadOnlySpan<byte> eventValue)
    {
        if (eventValue.SequenceEqual("sta_connect_fail"u8)) return WifiEventKind.StaConnectFail;
        if (eventValue.SequenceEqual("sta_disconnected"u8)) return WifiEventKind.StaDisconnected;
        if (eventValue.SequenceEqual("sta_connecting"u8)) return WifiEventKind.StaConnecting;
        if (eventValue.SequenceEqual("sta_connected"u8)) return WifiEventKind.StaConnected;
        if (eventValue.SequenceEqual("sta_ip_acquired"u8)) return WifiEventKind.StaIpAcquired;
        return WifiEventKind.Unknown;
    }

    private static BTHomeEventKind GetBTHomeEventKind(ReadOnlySpan<byte> eventValue)
    {
        if (eventValue.SequenceEqual("device_discovered"u8)) return BTHomeEventKind.DeviceDiscovered;
        if (eventValue.SequenceEqual("discovery_done"u8)) return BTHomeEventKind.DiscoveryDone;
        return BTHomeEventKind.Unknown;
    }

    private static BTHomeDeviceEventKind GetBTHomeDeviceEventKind(ReadOnlySpan<byte> eventValue)
    {
        if (eventValue.SequenceEqual("new_objects"u8)) return BTHomeDeviceEventKind.NewObjects;
        if (eventValue.SequenceEqual("single_push"u8)) return BTHomeDeviceEventKind.SinglePush;
        if (eventValue.SequenceEqual("double_push"u8)) return BTHomeDeviceEventKind.DoublePush;
        if (eventValue.SequenceEqual("triple_push"u8)) return BTHomeDeviceEventKind.TriplePush;
        if (eventValue.SequenceEqual("long_push"u8)) return BTHomeDeviceEventKind.LongPush;
        if (eventValue.SequenceEqual("long_double_push"u8)) return BTHomeDeviceEventKind.LongDoublePush;
        if (eventValue.SequenceEqual("long_triple_push"u8)) return BTHomeDeviceEventKind.LongTriplePush;
        if (eventValue.SequenceEqual("rotate_left"u8)) return BTHomeDeviceEventKind.RotateLeft;
        if (eventValue.SequenceEqual("rotate_right"u8)) return BTHomeDeviceEventKind.RotateRight;
        return BTHomeDeviceEventKind.Unknown;
    }

    private static ButtonEventKind GetButtonEventKind(ReadOnlySpan<byte> eventValue)
    {
        if (eventValue.SequenceEqual("single_push"u8)) return ButtonEventKind.SinglePush;
        if (eventValue.SequenceEqual("double_push"u8)) return ButtonEventKind.DoublePush;
        if (eventValue.SequenceEqual("triple_push"u8)) return ButtonEventKind.TriplePush;
        if (eventValue.SequenceEqual("long_push"u8)) return ButtonEventKind.LongPush;
        return ButtonEventKind.Unknown;
    }

    private static bool ReadComponentProperty(ref Utf8JsonReader reader, ref ComponentType componentType, ref ushort? id)
    {
        if (!reader.ValueSpan.SequenceEqual("component"u8)) return false;

        reader.Read();
        componentType = GetComponentType(reader.ValueSpan);
        id ??= GetComponentId(reader.ValueSpan);
        reader.Read();
        return true;
    }

    private static ushort? GetComponentId(ReadOnlySpan<byte> span)
    {
        int colon = span.IndexOf((byte)':');
        if (colon < 0 || colon == span.Length - 1) return null;

        return ushort.TryParse(span[(colon + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            ? id
            : null;
    }

    private static bool ReadNullableComponentIdProperty(ref Utf8JsonReader reader, ReadOnlySpan<byte> propertyName, ref ushort? value)
    {
        if (!reader.ValueSpan.SequenceEqual(propertyName)) return false;

        reader.Read();

        if (reader.TokenType == JsonTokenType.Null)
        {
            value = null;
            reader.Read();
            return true;
        }

        value = checked((ushort)reader.GetInt32());
        reader.Read();
        return true;
    }

    private static bool ReadInt32Property(ref Utf8JsonReader reader, ReadOnlySpan<byte> propertyName, ref int value)
    {
        if (!reader.ValueSpan.SequenceEqual(propertyName)) return false;

        reader.Read();
        value = reader.GetInt32();
        reader.Read();
        return true;
    }

    private static bool ReadNullableInt32Property(ref Utf8JsonReader reader, ReadOnlySpan<byte> propertyName, ref int? value)
    {
        if (!reader.ValueSpan.SequenceEqual(propertyName)) return false;

        reader.Read();

        if (reader.TokenType == JsonTokenType.Null)
        {
            value = null;
            reader.Read();
            return true;
        }

        value = reader.GetInt32();
        reader.Read();
        return true;
    }

    private static bool ReadInt64Property(ref Utf8JsonReader reader, ReadOnlySpan<byte> propertyName, ref long value)
    {
        if (!reader.ValueSpan.SequenceEqual(propertyName)) return false;

        reader.Read();
        value = reader.GetInt64();
        reader.Read();
        return true;
    }

    private static bool ReadNullableInt64Property(ref Utf8JsonReader reader, ReadOnlySpan<byte> propertyName, ref long? value)
    {
        if (!reader.ValueSpan.SequenceEqual(propertyName)) return false;

        reader.Read();

        if (reader.TokenType == JsonTokenType.Null)
        {
            value = null;
            reader.Read();
            return true;
        }

        value = reader.GetInt64();
        reader.Read();
        return true;
    }

    private static bool ReadDoubleProperty(ref Utf8JsonReader reader, ReadOnlySpan<byte> propertyName, ref double value)
    {
        if (!reader.ValueSpan.SequenceEqual(propertyName)) return false;

        reader.Read();
        value = reader.GetDouble();
        reader.Read();
        return true;
    }

    private static bool ReadNullableDoubleProperty(ref Utf8JsonReader reader, ReadOnlySpan<byte> propertyName, ref double? value)
    {
        if (!reader.ValueSpan.SequenceEqual(propertyName)) return false;

        reader.Read();

        if (reader.TokenType == JsonTokenType.Null)
        {
            value = null;
            reader.Read();
            return true;
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            value = reader.GetDouble();
            reader.Read();
            return true;
        }

        if (reader.TokenType == JsonTokenType.String &&
            double.TryParse(reader.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            value = parsed;
        }

        reader.Read();
        return true;
    }

    private static bool ReadBoolProperty(ref Utf8JsonReader reader, ReadOnlySpan<byte> propertyName, ref bool value)
    {
        if (!reader.ValueSpan.SequenceEqual(propertyName)) return false;

        reader.Read();
        value = reader.GetBoolean();
        reader.Read();
        return true;
    }

    private static bool ReadNullableBoolProperty(ref Utf8JsonReader reader, ReadOnlySpan<byte> propertyName, ref bool? value)
    {
        if (!reader.ValueSpan.SequenceEqual(propertyName)) return false;

        reader.Read();

        if (reader.TokenType == JsonTokenType.Null)
        {
            value = null;
            reader.Read();
            return true;
        }

        value = reader.GetBoolean();
        reader.Read();
        return true;
    }

    private static bool ReadNullableStringProperty(ref Utf8JsonReader reader, ReadOnlySpan<byte> propertyName, ref string? value)
    {
        if (!reader.ValueSpan.SequenceEqual(propertyName)) return false;

        reader.Read();
        value = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
        reader.Read();
        return true;
    }

    private static bool ReadJsonProperty(ref Utf8JsonReader reader, ReadOnlySpan<byte> propertyName, ref string? value)
    {
        if (!reader.ValueSpan.SequenceEqual(propertyName)) return false;

        reader.Read();

        if (reader.TokenType == JsonTokenType.Null)
        {
            value = null;
            reader.Read();
            return true;
        }

        using var document = JsonDocument.ParseValue(ref reader);
        value = document.RootElement.GetRawText();
        reader.Read();
        return true;
    }

    private static bool ReadTimestampProperty(ref Utf8JsonReader reader, ReadOnlySpan<byte> propertyName, ref long value)
    {
        if (!reader.ValueSpan.SequenceEqual(propertyName)) return false;

        reader.Read();
        value = ParseUnixTimestamp(reader.ValueSpan);
        reader.Read();
        return true;
    }

    private static bool SkipProperty(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.PropertyName) return reader.Read();

        if (!reader.Read()) return false;

        if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
        {
            reader.Skip();
        }

        return reader.Read();
    }

    private static DateTimeOffset GetEventTimestamp(long eventTimestamp, DateTimeOffset fallbackTimestamp)
    {
        if (eventTimestamp != 0) return DateTimeOffset.FromUnixTimeMilliseconds(eventTimestamp);
        return fallbackTimestamp;
    }

    /// <summary>Converts a Shelly Unix timestamp to millisecond precision without temporary strings.</summary>
    private static long ParseUnixTimestamp(ReadOnlySpan<byte> value)
    {
        int dot = value.IndexOf((byte)'.');

        if (dot < 0)
        {
            long secondsOnly = long.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture);
            return secondsOnly * 1000;
        }

        long seconds = long.Parse(value[..dot], NumberStyles.None, CultureInfo.InvariantCulture);
        int ms = 0;
        var fraction = value[(dot + 1)..];

        if (fraction.Length > 0) ms += (fraction[0] - (byte)'0') * 100;
        if (fraction.Length > 1) ms += (fraction[1] - (byte)'0') * 10;
        if (fraction.Length > 2) ms += fraction[2] - (byte)'0';

        return seconds * 1000 + ms;
    }

    /// <summary>Writes an event to the pooled array, growing it when needed.</summary>
    private void AddEvent(IShellyComponentEvent componentEvent)
    {
        if (_eventsCount < _events.Length)
        {
            _events[_eventsCount++] = componentEvent;
            return;
        }

        var old = _events;
        var next = _eventsPool.Rent(old.Length * 2);

        old.AsSpan(0, _eventsCount).CopyTo(next);

        _eventsPool.Return(old, clearArray: true);
        _events = next;
        _events[_eventsCount++] = componentEvent;
    }
}
