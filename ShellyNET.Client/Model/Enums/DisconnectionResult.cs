namespace ShellyNET.Client.Model.Enums;

/// <summary>Describes how an explicit disconnection request completed.</summary>
public enum DisconnectionResult
{
    /// <summary>The active connection was closed.</summary>
    Disconnected,

    /// <summary>No active connection existed.</summary>
    NotConnected,

    /// <summary>The graceful shutdown was canceled and the transport was aborted.</summary>
    Canceled,

    /// <summary>The graceful shutdown failed because of a transport error and the transport was aborted.</summary>
    TransportError,

    /// <summary>The client failed while completing the requested shutdown.</summary>
    ClientError
}
