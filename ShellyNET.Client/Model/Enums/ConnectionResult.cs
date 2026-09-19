namespace ShellyNET.Client.Model.Enums;

/// <summary>Describes how an attempt to establish the Shelly WebSocket transport completed.</summary>
public enum ConnectionResult
{
    /// <summary>The client connected to the documented ws:// device RPC endpoint.</summary>
    Connected,

    /// <summary>The client was already connected and no new connection was created.</summary>
    AlreadyConnected,

    /// <summary>The connection attempt was canceled before a transport was established.</summary>
    Canceled,

    /// <summary>Neither the plain nor TLS WebSocket endpoint could establish a usable connection.</summary>
    ConnectionFailed,

    /// <summary>The transport connected but Shelly.GetDeviceInfo could not initialize the client.</summary>
    InitializationFailed
}
