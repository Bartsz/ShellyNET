namespace ShellyNET.Client.Model.Enums;

public enum SysBootType : byte
{
    Unknown = 0,
    PowerOn = 1,
    SoftwareRestart = 2,
    DeepSleepWake = 3,
    Internal = 4
}

public enum SysWakeupCause : byte
{
    Unknown = 0,
    Button = 1,
    Usb = 2,
    Periodic = 3,
    StatusUpdate = 4,
    Alarm = 5,
    AlarmTest = 6,
    Undefined = 7
}

public enum SysProvisioningStage : byte
{
    Unknown = 0,
    Pending = 1,
    Confirmed = 2
}