namespace ShellyNET.Client.Model;

/// <summary> Property names aggregated from all component NotifyStatus types. </summary>
public enum PropertyName : byte
{
    Output,
    ActivePower,
    OnTime,
    TemperatureC,
    Brightness,
    TimerStatedAt,
    TimerDuration,
    Transition,
    Id,
    Voltage,
    PowerFactor,
    Frequency,
    LastMinuteEnergyReturned_mWh,
    TotalEnergyReturnedWh,
    LastMinuteEnergy_mWh,
    TotalOnTimeMinutes,
    OffToOnCylesCount,
    OffToOnCyckesCountLastResetTime,
    TotalMinutesAbovePowerThreshold,
    TotalMinutesAbovePowerThrLastResetTime,
 
    Current,
    TotalEnergyConsumedWh,
    CoverCurrentPosition,
    CoverTargetPosition,
    CoverMoveTimeoutSeconds,
    CoverMoveStartedAUnixMs,
    CoverHasPositionControl,
    CoverLastDirection,
    CoverSlatPosition,
    Error,
    State,
     
    LastMinuteEnergyConsumed_mWh,
    Flag,

    Percent,
    PercentTransformed,
    InputTotalCount,
    InputTotalCountTransformed,
    InputLastMinuteCount,
    InputTransformedLastMinuteCount,
    FrequencyTransformed,
     
    TransitionTargetOutput,
    TransitionTargetBrightness,
    TransitionStartedAt,
    TransitionDuration,
    TemperatureF,
    CalibrationProgress,

    Red,
    Green,
    Blue,
    White,
    ColorTemperatureK,
    RGBCCTMode,
    TransitionTargetRed,
    TransitionTargetGreen,
    TransitionTargetBlue,
    WhiteTransitionTarget,
    TransitionTargetColorTemperatureK,
    TotalOnTimeCounterLastResetTime,
    CurrentRgb,
    RgbTransitionTarget,
    ColorTemperatureKTarget,
    Connected,
    ApperentPower,


    CameraZoneMotion,
    StorageRevision,
    StoragePresent,
    StorageActive,
    StorageFileSystemFreeBytes,
    StorageFileSystemSizeBytes,
    BluetoothAddress,
    BluetoothAddressType,
    BlePairingStartedAt,
    BlePairingDurationSeconds,
    BleAssociationStartedAt,
    BleAssociationDurationSeconds,
    CbTotalCycles,
    CbSafetyLocked,
    CbErrorVoltmeterId,
    DeviceBatteryVoltage,
    DeviceBatteryPercent,
    ExternalPowerPresent,
    EmPhaseACurrent,
    EmPhaseAVoltage,
    EmPhaseAActivePower,
    EmPhaseAApparentPower,
    EmPhaseAPowerFactor,
    EmPhaseAFrequency,
    EmPhaseAError,
    EmPhaseAFlag,
    EmPhaseBCurrent,
    EmPhaseBVoltage,
    EmPhaseBActivePower,
    EmPhaseBApparentPower,
    EmPhaseBPowerFactor,
    EmPhaseBFrequency,
    EmPhaseBError,
    EmPhaseBFlag,
    EmPhaseCCurrent,
    EmPhaseCVoltage,
    EmPhaseCActivePower,
    EmPhaseCApparentPower,
    EmPhaseCPowerFactor,
    EmPhaseCFrequency,
    EmPhaseCError,
    EmPhaseCFlag,
    EmNeutralCurrent,
    EmNeutralError,
    EmTotalCurrent,
    EmTotalActivePower,
    EmTotalApparentPower,
    EmUserCalibratedPhase,
    Em1FactoryCalibration,
    Em1CalibrationSourceComponentId,
    EmPhaseATotalActiveEnergyWh,
    EmPhaseATotalActiveReturnedEnergyWh,
    EmPhaseBTotalActiveEnergyWh,
    EmPhaseBTotalActiveReturnedEnergyWh,
    EmPhaseCTotalActiveEnergyWh,
    EmPhaseCTotalActiveReturnedEnergyWh,
    EmTotalActiveEnergyWh,
    EmTotalActiveReturnedEnergyWh,
    IpAddress,
    Alarm,
    Mute,
    RelativeHumidityPercent,
    IlluminanceLux,
    IlluminationLevel,
    MatterFabricCount,
    MatterCommissionable,
    Enabled,
    PresenceLiveTrackStartedAt,
    PresenceLiveTrackDurationSeconds,
    PresenceLiveTrackIntervalSeconds,
    PresenceZoneState,
    PresenceZoneObjectCount,
    TransformedVoltage,
    ZigbeeNetworkState,

