namespace ShellyNET.Client.Model.Enums;

/// <summary>Identifies a BTHome v2 object by its protocol Object ID.</summary>
public enum BTHomeObjectType : byte
{
    /// <summary>Packet ID. Data type: UInt8. Unit: none. Resolution: 1.</summary>
    PacketId = 0x00,

    /// <summary>Battery level. Data type: UInt8. Unit: %. Resolution: 1 %.</summary>
    Battery = 0x01,

    /// <summary>Temperature. Data type: Int16. Unit: °C. Resolution: 0.01 °C.</summary>
    Temperature_0_01 = 0x02,

    /// <summary>Relative humidity. Data type: UInt16. Unit: %. Resolution: 0.01 %.</summary>
    Humidity_0_01 = 0x03,

    /// <summary>Atmospheric pressure. Data type: UInt24. Unit: hPa. Resolution: 0.01 hPa.</summary>
    Pressure = 0x04,

    /// <summary>Illuminance. Data type: UInt24. Unit: lx. Resolution: 0.01 lx.</summary>
    Illuminance = 0x05,

    /// <summary>Mass. Data type: UInt16. Unit: kg. Resolution: 0.01 kg.</summary>
    Mass_Kilogram = 0x06,

    /// <summary>Mass. Data type: UInt16. Unit: lb. Resolution: 0.01 lb.</summary>
    Mass_Pound = 0x07,

    /// <summary>Dew point. Data type: Int16. Unit: °C. Resolution: 0.01 °C.</summary>
    DewPoint = 0x08,

    /// <summary>Count. Data type: UInt8. Unit: count. Resolution: 1.</summary>
    Count_UInt8 = 0x09,

    /// <summary>Energy. Data type: UInt24. Unit: kWh. Resolution: 0.001 kWh.</summary>
    Energy_UInt24 = 0x0A,

    /// <summary>Power. Data type: UInt24. Unit: W. Resolution: 0.01 W.</summary>
    Power_UInt24 = 0x0B,

    /// <summary>Voltage. Data type: UInt16. Unit: V. Resolution: 0.001 V.</summary>
    Voltage_0_001 = 0x0C,

    /// <summary>PM2.5 concentration. Data type: UInt16. Unit: µg/m³. Resolution: 1 µg/m³.</summary>
    Pm2_5 = 0x0D,

    /// <summary>PM10 concentration. Data type: UInt16. Unit: µg/m³. Resolution: 1 µg/m³.</summary>
    Pm10 = 0x0E,

    /// <summary>Generic binary state. Data type: UInt8. Unit: boolean. Resolution: 1.</summary>
    GenericBoolean = 0x0F,

    /// <summary>Power binary state. Data type: UInt8. Unit: boolean. Resolution: 1.</summary>
    PowerBinary = 0x10,

    /// <summary>Opening binary state. Data type: UInt8. Unit: boolean. Resolution: 1.</summary>
    Opening = 0x11,

    /// <summary>CO₂ concentration. Data type: UInt16. Unit: ppm. Resolution: 1 ppm.</summary>
    Co2 = 0x12,

    /// <summary>Total volatile organic compounds concentration. Data type: UInt16. Unit: µg/m³. Resolution: 1 µg/m³.</summary>
    Tvoc = 0x13,

    /// <summary>Moisture. Data type: UInt16. Unit: %. Resolution: 0.01 %.</summary>
    Moisture_0_01 = 0x14,

    /// <summary>Low-battery binary state. Data type: UInt8. Unit: boolean. Resolution: 1.</summary>
    BatteryLow = 0x15,

    /// <summary>Battery-charging binary state. Data type: UInt8. Unit: boolean. Resolution: 1.</summary>
    BatteryCharging = 0x16,

    /// <summary>Carbon-monoxide detection state. Data type: UInt8. Unit: boolean. Resolution: 1.</summary>
    CarbonMonoxide = 0x17,

    /// <summary>Cold-condition state. Data type: UInt8. Unit: boolean. Resolution: 1.</summary>
    Cold = 0x18,

    /// <summary>Connectivity state. Data type: UInt8. Unit: boolean. Resolution: 1.</summary>
    Connectivity = 0x19,

    /// <summary>Door state. Data type: UInt8. Unit: boolean. Resolution: 1.</summary>
    Door = 0x1A,

    /// <summary>Garage-door state. Data type: UInt8. Unit: boolean. Resolution: 1.</summary>
    GarageDoor = 0x1B,

    /// <summary>Gas-detection state. Data type: UInt8. Unit: boolean. Resolution: 1.</summary>
    GasBinary = 0x1C,

    /// <summary>Heat-condition state. Data type: UInt8. Unit: boolean. Resolution: 1.</summary>
    Heat = 0x1D,

    /// <summary>Light-detection state. Data type: UInt8. Unit: boolean. Resolution: 1.</summary>
    Light = 0x1E,

