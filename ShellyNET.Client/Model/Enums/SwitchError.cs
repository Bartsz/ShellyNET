namespace ShellyNET.Client;

public enum SwitchError : byte
{
    UnknownError = 0,
    Overtemp,
    Overpower,
    Overvoltage,
    Undervoltage,
}
