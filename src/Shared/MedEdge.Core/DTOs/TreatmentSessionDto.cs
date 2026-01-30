namespace MedEdge.Core.DTOs;

/// <summary>
/// Data transfer object for treatment sessions.
/// </summary>
public record TreatmentSessionDto(
    string Id,
    string PatientId,
    string PatientName,
    string StationId,
    string StationNumber,
    DateTime ScheduledStart,
    DateTime? ActualStart,
    DateTime? ActualEnd,
    string TreatmentType,
    string Status,
    string PrescribingPhysician,
    string? Notes,
    int? DurationMinutes,
    string CurrentPhase,
    List<TreatmentPhaseSummaryDto> Phases
);

/// <summary>
/// Summary DTO for treatment phases.
/// </summary>
public record TreatmentPhaseSummaryDto(
    string Id,
    string PhaseName,
    string Status,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    int? DurationMinutes
);

/// <summary>
/// Request DTO for creating a treatment session.
/// </summary>
public record CreateTreatmentSessionRequest(
    string PatientId,
    string StationId,
    DateTime ScheduledStart,
    string TreatmentType,
    string PrescribingPhysician,
    string? Notes
);

/// <summary>
/// Request DTO for starting a treatment session.
/// </summary>
public record StartTreatmentSessionRequest(
    DateTime ActualStart,
    string? Notes
);

/// <summary>
/// Request DTO for updating treatment session status/phase.
/// </summary>
public record UpdateTreatmentSessionRequest(
    string Status,
    string? CurrentPhaseId,
    string? Notes
);

/// <summary>
/// Request DTO for interrupting a treatment session.
/// </summary>
public record InterruptTreatmentSessionRequest(
    string InterruptionReason,
    string? Notes
);

/// <summary>
/// Request DTO for completing a treatment session.
/// </summary>
public record CompleteTreatmentSessionRequest(
    string OutcomeType,
    string Reason,
    string? Description,
    bool WasAdverseEvent,
    string? AdverseEventDetails
);

/// <summary>
/// Request DTO for creating a treatment observation.
/// </summary>
public record CreateTreatmentObservationRequest(
    string TreatmentSessionId,
    string ObservationType,
    string Code,
    string CodeDisplay,
    double? Value,
    string? Unit,
    string? Notes
);