    /// <summary>Lock state. Data type: UInt8. Unit: boolean. Resolution: 1.</summary>
    Lock = 0x1F,

    /// <summary>Moisture-detection state. Data type: UInt8. Unit: boolean. Resolution: 1.</summary>
    MoistureBinary = 0x20,

    /// <summary>Motion-detection state. Data type: UInt8. Unit: boolean. Resolution: 1.</summary>
    Motion = 0x21,

    /// <summary>Moving state. Data type: UInt8. Unit: boolean. Resolution: 1.</summary>
    Moving = 0x22,

    /// <summary>Occupancy state. Data type: UInt8. Unit: boolean. Resolution: 1.</summary>
    Occupancy = 0x23,

    /// <summary>Plug state. Data type: UInt8. Unit: boolean. Resolution: 1.</summary>
    Plug = 0x24,

    /// <summary>Presence state. Data type: UInt8. Unit: boolean. Resolution: 1.</summary>
    Presence = 0x25,

    /// <summary>Problem state. Data type: UInt8. Unit: boolean. Resolution: 1.</summary>
    Problem = 0x26,

    /// <summary>Running state. Data type: UInt8. Unit: boolean. Resolution: 1.</summary>
    Running = 0x27,

    /// <summary>Safety state. Data type: UInt8. Unit: boolean. Resolution: 1.</summary>
    Safety = 0x28,

    /// <summary>Smoke-detection state. Data type: UInt8. Unit: boolean. Resolution: 1.</summary>
    Smoke = 0x29,

    /// <summary>Sound-detection state. Data type: UInt8. Unit: boolean. Resolution: 1.</summary>
    Sound = 0x2A,

    /// <summary>Tamper state. Data type: UInt8. Unit: boolean. Resolution: 1.</summary>
    Tamper = 0x2B,

    /// <summary>Vibration-detection state. Data type: UInt8. Unit: boolean. Resolution: 1.</summary>
    Vibration = 0x2C,

    /// <summary>Window state. Data type: UInt8. Unit: boolean. Resolution: 1.</summary>
    Window = 0x2D,

    /// <summary>Relative humidity. Data type: UInt8. Unit: %. Resolution: 1 %.</summary>
    Humidity_1 = 0x2E,

    /// <summary>Moisture. Data type: UInt8. Unit: %. Resolution: 1 %.</summary>
    Moisture_1 = 0x2F,

    /// <summary>Button event. Data type: UInt8 event ID. Unit: event. Resolution: 1 event ID.</summary>
    ButtonEvent = 0x3A,

    /// <summary>Command event. Variable-length command object. Unit: command. Resolution: 1 opcode.</summary>
    Command = 0x3B,

    /// <summary>Dimmer event. Variable-length event object. Unit: event/steps. Resolution: 1 event ID or step.</summary>
    DimmerEvent = 0x3C,

    /// <summary>Count. Data type: UInt16. Unit: count. Resolution: 1.</summary>
    Count_UInt16 = 0x3D,

    /// <summary>Count. Data type: UInt32. Unit: count. Resolution: 1.</summary>
    Count_UInt32 = 0x3E,

    /// <summary>Rotation. Data type: Int16. Unit: °. Resolution: 0.1°.</summary>
    Rotation = 0x3F,

    /// <summary>Distance. Data type: UInt16. Unit: mm. Resolution: 1 mm.</summary>
    Distance_Millimeter = 0x40,

    /// <summary>Distance. Data type: UInt16. Unit: m. Resolution: 0.1 m.</summary>
    Distance_Meter = 0x41,

    /// <summary>Duration. Data type: UInt24. Unit: s. Resolution: 0.001 s.</summary>
    Duration = 0x42,

    /// <summary>Current. Data type: UInt16. Unit: A. Resolution: 0.001 A.</summary>
    Current_UInt16 = 0x43,

    /// <summary>Speed. Data type: UInt16. Unit: m/s. Resolution: 0.01 m/s.</summary>
    Speed_UInt16 = 0x44,

    /// <summary>Temperature. Data type: Int16. Unit: °C. Resolution: 0.1 °C.</summary>
    Temperature_0_1 = 0x45,

    /// <summary>UV index. Data type: UInt8. Unit: UV index. Resolution: 0.1.</summary>
    UvIndex = 0x46,

    /// <summary>Volume. Data type: UInt16. Unit: L. Resolution: 0.1 L.</summary>
    Volume_Liter_0_1 = 0x47,

    /// <summary>Volume. Data type: UInt16. Unit: mL. Resolution: 1 mL.</summary>
    Volume_Milliliter = 0x48,

    /// <summary>Volume flow rate. Data type: UInt16. Unit: m³/h. Resolution: 0.001 m³/h.</summary>
    VolumeFlowRate = 0x49,

