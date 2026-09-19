namespace ShellyNET.Client.Model;

public sealed class SystemStatus
{
    [JsonPropertyName("mac")]
    public string Mac { get; init; } = string.Empty;

    [JsonPropertyName("restart_required")]
    public bool RestartRequired { get; init; }

    [JsonPropertyName("time")]
    public string? Time { get; init; }

    [JsonPropertyName("unixtime")]
    public double? UnixTime { get; init; }

    [JsonPropertyName("last_sync_ts")]
    public double? LastSyncTimestamp { get; init; }

    [JsonPropertyName("uptime")]
    public long UptimeSeconds { get; init; }

    [JsonPropertyName("ram_size")]
    public long RamSizeBytes { get; init; }

    [JsonPropertyName("ram_free")]
    public long RamFreeBytes { get; init; }

    [JsonPropertyName("ram_min_free")]
    public long? RamMinimumFreeBytes { get; init; }

    [JsonPropertyName("fs_size")]
    public long FileSystemSizeBytes { get; init; }

    [JsonPropertyName("fs_free")]
    public long FileSystemFreeBytes { get; init; }

    [JsonPropertyName("cfg_rev")]
    public long ConfigurationRevision { get; init; }

    [JsonPropertyName("kvs_rev")]
    public long KvsRevision { get; init; }

    [JsonPropertyName("schedule_rev")]
    public long? ScheduleRevision { get; init; }

    [JsonPropertyName("webhook_rev")]
    public long? WebhookRevision { get; init; }

    [JsonPropertyName("knx_rev")]
    public long? KnxRevision { get; init; }

    [JsonPropertyName("btrelay_rev")]
    public long? BluetoothRelayRevision { get; init; }

    [JsonPropertyName("bthc_rev")]
    public long? BthomeControlRevision { get; init; }

    [JsonPropertyName("available_updates")]
    public SystemAvailableUpdates? AvailableUpdates { get; init; }

    [JsonPropertyName("wakeup_reason")]
    public SystemWakeupReason? WakeupReason { get; init; }

    [JsonPropertyName("wakeup_period")]
    public long? WakeupPeriodSeconds { get; init; }

    [JsonPropertyName("reset_reason")]
    public long? ResetReason { get; init; }

    [JsonPropertyName("utc_offset")]
    public int? UtcOffsetSeconds { get; init; }

    [JsonPropertyName("ch")]
    public string[]? Channels { get; init; }

    [JsonPropertyName("provisioning")]
    public SystemProvisioning? Provisioning { get; init; }
}

public sealed class SystemAvailableUpdates
{
    [JsonPropertyName("beta")]
    public SystemFirmwareUpdate? Beta { get; init; }

    [JsonPropertyName("stable")]
    public SystemFirmwareUpdate? Stable { get; init; }

    [JsonPropertyName("test")]
    public SystemFirmwareUpdate? Test { get; init; }
}

public sealed class SystemFirmwareUpdate
{
    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;

    [JsonPropertyName("nsteps")]
    public int? NumberOfSteps { get; init; }
}

public sealed class SystemWakeupReason
{
    [JsonPropertyName("boot")]
    public SystemWakeupBoot Boot { get; init; }

    [JsonPropertyName("cause")]
    public SystemWakeupCause Cause { get; init; }
}

public sealed class SystemProvisioning
{
    [JsonPropertyName("stage")]
    public SystemProvisioningStage Stage { get; init; }

    [JsonPropertyName("started_at")]
    public double StartedAt { get; init; }

    [JsonPropertyName("started_ut")]
    public long StartedUptimeSeconds { get; init; }

    [JsonPropertyName("duration")]
    public long DurationSeconds { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter<SystemWakeupBoot>))]
public enum SystemWakeupBoot : byte
{
    [JsonStringEnumMemberName("unknown")]
    Unknown = 0,

    [JsonStringEnumMemberName("poweron")]
    PowerOn,

    [JsonStringEnumMemberName("software_restart")]
    SoftwareRestart,

    [JsonStringEnumMemberName("deepsleep_wake")]
    DeepSleepWake,

    [JsonStringEnumMemberName("internal")]
    Internal,
}

[JsonConverter(typeof(JsonStringEnumConverter<SystemWakeupCause>))]
public enum SystemWakeupCause : byte
{
    [JsonStringEnumMemberName("undefined")]
    Undefined = 0,

    [JsonStringEnumMemberName("button")]
    Button,

    [JsonStringEnumMemberName("usb")]
    Usb,

    [JsonStringEnumMemberName("periodic")]
    Periodic,

    [JsonStringEnumMemberName("status_update")]
    StatusUpdate,

    [JsonStringEnumMemberName("alarm")]
    Alarm,

    [JsonStringEnumMemberName("alarm_test")]
    AlarmTest,
}

[JsonConverter(typeof(JsonStringEnumConverter<SystemProvisioningStage>))]
public enum SystemProvisioningStage : byte
{
    [JsonStringEnumMemberName("pending")]
    Pending = 0,

    [JsonStringEnumMemberName("confirmed")]
    Confirmed,
}