namespace ShellyNET.Client.Model.Enums;

 
public enum Pm1Error : byte
{
    Unknown = 0,
    OutOfRangeVoltage,
    OutOfRangeCurrent,
    PowerMeterFailure,
    OutOfRangeApparentPower,
    OutOfRangeActivePower
}

public enum Pm1Flag : byte
{
    Unknown = 0,
    Overcurrent,
    Overvoltage,
    Overpower,
    Undervoltage,
    Undercurrent,
    Underpower
}