    /// <summary>Voltage. Data type: UInt16. Unit: V. Resolution: 0.1 V.</summary>
    Voltage_0_1 = 0x4A,

    /// <summary>Gas volume. Data type: UInt24. Unit: m³. Resolution: 0.001 m³.</summary>
    Gas_UInt24 = 0x4B,

    /// <summary>Gas volume. Data type: UInt32. Unit: m³. Resolution: 0.001 m³.</summary>
    Gas_UInt32 = 0x4C,

    /// <summary>Energy. Data type: UInt32. Unit: kWh. Resolution: 0.001 kWh.</summary>
    Energy_UInt32 = 0x4D,

    /// <summary>Volume. Data type: UInt32. Unit: L. Resolution: 0.001 L.</summary>
    Volume_Liter_0_001 = 0x4E,

    /// <summary>Water volume. Data type: UInt32. Unit: L. Resolution: 0.001 L.</summary>
    Water = 0x4F,

    /// <summary>Unix timestamp. Data type: UInt32. Unit: seconds since Unix epoch. Resolution: 1 s.</summary>
    Timestamp = 0x50,

    /// <summary>Acceleration. Data type: UInt16. Unit: m/s². Resolution: 0.001 m/s².</summary>
    Acceleration_UInt16 = 0x51,

    /// <summary>Gyroscope angular velocity. Data type: UInt16. Unit: °/s. Resolution: 0.001 °/s.</summary>
    Gyroscope = 0x52,

    /// <summary>UTF-8 text. Variable-length data. Unit: text. Resolution: one UTF-8 byte/code sequence.</summary>
    Text = 0x53,

    /// <summary>Raw byte sequence represented as hexadecimal text by receivers. Variable-length data. Unit: byte. Resolution: 1 byte.</summary>
    Raw = 0x54,

    /// <summary>Stored volume. Data type: UInt32. Unit: L. Resolution: 0.001 L.</summary>
    VolumeStorage = 0x55,

    /// <summary>Conductivity. Data type: UInt16. Unit: µS/cm. Resolution: 1 µS/cm.</summary>
    Conductivity = 0x56,

    /// <summary>Temperature. Data type: Int8. Unit: °C. Resolution: 1 °C.</summary>
    Temperature_1 = 0x57,

    /// <summary>Temperature. Data type: Int8. Unit: °C. Resolution: 0.35 °C.</summary>
    Temperature_0_35 = 0x58,

    /// <summary>Count. Data type: Int8. Unit: count. Resolution: 1.</summary>
    Count_Int8 = 0x59,

    /// <summary>Count. Data type: Int16. Unit: count. Resolution: 1.</summary>
    Count_Int16 = 0x5A,

    /// <summary>Count. Data type: Int32. Unit: count. Resolution: 1.</summary>
    Count_Int32 = 0x5B,

    /// <summary>Power. Data type: Int32. Unit: W. Resolution: 0.01 W.</summary>
    Power_Int32 = 0x5C,

    /// <summary>Current. Data type: Int16. Unit: A. Resolution: 0.001 A.</summary>
    Current_Int16 = 0x5D,

    /// <summary>Direction. Data type: UInt16. Unit: °. Resolution: 0.01°.</summary>
    Direction = 0x5E,

    /// <summary>Precipitation. Data type: UInt16. Unit: mm. Resolution: 0.1 mm.</summary>
    Precipitation = 0x5F,

    /// <summary>Channel number. Data type: UInt8. Unit: channel. Resolution: 1.</summary>
    Channel = 0x60,

    /// <summary>Rotational speed. Data type: UInt16. Unit: rpm. Resolution: 1 rpm.</summary>
    RotationalSpeed = 0x61,

    /// <summary>Signed speed. Data type: Int32. Unit: m/s. Resolution: 0.000001 m/s.</summary>
    Speed_Int32 = 0x62,

    /// <summary>Signed acceleration. Data type: Int32. Unit: m/s². Resolution: 0.000001 m/s².</summary>
    Acceleration_Int32 = 0x63,

    /// <summary>Light level. Data type: UInt8. Unit: level (0 dark, 1 twilight, 2 bright). Resolution: 1 level.</summary>
    LightLevel = 0x64,

    /// <summary>Settings revision. Data type: UInt8. Unit: revision. Resolution: 1.</summary>
    SettingsRevision = 0x65,

    /// <summary>Device type identifier. Data type: UInt16. Unit: identifier. Resolution: 1.</summary>
    DeviceTypeId = 0xF0,

    /// <summary>Firmware version encoded in four bytes. Data type: UInt32. Unit: version. Resolution: one version component byte.</summary>
    FirmwareVersion_UInt32 = 0xF1,

    /// <summary>Firmware version encoded in three bytes. Data type: UInt24. Unit: version. Resolution: one version component byte.</summary>
    FirmwareVersion_UInt24 = 0xF2,
}
