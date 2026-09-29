using ShellyNET.Client.Model;
using System.Buffers.Binary;
using System.Net;

namespace ShellyNET.Client.Parsers;

public sealed partial class ShellyNotifyStatusParser
{

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

    public static bool TryPackMacAddress(ReadOnlySpan<byte> value, out long packedAddress)
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

    private bool ReadBluetoothAddressProperty(ref Utf8JsonReader reader, ReadOnlySpan<byte> propertyName, DateTimeOffset timestamp, byte componentId, ComponentType componentType)
    {
        if (!reader.ValueSpan.SequenceEqual(propertyName)) return false;

        reader.Read();
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.ValueSpan;
            var comma = value.LastIndexOf((byte)',');
            var address = comma >= 0 ? value[..comma] : value;
            var suffix = comma >= 0 ? value[(comma + 1)..] : ReadOnlySpan<byte>.Empty;

            if (TryPackMacAddress(address, out var packedAddress))
                AddMessage(new(timestamp, packedAddress, componentId, componentType, PropertyName.BluetoothAddress));

            var addressType = suffix.SequenceEqual("p"u8) ? BluetoothAddressType.Public :
                              suffix.SequenceEqual("rs"u8) ? BluetoothAddressType.RandomStatic :
                              suffix.SequenceEqual("rn"u8) ? BluetoothAddressType.RandomNonResolvable :
                              suffix.SequenceEqual("rr"u8) ? BluetoothAddressType.RandomResolvable : BluetoothAddressType.Unknown;
            AddMessage(new(timestamp, (long)addressType, componentId, componentType, PropertyName.BluetoothAddressType));
        }

