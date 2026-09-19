namespace ShellyNET.Client.Model.Enums;

public enum LightError : byte
{
    Unknown = 0,
    Overcurrent,
    Overvoltage,
    Overpower,
    Undervoltage,
    Overtemp,
    UnsupportedLoad,
    CalAbortInterrupted,
    CalAbortPowerRead,
    CalAbortNoLoad,
    CalAbortNoSynchro,
    CalAbortNonDimmable,
    CalAbortOverpower,
    CalAbortUnsupportedLoad
}

public enum LightFlag : byte
{
    Unknown = 0,
    NoLoad,
    Uncalibrated
}
