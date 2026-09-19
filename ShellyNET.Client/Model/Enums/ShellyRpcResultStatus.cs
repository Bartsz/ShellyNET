namespace ShellyNET.Client;

/// <summary>Describes how an RPC call completed across Shelly protocol and client transport concerns.</summary>
public enum ShellyRpcResultStatus
{
    /// <summary>Shelly returned a successful RPC result.</summary>
    Success,

    /// <summary>Shelly returned an explicit JSON-RPC error response.</summary>
    RpcError,

    /// <summary>The operation requires an active connection but no connection exists.</summary>
    NotConnected,

    /// <summary>The operation was canceled by the caller.</summary>
    Canceled,

    /// <summary>The response did not arrive before the default RPC timeout elapsed.</summary>
    TimedOut,

    /// <summary>The connection ended before the RPC response could be received.</summary>
    ConnectionLost,

    /// <summary>The client could not process the operation or response.</summary>
    ClientError
}