    CameraArmed,
    CameraPrivacy,
    CameraStreamerState,
    CameraStreamerVersion,
    CameraMotion,
    CameraActiveStreams,
    CameraRecordingEncryptionConfigured,
    CameraRecordings,
    WifiConnectionStatus,
    WifiSsid,
    WifiBssid,
    WifiChannel,
    WifiRssi,
    WifiAccessPointClientCount,
    MacAddress,
    Time,
    UnixTime,
    LastSyncTimestamp,
    Uptime,
    RamSize,
    RamFree,
    FileSystemSize,
    FileSystemFree,
    ConfigRevision,
    KvsRevision,
    RestartRequired,
    BetaUpdateVersion,
    BetaUpdateSteps,
    StableUpdateVersion,
    StableUpdateSteps,
    ScheduleRevision,
    WebhookRevision,
    KnxRevision,
    BluetoothRelayRevision,
    BluetoothHomeControlRevision,
    WakeupPeriod,
    UtcOffset,
    BootType,
    WakeupCause,
    ProvisioningStartedAt,
    ProvisioningStartedUptime,
    ProvisioningDuration,
    ProvisioningStage,
}



public enum StorageError : byte
{
    Unknown = 0,
    NeedsFormatting,
    Mounting,
    MountFailed,
    MountingNfs,
    UnmountingNfs,
    UnmountFailed,
    ResolvingNfsHost,
    FileSystemError,
    Formatting,
    FormatFailed,
    Ejected,
    EjectFailed,
}

public enum BleFlag : byte
{
    Unknown = 0,
    Scanning,
    Advertising,
    Connected,
}

public enum BluetoothAddressType : byte
{
    Unknown = 0,
    Public,
    RandomStatic,
    RandomNonResolvable,
    RandomResolvable,
}

public enum CbError : byte
{
    Unknown = 0,
    Overtemp,
    Overvoltage,
    Undervoltage,
    Lever,
}

public enum CctError : byte
{
    Unknown = 0,
    Overtemp,
}

public enum DevicePowerError : byte
{
    Unknown = 0,
    Read,
}

public enum EmError : byte
{
    Unknown = 0,
    OutOfRangeActivePower,
    OutOfRangeApparentPower,
    OutOfRangeVoltage,
    OutOfRangeCurrent,
    PowerMeterFailure,
    PhaseSequence,
    CtTypeNotSet,
}

public enum EmFlag : byte
{
    Unknown = 0,
    Overcurrent,
    Overvoltage,
    Overpower,
    Undervoltage,
    Undercurrent,
    Underpower,
}

public enum EmPhase : byte
{
    Unknown = 0,
    A,
    B,
    C,
}

public enum Em1Error : byte
{
    Unknown = 0,
    OutOfRangeVoltage,
    OutOfRangeCurrent,
    PowerMeterFailure,
    CtTypeNotSet,
    OutOfRangeActivePower,
    OutOfRangeApparentPower,
}

public enum Em1Flag : byte
{
    Unknown = 0,
    Overcurrent,
    Overvoltage,
    Overpower,
    Undervoltage,
    Undercurrent,
    Underpower,
    CountDisabled,
}

public enum Em1DataError : byte
{
    Unknown = 0,
    CtTypeNotSet,
    DatabaseError,
}

public enum EmDataError : byte
{
    Unknown = 0,
    CtTypeNotSet,
    DatabaseError,
}

public enum FloodError : byte
{
    Unknown = 0,
    CableUnplugged,
}

public enum HumidityError : byte
{
    Unknown = 0,
    Read,
    OutOfRange,
}

public enum IlluminanceError : byte
{
    Unknown = 0,
    Read,
    OutOfRange,
}

public enum IlluminationLevel : byte
{
    Unknown = 0,
    Dark,
    Twilight,
    Bright,
}

public enum TemperatureError : byte
{
    Unknown = 0,
    Read,
    OutOfRange,
}

public enum VoltmeterError : byte
{
    Unknown = 0,
    Read,
    OutOfRange,
}

public enum ZigbeeNetworkState : byte
{
    Unknown = 0,
    Disabled,
    Initializing,
    Steering,
    Joined,
    Failed,
}