using ShellyNET.Client.Model;
using System.Threading.Channels;

namespace ShellyNET.Client;

public partial class ShellyClient
{
    public async ValueTask<ShellyComponentEvent> GetShellyEvent(CancellationToken ct)
    {
        while (true)
        {
            return await _shellyEventsChannel.Reader.ReadAsync(ct).ConfigureAwait(false);
        }
    }

    private readonly Channel<ShellyComponentEvent> _shellyEventsChannel = Channel.CreateBounded<ShellyComponentEvent>(new BoundedChannelOptions(128)
    {
        AllowSynchronousContinuations = false,
        SingleReader = true,
        SingleWriter = true,
        FullMode = BoundedChannelFullMode.Wait
    });

    private readonly ShellyComponentEvent[] _pendingEventUpdates = new ShellyComponentEvent[128];
    private byte _pendingEventsCount = 0;

    internal void ProcessNotifyEventMessage(Memory<byte> message, CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return;

        var reader = new Utf8JsonReader(message.Span);
        DateTimeOffset timestamp = default;

        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return;

        // go to events array
        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.PropertyName) reader.Read();
            if (reader.ValueSpan.SequenceEqual("events"u8)) break;
        }

        if (reader.ValueSpan.SequenceEqual("events"u8))
        {
            reader.Read(); // opened array "["

            // Iterate events
            // during first read, moves to "{"
            // during following reads, moves from "}" to "{"
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray) return;

                ComponentType componentType = ComponentType.Unkown;
                byte? id = null;
                ComponentEvent componentEvent = ComponentEvent.Unkown;
                long eventTimestamp = 0;

                // read event data 
                // should always move to property  name
                while (true)
                {
                    bool readProperty = false;
                    if (reader.TokenType == JsonTokenType.EndArray) return;
                    if (reader.TokenType == JsonTokenType.EndObject) break;
                    if (reader.TokenType != JsonTokenType.PropertyName)
                    {
                        reader.Read();
                        continue;
                    }

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
                        componentEvent = GetComponentEvent(reader.ValueSpan);
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

                    if (eventTimestamp != 0 && componentEvent != ComponentEvent.Unkown &&
                       id.HasValue && componentType != ComponentType.Unkown)
                    {
                        var datetimestamp = DateTimeOffset.FromUnixTimeMilliseconds(eventTimestamp);
                        var eventMsg = new ShellyComponentEvent(datetimestamp, id.Value, componentType, componentEvent);
                        var enqueued = EnqueueEventMessage(eventMsg);
                        if (!enqueued) return;
                        // consumer couldn't clear 128 messages buffer and 128 messages queue
                        // at this point we don't care much. Event though we are connected, 
                        // consumer is not responding
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
        if (eventValue.SequenceEqual("long_push"u8)) return ComponentEvent.LongPush;
        if (eventValue.SequenceEqual("single_push"u8)) return ComponentEvent.SinglePush;
        if (eventValue.SequenceEqual("double_push"u8)) return ComponentEvent.DoublePush;
        if (eventValue.SequenceEqual("triple_push"u8)) return ComponentEvent.TriplePush;
        if (eventValue.SequenceEqual("btn_down"u8)) return ComponentEvent.BtnDown;
        if (eventValue.SequenceEqual("btn_up"u8)) return ComponentEvent.BtnUp;
        return ComponentEvent.Unkown;
    }


    /// <summary>Writes a status change immediately when channel capacity is available and delegates only backpressure to the asynchronous path.</summary>
    private bool EnqueueEventMessage(ShellyComponentEvent stateChange)
    {
        if (_shellyEventsChannel.Writer.TryWrite(stateChange)) return true;

        if (_pendingEventsCount == 128) return false;
        //return SendShellyStateChangeMsgAsync(stateChange, CancellationToken.None);
        _pendingEventUpdates[_pendingEventsCount] = stateChange;
        _pendingEventsCount++;

        return true;
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
