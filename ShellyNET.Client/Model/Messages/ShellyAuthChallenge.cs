namespace ShellyNET.Client.Model.Messages;

/// <summary>Represents the authentication challenge embedded as JSON in a Shelly 401 error message.</summary>
internal sealed class ShellyAuthChallenge
{
    /// <summary>Gets the discriminant used for exhaustive Shelly message handling.</summary>
    public ShellyMessageType MessageType => ShellyMessageType.AuthenticationChallange;

    /// <summary>Gets the authentication mechanism name.</summary>
    [JsonPropertyName("auth_type")]
    public string? AuthenticationType { get; init; }

    /// <summary>Gets the server nonce normalized to text while accepting numeric legacy challenges.</summary>
    [JsonConverter(typeof(ShellyNonceJsonConverter))]
    public string? Nonce { get; init; }

    /// <summary>Gets the optional legacy nonce counter returned through WebSocket challenges.</summary>
    [JsonPropertyName("nc")]
    public int? NonceCount { get; init; }

    /// <summary>Gets the authentication realm supplied by the device.</summary>
    public string? Realm { get; init; }

    /// <summary>Gets the digest algorithm requested by the device.</summary>
    public string? Algorithm { get; init; }

    /// <summary>Gets whether firmware 2.0+ rejected the previous request only because its nonce expired or was exhausted.</summary>
    public bool? Stale { get; init; }
}
