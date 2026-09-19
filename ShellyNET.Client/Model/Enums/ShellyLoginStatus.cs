namespace ShellyNET.Client.Model.Enums;

/// <summary>Describes the outcome of explicit Shelly digest-authentication validation.</summary>
public enum ShellyLoginStatus
{
    /// <summary>The credentials were accepted and digest state is ready for protected RPC calls.</summary>
    Success,

    /// <summary>The device does not currently require authentication.</summary>
    AuthenticationNotRequired,

    /// <summary>No active WebSocket connection exists.</summary>
    NotConnected,

    /// <summary>The caller canceled authentication.</summary>
    Canceled,

    /// <summary>A challenge or authenticated response exceeded the default RPC timeout.</summary>
    TimedOut,

    /// <summary>The connection ended before authentication completed.</summary>
    ConnectionLost,

    /// <summary>The authenticated retry was rejected with another 401 response.</summary>
    InvalidCredentials,

    /// <summary>The device requested an authentication mechanism or algorithm this client does not implement.</summary>
    UnsupportedAuthentication,

    /// <summary>Shelly returned a non-authentication RPC error while validating credentials.</summary>
    RpcError,

    /// <summary>The client could not parse or validate the authentication exchange.</summary>
    ClientError
}
