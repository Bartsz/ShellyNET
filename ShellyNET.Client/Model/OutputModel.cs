using ShellyNET.Client.Model.Enums;

namespace ShellyNET.Client.Model;

public interface IShellyStatusMessage
{
    byte Id { get; }
    ComponentType ComponentType { get; }
    PropertyName PropertyName { get; }
    DateTimeOffset Timestamp { get; }
}

/// <summary> Repesents numeric and bool status change of a single property of a component. </summary>
public readonly struct ShellyNotifyStatusMessage : IShellyStatusMessage
{
    /// <summary> Constructor valid for all native Shelly Components. </summary>
    public ShellyNotifyStatusMessage(DateTimeOffset timestamp, double value, byte id, ComponentType componentType, PropertyName propertyName)
    {
        Timestamp = timestamp;
        Value = value;
        Id = id;
        ComponentType = componentType;
        PropertyName = propertyName;
    }

    /// <summary> Constructor valid for all native Shelly Components. </summary>
    public ShellyNotifyStatusMessage(DateTimeOffset timestamp, double? value, byte id, ComponentType componentType, PropertyName propertyName)
    {
        Timestamp = timestamp;
        IsExplicitNull = value is null;
        Id = id;
        ComponentType = componentType;
        PropertyName = propertyName;
    }

    /// <summary> Constructor valid for all native Shelly Components. </summary>
    public ShellyNotifyStatusMessage(DateTimeOffset timestamp, long value, byte id, ComponentType componentType, PropertyName propertyName)
    {
        Timestamp = timestamp;
        Value = BitConverter.Int64BitsToDouble(value);
        Id = id;
        ComponentType = componentType;
        PropertyName = propertyName;
    }

    /// <summary> Constructor valid for all native Shelly Components. </summary>
    public ShellyNotifyStatusMessage(DateTimeOffset timestamp, bool? state, byte id, ComponentType componentType, PropertyName propertyName)
    {
        Timestamp = timestamp;
        Value = state is null ? null : state.Value ? 1 : 0;
        IsExplicitNull = state is null;
        Id = id;
        ComponentType = componentType;
        PropertyName = propertyName;
    }

    /// <summary> Constructor valid only for BTHomeSensor compoennts</summary>
    public ShellyNotifyStatusMessage(DateTimeOffset timestamp, bool state, byte id)
    {
        Timestamp = timestamp;
        Value = state ? 1 : 0;
        Id = id;
        ComponentType = ComponentType.BTHomeSensor;
    }

    /// <summary> Constructor valid only for BTHomeSensor compoennts</summary>
    public ShellyNotifyStatusMessage(DateTimeOffset timestamp, double value, byte id)
    {
        Timestamp = timestamp;
        Value = value;
        Id = id;
        ComponentType = ComponentType.BTHomeSensor;
    }

    public DateTimeOffset Timestamp { get; }
    public double? Value { get; }
    // Message normalized to 0-255 for BTHomeSensor message
    public byte Id { get; }
    public ComponentType ComponentType { get; }
    public PropertyName PropertyName { get; }
    public bool IsExplicitNull { get; }

    public bool? AsBool => IsExplicitNull ? null : Value != 0;
    public DateTimeOffset? AsDateTimeOffset => Value is null ? null : DateTimeOffset.FromUnixTimeMilliseconds(BitConverter.DoubleToInt64Bits(Value.Value));
    public long? AsLong => Value is null ? null : BitConverter.DoubleToInt64Bits(Value.Value);

}

/// <summary> Repesents numeric and bool status change of a single property of a component. </summary>
public readonly struct BTHomeSensorStatus 
{
    public DateTimeOffset Timestamp { get; }
    public bool AsValueBool => Value == 1;
    public double Value { get; }
    public string? Text { get; }
    public long MacAddress { get; }
    public byte Idx { get; }
    public BTHomeObjectType ObjectType { get; } 
     
     
    public BTHomeSensorStatus(DateTimeOffset timestamp, double value, long macAddress, byte idx, BTHomeObjectType objectType)
    {
        Timestamp = timestamp;
        Value = value; 
        MacAddress = macAddress;
        Idx = idx;
        ObjectType = objectType;
    }
      
    public BTHomeSensorStatus(DateTimeOffset timestamp, string? text, long macAddress, byte idx, BTHomeObjectType objectType)
    {
        Timestamp = timestamp;
        Text = text;
        MacAddress = macAddress;
        Idx = idx;
        ObjectType = objectType;
    }
     
}


public struct ShellyNotifyStatusStringMessage : IShellyStatusMessage
{
    /// <summary> Constructor valid for all native Shelly Components. </summary>
    internal ShellyNotifyStatusStringMessage(DateTimeOffset timestamp, string? state, byte id, ComponentType componentType, PropertyName propertyName)
    {
        Timestamp = timestamp;
        State = state;
        Id = id;
        ComponentType = componentType;
        PropertyName = propertyName;
    }

    /// <summary> Constructor valid only for BTHomeSensor compoennts</summary>
    internal ShellyNotifyStatusStringMessage(DateTimeOffset timestamp, string? state, byte id)
    {
        Timestamp = timestamp;
        State = state;
        Id = id;
        ComponentType = ComponentType.BTHomeSensor;
    }

    public DateTimeOffset Timestamp { get; }
    public string? State { get; }
    public byte Id { get; }
    public ComponentType ComponentType { get; }
    public PropertyName PropertyName { get; }
}

