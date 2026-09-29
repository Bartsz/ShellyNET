using ShellyNET.Client.Model;
using System.Buffers;

namespace ShellyNET.Client.Parsers;

public readonly struct ShellyNotifyEventParseResult : IDisposable
{
    private readonly ArrayPool<ShellyComponentEvent>? _eventsPool;

    private readonly ShellyComponentEvent[] _events { get; }

    public ShellyNotifyEventParseResult(ShellyComponentEvent[] events, ArrayPool<ShellyComponentEvent> eventsPool, ushort eventsCount)
    {
        _events = events;
        _eventsPool = eventsPool;
        EventsCount = eventsCount;
    }

    public ushort EventsCount { get; }
    public ReadOnlySpan<ShellyComponentEvent> EventsSpan => _events.AsSpan(0, EventsCount);
    public void Dispose()
    {
        _eventsPool?.Return(_events, clearArray: true);
    }
}

public class ShellyNotifyEventParser
{
    private readonly ArrayPool<ShellyComponentEvent> _eventsPool = ArrayPool<ShellyComponentEvent>.Shared;
    private ShellyComponentEvent[] _events = [];
    private ushort _eventsCount = 0;

    public ShellyNotifyEventParseResult ProcessNotifyEventMessage(Memory<byte> message, CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return new();

        var reader = new Utf8JsonReader(message.Span);
        DateTimeOffset timestamp = default;

        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return new();

        _eventsCount = 0;
        _events = _eventsPool.Rent(4);

        // go to events array
        OpenEventsArray(ref reader);
        ParseEvents(ref reader);

        return new(_events, _eventsPool, _eventsCount);
    }

