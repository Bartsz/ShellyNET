namespace ShellyNET.Client.Model;

/// <summary> Property names aggregated from all component NotifyStatus types. </summary>
public enum PropertyName : byte
{
    Output,
    AperentPower,
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
    LastOffToOnCyckesCountReset,
    TotalMinutesAbovePowerThreshold,
    LastTotalMinutesAbovePowerThrReset,
 
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
}
