namespace ShellyNET.Client.Model.Enums;

public enum CameraStreamerState : byte
{
    Unknown = 0,
    Stopped,
    Starting,
    Running,
    Stopping,
}

public enum CameraError : byte
{
    Unknown = 0,
    StreamerFileSystemBad,
}

public enum WifiConnectionStatus : byte
{
    Unknown = 0,
    Disconnected,
    Connecting,
    Connected,
    GotIp,
}