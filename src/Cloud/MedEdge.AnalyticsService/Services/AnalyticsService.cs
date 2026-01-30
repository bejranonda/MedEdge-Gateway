using MedEdge.Core.Domain.Entities;
using MedEdge.FhirApi.Data;
using Microsoft.EntityFrameworkCore;

namespace MedEdge.AnalyticsService.Services;

/// <summary>
/// Service for generating treatment analytics and metrics.
/// </summary>
public interface IAnalyticsService
{
    // Daily Aggregation
    Task GenerateDailyMetricsAsync(DateOnly date);

    // Queries
    Task<List<TreatmentMetricsEntity>> GetMetricsByDateRangeAsync(DateOnly start, DateOnly end, string? areaId = null, string? stationId = null);
    Task<TreatmentMetricsEntity?> GetLatestMetricsAsync(string? areaId = null, string? stationId = null);
    Task<List<TreatmentMetricsEntity>> GetAreaComparisonAsync(DateOnly date);
    Task<TreatmentTrendDto> GetTreatmentTrendsAsync(int days);
    Task<StationPerformanceDto> GetStationPerformanceAsync(string stationId, int days);
}

/// <summary>
/// DTO for treatment trends over time.
/// </summary>
public record TreatmentTrendDto(
    List<DateOnly> Dates,
    List<int> TotalTreatments,
    List<int> CompletedTreatments,
    List<double> CompletionRates,
    List<double> AverageDurations
);

/// <summary>
/// DTO for station performance metrics.
/// </summary>
public record StationPerformanceDto(
    string StationId,
    string StationNumber,
    int TotalTreatments,
    int CompletedTreatments,
    double CompletionRate,
    double AverageDurationMinutes,
    double OnTimeStartPercentage,
    int AdverseEventCount,
    List<string> CommonIssues
);