    private void ParseEvents(ref Utf8JsonReader reader)
    {
        if (reader.ValueSpan.SequenceEqual("events"u8))
        {
            reader.Read(); // opened array "["
            if (reader.TokenType != JsonTokenType.StartArray) return;
            // Iterate events
            // during first read, moves to "{"
            // during following reads, moves from "}" to "{"
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray) return;

                ComponentType componentType = ComponentType.Unkown;
                byte? id = null;
                ComponentEvent eventType = ComponentEvent.Unkown;
                ComponentType targetComponent = ComponentType.Unkown;
                bool restartRequired = false;
                long eventTimestamp = 0;

                // read event data 
                // should always move to property  name
                while (true)
                {
                    bool readProperty = false;
                    if (reader.TokenType == JsonTokenType.EndArray) return;
                    if (reader.TokenType == JsonTokenType.EndObject) break;
                    if (reader.TokenType != JsonTokenType.PropertyName) { reader.Read(); continue; }

                    if (reader.ValueSpan.SequenceEqual("component"u8))
                    {
                        reader.Read();
                        componentType = GetComponentType(reader.ValueSpan);
                        reader.Read();
                        readProperty = true;
                    }

                    if (reader.ValueSpan.SequenceEqual("id"u8))
                    {
                        reader.Read();
                        id = byte.Parse(reader.ValueSpan);
                        reader.Read();
                        readProperty = true;
                    }

                    if (reader.ValueSpan.SequenceEqual("event"u8))
                    {
                        reader.Read();
                        eventType = GetComponentEvent(reader.ValueSpan);
                        reader.Read();
                        readProperty = true;
                    }

                    switch (componentType)
                    {
                       case ComponentType.Button:
                            break;
                        case ComponentType.Input:
                            break;
               
                    }

                    if (reader.ValueSpan.SequenceEqual("target"u8))
                    {
                        reader.Read();
                        targetComponent = GetComponentType(reader.ValueSpan);
                        reader.Read();
                        readProperty = true;
                    }

                    if (reader.ValueSpan.SequenceEqual("restart_required"u8))
                    {
                        reader.Read();
                        restartRequired = reader.GetBoolean();
                        reader.Read();
                        readProperty = true;
                    }

                    if (reader.ValueSpan.SequenceEqual("ts"u8))
                    {
                        reader.Read();
                        eventTimestamp = ParseUnixTimestamp(reader.ValueSpan);
                        reader.Read(); // if this is last property as expected then this moves to "}"
                        readProperty = true;
                    }

                    if (eventTimestamp != 0 && eventType != ComponentEvent.Unkown &&
                       id.HasValue && componentType != ComponentType.Unkown)
                    {
                        var datetimestamp = DateTimeOffset.FromUnixTimeMilliseconds(eventTimestamp);
                        var eventMsg = new ShellyComponentEvent(datetimestamp, id.Value, componentType, eventType);
                        AddEvent(eventMsg);
                    }

                    if (!readProperty)
                    {
                        if (!reader.Read()) return;
                    }

                    if (reader.TokenType == JsonTokenType.EndObject) break;
                }
            }
        }
    }

    private static void OpenEventsArray(ref Utf8JsonReader reader)
    {
        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.PropertyName) reader.Read();
            if (reader.ValueSpan.SequenceEqual("events"u8)) break;
        }
    }

    /// <summary> Converts component name to enum </summary>
    private ComponentType GetComponentType(ReadOnlySpan<byte> span)
    {
        // for now it is not alphabetical order, since only usage seems to be for input and maybe switch
        if (span.StartsWith("input:"u8)) return ComponentType.Input;
        if (span.StartsWith("switch:"u8)) return ComponentType.Switch;
        if (span.StartsWith("ble"u8)) return ComponentType.BluetoothLowEnergy;
        if (span.StartsWith("camera:"u8)) return ComponentType.Camera;
        if (span.StartsWith("camerazone:"u8)) return ComponentType.CameraZone;
        if (span.StartsWith("cb:"u8)) return ComponentType.CB;
        if (span.StartsWith("cct:"u8)) return ComponentType.CCT;
        if (span.StartsWith("cloud"u8)) return ComponentType.Cloud;
        if (span.StartsWith("cover:"u8)) return ComponentType.Cover;
        if (span.StartsWith("dali:"u8)) return ComponentType.DALI;
        if (span.StartsWith("devicepower:"u8)) return ComponentType.DevicePower;
        if (span.StartsWith("em:"u8)) return ComponentType.EM;
        if (span.StartsWith("em1:"u8)) return ComponentType.EM1;
        if (span.StartsWith("em1data:"u8)) return ComponentType.EM1Data;
        if (span.StartsWith("emdata:"u8)) return ComponentType.EMData;
        if (span.StartsWith("eth"u8)) return ComponentType.Ethernet;
        if (span.StartsWith("flood:"u8)) return ComponentType.Flood;
        if (span.StartsWith("humidity:"u8)) return ComponentType.Humidity;
        if (span.StartsWith("illuminance:"u8)) return ComponentType.Illuminance;
        if (span.StartsWith("light:"u8)) return ComponentType.Light;
        if (span.StartsWith("matter"u8)) return ComponentType.Matter;
        if (span.StartsWith("modbus"u8)) return ComponentType.Modbus;
        if (span.StartsWith("mbrtuclient:"u8)) return ComponentType.ModbusRTUClient;
        if (span.StartsWith("mqtt"u8)) return ComponentType.MQTT;
        if (span.StartsWith("pill:"u8)) return ComponentType.Pill;
        if (span.StartsWith("pm1:"u8)) return ComponentType.PM1;
        if (span.StartsWith("presence:"u8)) return ComponentType.Presence;
        if (span.StartsWith("presencezone:"u8)) return ComponentType.PresenceZone;
        if (span.StartsWith("rgb:"u8)) return ComponentType.RGB;
        if (span.StartsWith("rgbcct:"u8)) return ComponentType.RGBCCT;
        if (span.StartsWith("rgbw:"u8)) return ComponentType.RGBW;
        if (span.StartsWith("script:"u8)) return ComponentType.Script;
        if (span.StartsWith("serial:"u8)) return ComponentType.Serial;
        if (span.StartsWith("smoke:"u8)) return ComponentType.Smoke;
        if (span.StartsWith("storage:"u8)) return ComponentType.Storage;
        if (span.StartsWith("sys"u8)) return ComponentType.System;
        if (span.StartsWith("temperature:"u8)) return ComponentType.Temperature;
        if (span.StartsWith("voltmeter:"u8)) return ComponentType.Voltmeter;
        if (span.StartsWith("wifi"u8)) return ComponentType.WiFi;
        if (span.StartsWith("zigbee"u8)) return ComponentType.Zigbee;
        if (span.StartsWith("ws"u8)) return ComponentType.OutboundWebsocket;

        return ComponentType.Unkown;
    }

    private ComponentEvent GetComponentEvent(ReadOnlySpan<byte> eventValue)
    {
        if (eventValue.SequenceEqual("btn_down"u8)) return ComponentEvent.BtnDown;
        if (eventValue.SequenceEqual("btn_up"u8)) return ComponentEvent.BtnUp;
        if (eventValue.SequenceEqual("single_push"u8)) return ComponentEvent.SinglePush;
        if (eventValue.SequenceEqual("double_push"u8)) return ComponentEvent.DoublePush;
        if (eventValue.SequenceEqual("long_push"u8)) return ComponentEvent.LongPush;
        if (eventValue.SequenceEqual("triple_push"u8)) return ComponentEvent.TriplePush;
        return ComponentEvent.Unkown;
    }


    /// <summary>Writes a status change immediately when channel capacity is available and delegates only backpressure to the asynchronous path.</summary>
    private void AddEvent(ShellyComponentEvent stateChange)
    {
        if (_eventsCount < _events.Length)
        {
            _events[_eventsCount++] = stateChange;
            return;
        }

        var old = _events;
        var next = _eventsPool.Rent(old.Length * 2);

        old.AsSpan(0, _eventsCount).CopyTo(next);

        _eventsPool.Return(old);
        _events = next;
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


}


/// <summary> Repesents numeric and bool status change of a single property of a component. </summary>
public readonly struct ShellyComponentEvent
{
    public ShellyComponentEvent(DateTimeOffset timestamp, byte id, ComponentType componentType, ComponentEvent componentEvent)
    {
        Timestamp = timestamp;
        Id = id;
        ComponentType = componentType;
        ComponentEvent = componentEvent;
    }

    public DateTimeOffset Timestamp { get; }
    public byte Id { get; }
    public ComponentType ComponentType { get; }
    public ComponentEvent ComponentEvent { get; }
}
