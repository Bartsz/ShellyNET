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
    public ShellyNotifyStatusMessage(DateTimeOffset timestamp, double value, byte id, ComponentType componentType, PropertyName propertyName)
    {
        Timestamp = timestamp;
        Value = value;
        Id = id;
        ComponentType = componentType;
        PropertyName = propertyName;
    }

    public ShellyNotifyStatusMessage(DateTimeOffset timestamp, double? value, byte id, ComponentType componentType, PropertyName propertyName)
    {
        Timestamp = timestamp;
        IsExplicitNull = value is null;
        Id = id;
        ComponentType = componentType;
        PropertyName = propertyName;
    }

    public ShellyNotifyStatusMessage(DateTimeOffset timestamp, long value, byte id, ComponentType componentType, PropertyName propertyName)
    {
        Timestamp = timestamp;
        Value = BitConverter.Int64BitsToDouble(value);
        Id = id;
        ComponentType = componentType;
        PropertyName = propertyName;
    }

    public ShellyNotifyStatusMessage(DateTimeOffset timestamp, bool? state, byte id, ComponentType componentType, PropertyName propertyName)
    {
        Timestamp = timestamp;
        Value = state is null ? null : state.Value ? 1 : 0;
        IsExplicitNull = state is null;
        Id = id;
        ComponentType = componentType;
        PropertyName = propertyName;
    }

    public DateTimeOffset Timestamp { get; }
    public double? Value { get; }
    public byte Id { get; }
    public ComponentType ComponentType { get; }
    public PropertyName PropertyName { get; }
    public bool IsExplicitNull { get; }

    public bool? AsBool => IsExplicitNull ? null : Value != 0;
    public DateTimeOffset? AsDateTimeOffset => Value is null ? null : DateTimeOffset.FromUnixTimeMilliseconds(BitConverter.DoubleToInt64Bits(Value.Value));
    public long? AsLong => Value is null ? null : BitConverter.DoubleToInt64Bits(Value.Value);
}

public struct ShellyNotifyStatusStringMessage : IShellyStatusMessage

{
    internal ShellyNotifyStatusStringMessage(DateTimeOffset timestamp, string? state, byte id, ComponentType componentType, PropertyName propertyName)
    {
        Timestamp = timestamp;
        State = state;
        Id = id;
        ComponentType = componentType;
        PropertyName = propertyName;
    }

    public DateTimeOffset Timestamp { get; }
    public string? State { get; }
    public byte Id { get; }
    public ComponentType ComponentType { get; }
    public PropertyName PropertyName { get; }
}

