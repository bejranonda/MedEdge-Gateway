namespace MedEdge.Core.Domain.Entities;

/// <summary>
/// Aggregated treatment metrics for a station, area, or entire center.
/// Calculated daily for analytics and reporting.
/// </summary>
public class TreatmentMetricsEntity
{
    public string Id { get; set; } = default!;
    public string? StationId { get; set; }
    public string? AreaId { get; set; }
    public DateOnly MetricDate { get; set; }
    public int TotalTreatments { get; set; }
    public int CompletedTreatments { get; set; }
    public int InterruptedTreatments { get; set; }
    public int CancelledTreatments { get; set; }
    public double AverageSessionDurationMinutes { get; set; }
    public double OnTimeStartPercentage { get; set; }
    public int AdverseEventCount { get; set; }
    public string AdverseEventDetails { get; set; } = "{}"; // JSON stored as string
    public Dictionary<string, double> AverageVitals { get; set; } = new();
    public double StationUtilizationPercentage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public StationEntity? Station { get; set; }
    public TreatmentAreaEntity? Area { get; set; }
}