        reader.Read();
        return true;
    }

    private bool ReadRGBProperty(ref Utf8JsonReader reader, ReadOnlySpan<byte> keyName, DateTimeOffset timestamp, byte componentId, ComponentType componentType, PropertyName property)
    {
        if (!reader.ValueSpan.SequenceEqual(keyName)) return false;

        byte R = 0;
        byte G = 0;
        byte B = 0;
        reader.Read(); // read to open array "["
        reader.Read(); // read to R 
        if (reader.TokenType == JsonTokenType.EndArray) { reader.Read(); return true; }

        if (reader.TokenType == JsonTokenType.Number) { R = reader.GetByte(); reader.Read(); } else if (reader.TokenType == JsonTokenType.Null) { reader.Read(); }
        if (reader.TokenType == JsonTokenType.Number) { G = reader.GetByte(); reader.Read(); } else if (reader.TokenType == JsonTokenType.Null) { reader.Read(); }
        if (reader.TokenType == JsonTokenType.Number) { B = reader.GetByte(); reader.Read(); } else if (reader.TokenType == JsonTokenType.Null) { reader.Read(); }

        long packedRGB = (R << 16) + (G << 8) + B;
        AddMessage(new(timestamp, packedRGB, componentId, componentType, property));

        if (reader.TokenType == JsonTokenType.EndArray) { reader.Read(); }
        return true;
    }

    private bool ReadMacAddressProperty(ref Utf8JsonReader reader, ReadOnlySpan<byte> propertyName, DateTimeOffset timestamp, byte componentId, ComponentType componentType, PropertyName property)
    {
        if (!reader.ValueSpan.SequenceEqual(propertyName)) return false;

        reader.Read();

        if (reader.TokenType == JsonTokenType.Null)
        {
            AddMessage(new(timestamp, (double?)null, componentId, componentType, property));
        }
        else if (reader.TokenType == JsonTokenType.String && TryPackMacAddress(reader.ValueSpan, out var packedAddress))
        {
            AddMessage(new(timestamp, packedAddress, componentId, componentType, property));
        }

        reader.Read();
        return true;
    }


    private bool ReadStringProperty(ref Utf8JsonReader reader, ReadOnlySpan<byte> propertyName, DateTimeOffset timestamp, byte componentId, ComponentType componentType, PropertyName property)
    {
        if (!reader.ValueSpan.SequenceEqual(propertyName)) return false;

        reader.Read();
        var state = reader.TokenType == JsonTokenType.Null ? string.Empty : reader.GetString() ?? string.Empty;
        AddStringStatusMessage(new(timestamp, state, componentId, componentType, property));
        reader.Read();
        return true;
    }

    private bool ReadJsonStringProperty(ref Utf8JsonReader reader, ReadOnlySpan<byte> propertyName, DateTimeOffset timestamp, byte componentId, ComponentType componentType, PropertyName property)
    {
        if (!reader.ValueSpan.SequenceEqual(propertyName)) return false;

        using var document = JsonDocument.ParseValue(ref reader);
        AddStringStatusMessage(new(timestamp, document.RootElement.GetRawText(), componentId, componentType, property));
        reader.Read();
        return true;
    }


    private bool ReadBoolProperty(ref Utf8JsonReader reader, ReadOnlySpan<byte> proprtyName, DateTimeOffset timestamp, byte componentId, ComponentType componentType, PropertyName property)
    {
        if (!reader.ValueSpan.SequenceEqual(proprtyName)) return false;

        reader.Read();
        AddMessage(new(timestamp, reader.GetBoolean(), componentId, componentType, property));
        reader.Read();
        return true;
    }

    private bool ReadNullableBoolProperty(ref Utf8JsonReader reader, ReadOnlySpan<byte> proprtyName, DateTimeOffset timestamp, byte componentId, ComponentType componentType, PropertyName property)
    {
        if (!reader.ValueSpan.SequenceEqual(proprtyName)) return false;

        reader.Read();
        if (reader.TokenType == JsonTokenType.Null)
            AddMessage(new(timestamp, (bool?)null, componentId, componentType, property));
        else
            AddMessage(new(timestamp, reader.GetBoolean(), componentId, componentType, property));

        reader.Read();
        return true;
    }

    private bool ReadInt64Property(ref Utf8JsonReader reader, ReadOnlySpan<byte> proprtyName, DateTimeOffset timestamp, byte componentId, ComponentType componentType, PropertyName property)
    {
        if (!reader.ValueSpan.SequenceEqual(proprtyName)) return false;

        reader.Read();
        AddMessage(new(timestamp, reader.GetInt64(), componentId, componentType, property));
        reader.Read();
        return true;
    }

    private bool ReadDoubleProperty(ref Utf8JsonReader reader, ReadOnlySpan<byte> proprtyName, DateTimeOffset timestamp, byte componentId, ComponentType componentType, PropertyName property)
    {
        if (!reader.ValueSpan.SequenceEqual(proprtyName)) return false;

        reader.Read();
        AddMessage(new(timestamp, reader.GetDouble(), componentId, componentType, property));
        reader.Read();
        return true;
    }

    private bool ReadNullableDoubleProperty(ref Utf8JsonReader reader, ReadOnlySpan<byte> proprtyName, DateTimeOffset timestamp, byte componentId, ComponentType componentType, PropertyName property)
    {
        if (!reader.ValueSpan.SequenceEqual(proprtyName)) return false;

        double? value = null;
        reader.Read();
        if (reader.TokenType != JsonTokenType.Null)
            value = reader.GetDouble();

        AddMessage(new(timestamp, value, componentId, componentType, property));

        reader.Read();
        return true;
    }

    private bool ReadTimeStampProperty(ref Utf8JsonReader reader, ReadOnlySpan<byte> proprtyName, DateTimeOffset timestamp, byte componentId, ComponentType componentType, PropertyName property)
    {
        if (!reader.ValueSpan.SequenceEqual(proprtyName)) return false;

        reader.Read();
        var ts = ParseUnixTimestamp(reader.ValueSpan);
        AddMessage(new(timestamp, ts, componentId, componentType, property));
        reader.Read();
        return true;
    }

    private bool ReadNullableTimeStampProperty(ref Utf8JsonReader reader, ReadOnlySpan<byte> proprtyName, DateTimeOffset timestamp, byte componentId, ComponentType componentType, PropertyName property)
    {
        if (!reader.ValueSpan.SequenceEqual(proprtyName)) return false;

        reader.Read();

        if (reader.TokenType == JsonTokenType.Null)
            AddMessage(new(timestamp, 0L, componentId, componentType, property));
        else
        {
            var ts = ParseUnixTimestamp(reader.ValueSpan);
            AddMessage(new(timestamp, ts, componentId, componentType, property));
        }
        reader.Read();
        return true;
    }

    private bool ReadNullableInt64Property(ref Utf8JsonReader reader, ReadOnlySpan<byte> propertyName, DateTimeOffset timestamp, byte componentId, ComponentType componentType, PropertyName property)
    {
        if (!reader.ValueSpan.SequenceEqual(propertyName)) return false;

        reader.Read();

        if (reader.TokenType == JsonTokenType.Null)
            AddMessage(new(timestamp, (double?)null, componentId, componentType, property));
        else
            AddMessage(new(timestamp, reader.GetInt64(), componentId, componentType, property));

        reader.Read();
        return true;
    }

    private bool ReadNullableStringProperty(ref Utf8JsonReader reader, ReadOnlySpan<byte> propertyName, DateTimeOffset timestamp, byte componentId, ComponentType componentType, PropertyName property)
    {
        if (!reader.ValueSpan.SequenceEqual(propertyName)) return false;

        reader.Read();

        if (reader.TokenType == JsonTokenType.Null)
            AddStringStatusMessage(new(timestamp, null, componentId, componentType, property));
        else
            AddStringStatusMessage(new(timestamp, reader.GetString(), componentId, componentType, property));

        reader.Read();
        return true;
    }
}
