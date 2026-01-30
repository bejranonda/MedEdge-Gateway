namespace MedEdge.Core.Domain.Entities;

/// <summary>
/// Represents a treatment session for a patient at a station.
/// Tracks the entire lifecycle from scheduling through completion.
/// </summary>
public class TreatmentSessionEntity
{
    public string Id { get; set; } = default!;
    public string PatientId { get; set; } = default!;
    public string StationId { get; set; } = default!;
    public DateTime ScheduledStart { get; set; }
    public DateTime? ActualStart { get; set; }
    public DateTime? ActualEnd { get; set; }
    public string TreatmentType { get; set; } = "general"; // hemodialysis, peritoneal, infusion, general
    public string Status { get; set; } = "scheduled"; // scheduled, in-progress, completed, interrupted, cancelled
    public string PrescribingPhysician { get; set; } = default!;
    public string? Notes { get; set; }
    public string? InterruptionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public FhirPatientEntity Patient { get; set; } = default!;
    public StationEntity Station { get; set; } = default!;
    public ICollection<TreatmentPhaseEntity> Phases { get; set; } = new List<TreatmentPhaseEntity>();
    public ICollection<TreatmentObservationEntity> Observations { get; set; } = new List<TreatmentObservationEntity>();
    public ICollection<TreatmentOutcomeEntity> Outcomes { get; set; } = new List<TreatmentOutcomeEntity>();
}

/// <summary>
/// Represents a phase within a treatment session.
/// Phases could be: Setup, Treatment, Cleanup, etc.
/// </summary>
public class TreatmentPhaseEntity
{
    public string Id { get; set; } = default!;
    public string TreatmentSessionId { get; set; } = default!;
    public string PhaseName { get; set; } = default!; // setup, treatment, cleanup, monitoring
    public string Status { get; set; } = "pending"; // pending, in-progress, completed, skipped
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Notes { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public TreatmentSessionEntity TreatmentSession { get; set; } = default!;
}

/// <summary>
/// Observations recorded specifically during a treatment session.
/// Links to the broader FHIR observations but provides treatment context.
/// </summary>
public class TreatmentObservationEntity
{
    public string Id { get; set; } = default!;
    public string TreatmentSessionId { get; set; } = default!;
    public string ObservationType { get; set; } = default!; // vital-sign, alarm, medication-given, parameter-change
    public string Code { get; set; } = default!; // LOINC or local code
    public string CodeDisplay { get; set; } = default!;
    public double? Value { get; set; }
    public string? Unit { get; set; }
    public string? Notes { get; set; }
    public DateTime RecordedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public TreatmentSessionEntity TreatmentSession { get; set; } = default!;
}

/// <summary>
/// Records the outcome of a treatment session.
/// </summary>
public class TreatmentOutcomeEntity
{
    public string Id { get; set; } = default!;
    public string TreatmentSessionId { get; set; } = default!;
    public string OutcomeType { get; set; } = default!; // completed, interrupted, complication, transferred
    public string Reason { get; set; } = default!;
    public string? Description { get; set; }
    public bool WasAdverseEvent { get; set; } = false;
    public string? AdverseEventDetails { get; set; }
    public DateTime RecordedAt { get; set; }
    public string RecordedBy { get; set; } = default!;
    public DateTime CreatedAt { get; set; }

    // Navigation
    public TreatmentSessionEntity TreatmentSession { get; set; } = default!;
}
