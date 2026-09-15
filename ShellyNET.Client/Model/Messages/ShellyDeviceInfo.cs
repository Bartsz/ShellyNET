namespace ShellyNET.Client.Model.Messages;

/// <summary>Describes identity, firmware, profile, authentication, and provisioning information reported by Shelly.GetDeviceInfo.</summary>
public sealed class ShellyDeviceInfo : IShellyMessage
{
    /// <summary>Gets the discriminant used for exhaustive Shelly message handling.</summary>
    public ShellyMessageType MessageType => ShellyMessageType.DeviceInfo;

    /// <summary>Gets the user-visible device name when the firmware includes it in the response.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the unique Shelly device identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the MAC address exactly as reported by the device.</summary>
    [JsonPropertyName("mac")]
    public required string MacAddress { get; init; }

    /// <summary>Gets the hardware slot identifier when exposed by the device.</summary>
    public int? Slot { get; init; }

    /// <summary>Gets the Shelly hardware model identifier.</summary>
    public required string Model { get; init; }

    /// <summary>Gets the Shelly hardware generation.</summary>
    [JsonPropertyName("gen")]
    public required int Generation { get; init; }

    /// <summary>Gets the firmware build identifier.</summary>
    [JsonPropertyName("fw_id")]
    public required string FirmwareId { get; init; }

    /// <summary>Gets the firmware version.</summary>
    [JsonPropertyName("ver")]
    public required string FirmwareVersion { get; init; }

    /// <summary>Gets the application name reported by the firmware.</summary>
    [JsonPropertyName("app")]
    public required string Application { get; init; }

    /// <summary>Gets the active profile for devices that support multiple profiles.</summary>
    public string? Profile { get; init; }

    /// <summary>Gets whether RPC authentication is enabled for the device.</summary>
    [JsonPropertyName("auth_en")]
    public required bool AuthenticationEnabled { get; init; }

    /// <summary>Gets the digest authentication realm, or null when authentication is disabled.</summary>
    [JsonPropertyName("auth_domain")]
    public string? AuthenticationDomain { get; init; }

    /// <summary>Gets the explicit discovery flag; null means the firmware omitted the field.</summary>
    public bool? Discoverable { get; init; }

    /// <summary>Gets the secure provisioning state when reported by firmware that supports it.</summary>
    [JsonPropertyName("provision")]
    public string? ProvisioningState { get; init; }

    /// <summary>Gets the cloud identity key when Shelly.GetDeviceInfo is called with ident=true.</summary>
    [JsonPropertyName("key")]
    public string? CloudKey { get; init; }

    /// <summary>Gets the manufacturing batch when Shelly.GetDeviceInfo is called with ident=true.</summary>
    public string? Batch { get; init; }

    /// <summary>Gets Shelly internal firmware flags when Shelly.GetDeviceInfo is called with ident=true.</summary>
    [JsonPropertyName("fw_sbits")]
    public string? FirmwareSbits { get; init; }

    /// <summary>Gets whether enhanced security is enabled when the firmware reports this field.</summary>
    [JsonPropertyName("enhanced_security")]
    public bool? EnhancedSecurity { get; init; }

    /// <summary>Gets the ShellyX JWS identifier when exposed by device-specific firmware.</summary>
    public string? Jti { get; init; }

    /// <summary>Gets decoded ShellyX JWS metadata without imposing a device-specific schema.</summary>
    public JsonElement? Jwt { get; init; }

    ///// <summary>Preserves device-specific and future fields not yet represented by this model.</summary>
    //[JsonExtensionData]
    //public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