public class AnalyticsService : IAnalyticsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(ApplicationDbContext context, ILogger<AnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task GenerateDailyMetricsAsync(DateOnly date)
    {
        _logger.LogInformation("Generating daily metrics for {Date}", date);

        var startDate = date.ToDateTime(TimeOnly.MinValue);
        var endDate = date.ToDateTime(TimeOnly.MaxValue);

        // Get all areas and stations
        var areas = await _context.TreatmentAreas.Where(a => a.IsActive).ToListAsync();
        var allStations = await _context.Stations.Include(s => s.Area).ToListAsync();

        // Generate metrics for each station
        foreach (var station in allStations)
        {
            await GenerateStationMetricsAsync(station.Id, station.AreaId, date, startDate, endDate);
        }

        // Generate metrics for each area
        foreach (var area in areas)
        {
            await GenerateAreaMetricsAsync(area.Id, date, startDate, endDate);
        }
    }

    private async Task GenerateStationMetricsAsync(string stationId, string areaId, DateOnly date, DateTime startDate, DateTime endDate)
    {
        var sessions = await _context.TreatmentSessions
            .Where(ts => ts.StationId == stationId
                && ts.ScheduledStart >= startDate
                && ts.ScheduledStart <= endDate)
            .Include(ts => ts.Observations)
            .Include(ts => ts.Outcomes)
            .ToListAsync();

        var existing = await _context.TreatmentMetrics
            .FirstOrDefaultAsync(tm => tm.StationId == stationId && tm.MetricDate == date);

        var metrics = existing ?? new TreatmentMetricsEntity
        {
            Id = $"METRIC-{stationId}-{date:yyyyMMdd}",
            StationId = stationId,
            AreaId = areaId,
            MetricDate = date,
            CreatedAt = DateTime.UtcNow
        };

        metrics.TotalTreatments = sessions.Count;
        metrics.CompletedTreatments = sessions.Count(s => s.Status == "completed");
        metrics.InterruptedTreatments = sessions.Count(s => s.Status == "interrupted");
        metrics.CancelledTreatments = sessions.Count(s => s.Status == "cancelled");

        metrics.AverageSessionDurationMinutes = sessions
            .Where(s => s.ActualStart.HasValue && s.ActualEnd.HasValue)
            .Select(s => (s.ActualEnd.Value - s.ActualStart.Value).TotalMinutes)
            .DefaultIfEmpty(0)
            .Average();

        metrics.OnTimeStartPercentage = sessions.Count > 0
            ? sessions.Count(s => !s.ActualStart.HasValue || s.ActualStart <= s.ScheduledStart.AddMinutes(15)) * 100.0 / sessions.Count
            : 0;

        metrics.AdverseEventCount = sessions
            .Sum(s => s.Outcomes.Count(o => o.WasAdverseEvent));

        var adverseEvents = sessions
            .SelectMany(s => s.Outcomes.Where(o => o.WasAdverseEvent))
            .GroupBy(o => o.OutcomeType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToDictionary(g => g.Type, g => g.Count);

        metrics.AdverseEventDetails = System.Text.Json.JsonSerializer.Serialize(adverseEvents);

        // Calculate average vitals from observations
        var vitals = sessions
            .SelectMany(s => s.Observations)
            .Where(o => o.ObservationType == "vital-sign")
            .GroupBy(o => o.CodeDisplay)
            .Select(g => new { Name = g.Key, Value = g.Average(o => o.Value ?? 0) })
            .ToDictionary(g => g.Name, g => g.Value);

        metrics.AverageVitals = vitals;

        metrics.StationUtilizationPercentage = sessions.Count > 0
            ? sessions.Count(s => s.Status == "completed" || s.Status == "in-progress") * 100.0 / sessions.Count
            : 0;

        metrics.UpdatedAt = DateTime.UtcNow;

        if (existing == null)
        {
            _context.TreatmentMetrics.Add(metrics);
        }

        await _context.SaveChangesAsync();
    }

    private async Task GenerateAreaMetricsAsync(string areaId, DateOnly date, DateTime startDate, DateTime endDate)
    {
        var stationMetrics = await _context.TreatmentMetrics
            .Where(tm => tm.AreaId == areaId && tm.MetricDate == date)
            .ToListAsync();

        var existing = await _context.TreatmentMetrics
            .FirstOrDefaultAsync(tm => tm.AreaId == areaId && tm.MetricDate == date && tm.StationId == null);

        var metrics = existing ?? new TreatmentMetricsEntity
        {
            Id = $"METRIC-{areaId}-{date:yyyyMMdd}",
            AreaId = areaId,
            MetricDate = date,
            CreatedAt = DateTime.UtcNow
        };

        if (stationMetrics.Any())
        {
            metrics.TotalTreatments = stationMetrics.Sum(tm => tm.TotalTreatments);
            metrics.CompletedTreatments = stationMetrics.Sum(tm => tm.CompletedTreatments);
            metrics.InterruptedTreatments = stationMetrics.Sum(tm => tm.InterruptedTreatments);
            metrics.CancelledTreatments = stationMetrics.Sum(tm => tm.CancelledTreatments);
            metrics.AverageSessionDurationMinutes = stationMetrics.Average(tm => tm.AverageSessionDurationMinutes);
            metrics.OnTimeStartPercentage = stationMetrics.Average(tm => tm.OnTimeStartPercentage);
            metrics.AdverseEventCount = stationMetrics.Sum(tm => tm.AdverseEventCount);
            metrics.StationUtilizationPercentage = stationMetrics.Average(tm => tm.StationUtilizationPercentage);
        }

        metrics.UpdatedAt = DateTime.UtcNow;

        if (existing == null)
        {
            _context.TreatmentMetrics.Add(metrics);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<TreatmentMetricsEntity>> GetMetricsByDateRangeAsync(DateOnly start, DateOnly end, string? areaId = null, string? stationId = null)
    {
        var query = _context.TreatmentMetrics.AsQueryable();

        if (!string.IsNullOrEmpty(areaId))
            query = query.Where(tm => tm.AreaId == areaId);

        if (!string.IsNullOrEmpty(stationId))
            query = query.Where(tm => tm.StationId == stationId);

        return await query
            .Where(tm => tm.MetricDate >= start && tm.MetricDate <= end)
            .Include(tm => tm.Station)
            .Include(tm => tm.Area)
            .OrderBy(tm => tm.MetricDate)
            .ToListAsync();
    }

    public async Task<TreatmentMetricsEntity?> GetLatestMetricsAsync(string? areaId = null, string? stationId = null)
    {
        var query = _context.TreatmentMetrics.AsQueryable();

        if (!string.IsNullOrEmpty(areaId))
            query = query.Where(tm => tm.AreaId == areaId);

        if (!string.IsNullOrEmpty(stationId))
            query = query.Where(tm => tm.StationId == stationId);

        return await query
            .OrderByDescending(tm => tm.MetricDate)
            .Include(tm => tm.Station)
            .Include(tm => tm.Area)
            .FirstOrDefaultAsync();
    }

    public async Task<List<TreatmentMetricsEntity>> GetAreaComparisonAsync(DateOnly date)
    {
        return await _context.TreatmentMetrics
            .Where(tm => tm.MetricDate == date && tm.AreaId != null)
            .Include(tm => tm.Area)
            .OrderByDescending(tm => tm.CompletedTreatments)
            .ToListAsync();
    }

    public async Task<TreatmentTrendDto> GetTreatmentTrendsAsync(int days)
    {
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = endDate.AddDays(-days + 1);

        var metrics = await _context.TreatmentMetrics
            .Where(tm => tm.MetricDate >= startDate && tm.MetricDate <= endDate)
            .GroupBy(tm => tm.MetricDate)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Date = g.Key,
                Total = g.Sum(tm => tm.TotalTreatments),
                Completed = g.Sum(tm => tm.CompletedTreatments),
                AvgDuration = g.Average(tm => tm.AverageSessionDurationMinutes)
            })
            .ToListAsync();

        return new TreatmentTrendDto(
            metrics.Select(m => m.Date).ToList(),
            metrics.Select(m => m.Total).ToList(),
            metrics.Select(m => m.Completed).ToList(),
            metrics.Select(m => m.Total > 0 ? m.Completed * 100.0 / m.Total : 0).ToList(),
            metrics.Select(m => m.AvgDuration).ToList()
        );
    }

    public async Task<StationPerformanceDto> GetStationPerformanceAsync(string stationId, int days)
    {
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = endDate.AddDays(-days + 1);

        var metrics = await _context.TreatmentMetrics
            .Where(tm => tm.StationId == stationId && tm.MetricDate >= startDate && tm.MetricDate <= endDate)
            .Include(tm => tm.Station)
            .ToListAsync();

        if (!metrics.Any())
        {
            var station = await _context.Stations.FindAsync(stationId);
            return new StationPerformanceDto(
                stationId,
                station?.StationNumber ?? "Unknown",
                0, 0, 0, 0, 0, 0, new List<string>()
            );
        }

        var totalTreatments = metrics.Sum(m => m.TotalTreatments);
        var completedTreatments = metrics.Sum(m => m.CompletedTreatments);
        var completionRate = totalTreatments > 0 ? completedTreatments * 100.0 / totalTreatments : 0;
        var avgDuration = metrics.Average(m => m.AverageSessionDurationMinutes);
        var onTimeStart = metrics.Average(m => m.OnTimeStartPercentage);
        var adverseEvents = metrics.Sum(m => m.AdverseEventCount);

        // Parse common issues from adverse event details
        var commonIssues = new List<string>();
        foreach (var metric in metrics.Where(m => !string.IsNullOrEmpty(m.AdverseEventDetails)))
        {
            try
            {
                var events = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(metric.AdverseEventDetails);
                if (events != null)
                {
                    commonIssues.AddRange(events.Keys);
                }
            }
            catch { }
        }

        return new StationPerformanceDto(
            stationId,
            metrics.First().Station?.StationNumber ?? "Unknown",
            totalTreatments,
            completedTreatments,
            completionRate,
            avgDuration,
            onTimeStart,
            adverseEvents,
            commonIssues.Distinct().ToList()
        );
    }
}
