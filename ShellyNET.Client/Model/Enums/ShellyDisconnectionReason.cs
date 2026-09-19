namespace ShellyNET.Client.Model.Enums;

/// <summary>Describes why an established connection ended without an explicit DisconnectAsync request.</summary>
public enum ShellyDisconnectionReason
{
    /// <summary>The remote Shelly endpoint initiated a WebSocket close.</summary>
    RemoteClose,

    /// <summary>The transport failed while a connection loop was active.</summary>
    TransportError,

    /// <summary>The client failed while processing an established connection.</summary>
    ClientError
}
