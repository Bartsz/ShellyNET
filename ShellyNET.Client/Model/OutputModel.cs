namespace ShellyNET.Client.Model;

/// <summary> Repesents numeric and bool status change of a single property of a component. </summary>
public readonly struct ShellyStateChangeMsg
{
    public ShellyStateChangeMsg(DateTimeOffset timestamp, double value, byte id, ComponentType componentType, PropertyName propertyType)
    {
        EventTimestamp = timestamp;
        Value = value;
        Id = id;
        ComponentType = componentType;
        PropertyType = propertyType;
    }

    public ShellyStateChangeMsg(DateTimeOffset timestamp, double? value, byte id, ComponentType componentType, PropertyName propertyType)
    {
        EventTimestamp = timestamp;
        IsNull = value is null;
        Id = id;
        ComponentType = componentType;
        PropertyType = propertyType;
    }

    public ShellyStateChangeMsg(DateTimeOffset timestamp, long value, byte id, ComponentType componentType, PropertyName propertyType)
    {
        EventTimestamp = timestamp;
        Value = BitConverter.Int64BitsToDouble(value);
        Id = id;
        ComponentType = componentType;
        PropertyType = propertyType;
    }

    public DateTimeOffset EventTimestamp { get; }
    public double Value { get; }
    public byte Id { get; }
    public ComponentType ComponentType { get; }
    public PropertyName PropertyType { get; }
    public bool IsNull { get; }

    public bool AsBool => Value != 0;
    public DateTimeOffset AsDateTimeOffset => DateTimeOffset.FromUnixTimeMilliseconds(BitConverter.DoubleToInt64Bits(Value));
    public long AsLong => BitConverter.DoubleToInt64Bits(Value);
}

