namespace ShellyNET.Client;

public enum CoverError
{
    UnknownError = 0,
    /// <summary>When the temperature exceeds a predefined limit, <c>Cover</c> will stop immediately.</summary>
    Overtemp,

    /// <summary>When power exceeds the configured limit, <c>Cover</c> will stop immediately.</summary>
    Overpower,

    /// <summary>When the voltage exceeds the configured limit, <c>Cover</c> will stop immediately.</summary>
    Overvoltage,

    /// <summary>When voltage falls below the configured limit, <c>Cover</c> will stop immediately.</summary>
    Undervoltage,

    /// <summary>When current exceeds the configured limit, <c>Cover</c> will stop immediately.</summary>
    Overcurrent,

    /// <summary>When an obstruction is hit, <c>Cover</c> will execute a configured recovery action
    /// (stop or reverse).</summary>
    Obstruction,

    /// <summary>When the safety switch is engaged while <c>Cover</c> is moving in one of the monitored directions
    /// (see <c>safety_switch.direction</c>), the error will be set immediately and <c>Cover</c> will execute
    /// a configured recovery action (stop, reverse or pause). When the safety switch is engaged while
    /// <c>Cover</c> is idle or moving in a non-monitored direction, the error will be set on the first
    /// open/close/go to position command that requests movement in one of the monitored directions.</summary>
    SafetySwitch,

    /// <summary>When the power meter's feedback indicates the motor is rotating in a different direction
    /// than commanded, <c>Cover</c> will stop immediately.</summary>
    BadFeedbackRotatingInWrongDirection,

    /// <summary>When power meters feedback indicates both outputs connected to the motor are active
    /// simultaneously (this should never be possible), <c>Cover</c> will stop immediately.</summary>
    BadFeedbackBothDirectionsActive,

    /// <summary>When power meters feedback indicates the motor is still rotating though it has been
    /// commanded to stop.</summary>
    BadFeedbackFailedToHalt,

    /// <summary>When calibration is aborted since <c>Cover</c> failed to reach a fully open position
    /// within the configured timeout (<c>maxtime_open</c>).</summary>
    CalAbortTimeoutOpen,

    /// <summary>When calibration is aborted since <c>Cover</c> failed to reach a fully closed position
    /// within the configured timeout (<c>maxtime_close</c>).</summary>
    CalAbortTimeoutClose,

    /// <summary>When calibration is aborted since a <c>Cover</c> safety feature got triggered during
    /// the calibration process. This means that at least one of <c>overtemp</c>, <c>overpower</c>,
    /// <c>overvoltage</c>, <c>undervoltage</c>, <c>overcurrent</c> or <c>safety_switch</c>
    /// will also be present in <c>errors</c>.</summary>
    CalAbortSafety,

    /// <summary>When calibration is aborted since <c>Cover</c> received an external command to stop
    /// during the calibration process (via input, RPC call, etc.).</summary>
    CalAbortExtCommand,

    /// <summary>When calibration is aborted since <c>Cover</c> reported a mismatch between expected motor
    /// state and power meters feedback during the calibration process. This means that at least one of
    /// <c>bad_feedback:rotating_in_wrong_direction</c>, <c>bad_feedback:both_directions_active</c> or
    /// <c>bad_feedback:failed_to_halt</c> will also be present in <c>errors</c>.</summary>
    CalAbortBadFeedback,

    /// <summary>When calibration is aborted since the measured time to fully close is negative or 0.</summary>
    CalAbortImplausibleTimeToFullyClose,

    /// <summary>When calibration is aborted since the measured time to fully open is negative or 0.</summary>
    CalAbortImplausibleTimeToFullyOpen,

    /// <summary>When calibration is aborted since measured power consumption in close direction
    /// is negative or 0.</summary>
    CalAbortImplausiblePowerConsumptionInCloseDir,

    /// <summary>When calibration is aborted since measured power consumption in open direction
    /// is negative or 0.</summary>
    CalAbortImplausiblePowerConsumptionInOpenDir,

    /// <summary>When calibration is aborted since it took more steps than allowed to reach a fully closed
    /// position during the 10-step-calibration-movement.</summary>
    CalAbortTooManyStepsToClose,

    /// <summary>When calibration is aborted since it took fewer steps than allowed to reach a fully closed
    /// position during the 10-step-calibration-movement.</summary>
    CalAbortTooFewStepsToClose,

    /// <summary>When calibration is aborted since the time to reach a fully closed position during
    /// the 10-step-calibration-movement is negative or 0.</summary>
    CalAbortImplausibleTimeToFullyCloseWithSteps,

    /// <summary>When calibration is aborted since the actual step duration during
    /// the 10-step-calibration-movement in open direction is negative or 0.</summary>
    CalAbortImplausibleStepDurationInOpenDir,

    /// <summary>When calibration is aborted since it took more steps than allowed to reach a fully open
    /// position during the 10-step-calibration-movement.</summary>
    CalAbortTooManyStepsToOpen,

    /// <summary>When calibration is aborted since it took fewer steps than allowed to reach a fully open
    /// position during the 10-step-calibration-movement.</summary>
    CalAbortTooFewStepsToOpen,

    /// <summary>When calibration is aborted since the time to reach a fully open position during
    /// the 10-step-calibration-movement is negative or 0.</summary>
    CalAbortImplausibleTimeToFullyOpenWithSteps
}
