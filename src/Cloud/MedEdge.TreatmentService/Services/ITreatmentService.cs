using MedEdge.Core.Domain.Entities;
using MedEdge.Core.DTOs;

namespace MedEdge.TreatmentService.Services;

/// <summary>
/// Service interface for treatment session management.
/// </summary>
public interface ITreatmentSessionService
{
    // Session CRUD
    Task<List<TreatmentSessionEntity>> GetAllSessionsAsync();
    Task<TreatmentSessionEntity?> GetSessionByIdAsync(string id);
    Task<List<TreatmentSessionEntity>> GetSessionsByPatientAsync(string patientId);
    Task<List<TreatmentSessionEntity>> GetSessionsByStationAsync(string stationId);
    Task<List<TreatmentSessionEntity>> GetActiveSessionsAsync();
    Task<List<TreatmentSessionEntity>> GetSessionsByStatusAsync(string status);
    Task<List<TreatmentSessionEntity>> GetSessionsByDateRangeAsync(DateTime start, DateTime end);

    // Session Lifecycle
    Task<TreatmentSessionEntity> ScheduleSessionAsync(CreateTreatmentSessionRequest request);
    Task<TreatmentSessionEntity?> StartSessionAsync(string id, StartTreatmentSessionRequest request);
    Task<TreatmentSessionEntity?> UpdateSessionAsync(string id, UpdateTreatmentSessionRequest request);
    Task<TreatmentSessionEntity?> InterruptSessionAsync(string id, InterruptTreatmentSessionRequest request);
    Task<TreatmentSessionEntity?> CompleteSessionAsync(string id, CompleteTreatmentSessionRequest request);
    Task<bool> CancelSessionAsync(string id);

    // Session Phases
    Task<TreatmentPhaseEntity?> AddPhaseAsync(string sessionId, string phaseName, int displayOrder);
    Task<TreatmentPhaseEntity?> UpdatePhaseAsync(string phaseId, string status, DateTime? startedAt, DateTime? completedAt);
    Task<TreatmentPhaseEntity?> GetCurrentPhaseAsync(string sessionId);

    // Session Observations
    Task<TreatmentObservationEntity> AddObservationAsync(CreateTreatmentObservationRequest request);
    Task<List<TreatmentObservationEntity>> GetObservationsAsync(string sessionId);

    // Station Assignment
    Task<bool> AssignStationAsync(string sessionId, string stationId);
    Task<bool> ReleaseStationAsync(string sessionId);
    Task<List<StationEntity>> GetAvailableStationsAsync(DateTime? forDate = null);
}

/// <summary>
/// Implementation of treatment session management service.
/// </summary>
public class TreatmentSessionService : ITreatmentSessionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TreatmentSessionService> _logger;

    public TreatmentSessionService(ApplicationDbContext context, ILogger<TreatmentSessionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Session CRUD

    public async Task<List<TreatmentSessionEntity>> GetAllSessionsAsync()
    {
        return await _context.TreatmentSessions
            .Include(ts => ts.Patient)
            .Include(ts => ts.Station)
            .Include(ts => ts.Phases)
            .OrderByDescending(ts => ts.ScheduledStart)
            .ToListAsync();
    }

    public async Task<TreatmentSessionEntity?> GetSessionByIdAsync(string id)
    {
        return await _context.TreatmentSessions
            .Include(ts => ts.Patient)
            .Include(ts => ts.Station)
            .Include(ts => ts.Phases)
            .Include(ts => ts.Observations)
            .Include(ts => ts.Outcomes)
            .FirstOrDefaultAsync(ts => ts.Id == id);
    }

    public async Task<List<TreatmentSessionEntity>> GetSessionsByPatientAsync(string patientId)
    {
        return await _context.TreatmentSessions
            .Include(ts => ts.Station)
            .Include(ts => ts.Phases)
            .Where(ts => ts.PatientId == patientId)
            .OrderByDescending(ts => ts.ScheduledStart)
            .ToListAsync();
    }

    public async Task<List<TreatmentSessionEntity>> GetSessionsByStationAsync(string stationId)
    {
        return await _context.TreatmentSessions
            .Include(ts => ts.Patient)
            .Include(ts => ts.Phases)
            .Where(ts => ts.StationId == stationId)
            .OrderByDescending(ts => ts.ScheduledStart)
            .ToListAsync();
    }

    public async Task<List<TreatmentSessionEntity>> GetActiveSessionsAsync()
    {
        return await _context.TreatmentSessions
            .Include(ts => ts.Patient)
            .Include(ts => ts.Station)
            .Include(ts => ts.Phases)
            .Where(ts => ts.Status == "in-progress")
            .OrderBy(ts => ts.ScheduledStart)
            .ToListAsync();
    }

    public async Task<List<TreatmentSessionEntity>> GetSessionsByStatusAsync(string status)
    {
        return await _context.TreatmentSessions
            .Include(ts => ts.Patient)
            .Include(ts => ts.Station)
            .Where(ts => ts.Status == status)
            .OrderByDescending(ts => ts.ScheduledStart)
            .ToListAsync();
    }

    public async Task<List<TreatmentSessionEntity>> GetSessionsByDateRangeAsync(DateTime start, DateTime end)
    {
        return await _context.TreatmentSessions
            .Include(ts => ts.Patient)
            .Include(ts => ts.Station)
            .Where(ts => ts.ScheduledStart >= start && ts.ScheduledStart <= end)
            .OrderByDescending(ts => ts.ScheduledStart)
            .ToListAsync();
    }

    // Session Lifecycle

    public async Task<TreatmentSessionEntity> ScheduleSessionAsync(CreateTreatmentSessionRequest request)
    {
        // Verify station availability
        var station = await _context.Stations.FindAsync(request.StationId);
        if (station == null)
            throw new InvalidOperationException($"Station {request.StationId} not found");

        if (station.Status != "available")
            throw new InvalidOperationException($"Station {request.StationId} is not available");

        // Create session
        var session = new TreatmentSessionEntity
        {
            Id = $"SESSION-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            PatientId = request.PatientId,
            StationId = request.StationId,
            ScheduledStart = request.ScheduledStart,
            TreatmentType = request.TreatmentType,
            Status = "scheduled",
            PrescribingPhysician = request.PrescribingPhysician,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Create default phases
        var phases = new List<TreatmentPhaseEntity>
        {
            new()
            {
                Id = $"PHASE-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                TreatmentSessionId = session.Id,
                PhaseName = "setup",
                Status = "pending",
                DisplayOrder = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = $"PHASE-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                TreatmentSessionId = session.Id,
                PhaseName = "treatment",
                Status = "pending",
                DisplayOrder = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = $"PHASE-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                TreatmentSessionId = session.Id,
                PhaseName = "cleanup",
                Status = "pending",
                DisplayOrder = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _context.TreatmentSessions.Add(session);
        _context.TreatmentPhases.AddRange(phases);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Treatment session {SessionId} scheduled for patient {PatientId} at station {StationId}",
            session.Id, request.PatientId, request.StationId);

        return session;
    }

    public async Task<TreatmentSessionEntity?> StartSessionAsync(string id, StartTreatmentSessionRequest request)
    {
        var session = await _context.TreatmentSessions
            .Include(ts => ts.Station)
            .Include(ts => ts.Phases)
            .FirstOrDefaultAsync(ts => ts.Id == id);

        if (session == null) return null;

        // Update session
        session.ActualStart = request.ActualStart;
        session.Status = "in-progress";
        session.UpdatedAt = DateTime.UtcNow;

        // Update station status
        session.Station.Status = "occupied";
        session.Station.CurrentPatientId = session.PatientId;
        session.Station.CurrentTreatmentId = session.Id;
        session.Station.UpdatedAt = DateTime.UtcNow;

        // Start first phase
        var firstPhase = session.Phases.OrderBy(p => p.DisplayOrder).FirstOrDefault();
        if (firstPhase != null)
        {
            firstPhase.Status = "in-progress";
            firstPhase.StartedAt = request.ActualStart;
            firstPhase.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Treatment session {SessionId} started", id);
        return session;
    }

    public async Task<TreatmentSessionEntity?> UpdateSessionAsync(string id, UpdateTreatmentSessionRequest request)
    {
        var session = await _context.TreatmentSessions.FindAsync(id);
        if (session == null) return null;

        session.Status = request.Status;
        session.Notes = request.Notes;
        session.UpdatedAt = DateTime.UtcNow;

        // Update phase if specified
        if (!string.IsNullOrEmpty(request.CurrentPhaseId))
        {
            var currentPhase = await _context.TreatmentPhases.FindAsync(request.CurrentPhaseId);
            if (currentPhase != null && currentPhase.TreatmentSessionId == id)
            {
                currentPhase.Status = "in-progress";
                if (!currentPhase.StartedAt.HasValue)
                {
                    currentPhase.StartedAt = DateTime.UtcNow;
                }
                currentPhase.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
        return session;
    }

    public async Task<TreatmentSessionEntity?> InterruptSessionAsync(string id, InterruptTreatmentSessionRequest request)
    {
        var session = await _context.TreatmentSessions
            .Include(ts => ts.Station)
            .FirstOrDefaultAsync(ts => ts.Id == id);

        if (session == null) return null;

        session.Status = "interrupted";
        session.InterruptionReason = request.InterruptionReason;
        session.Notes = request.Notes;
        session.ActualEnd = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;

        // Release station
        session.Station.Status = "available";
        session.Station.CurrentPatientId = null;
        session.Station.CurrentTreatmentId = null;
        session.Station.UpdatedAt = DateTime.UtcNow;

        // Complete current phase
        var currentPhase = await _context.TreatmentPhases
            .Where(p => p.TreatmentSessionId == id && p.Status == "in-progress")
            .FirstOrDefaultAsync();

        if (currentPhase != null)
        {
            currentPhase.Status = "completed";
            currentPhase.CompletedAt = DateTime.UtcNow;
            currentPhase.DurationMinutes = (int)(currentPhase.CompletedAt - currentPhase.StartedAt)?.TotalMinutes;
            currentPhase.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogWarning("Treatment session {SessionId} interrupted: {Reason}", id, request.InterruptionReason);
        return session;
    }

    public async Task<TreatmentSessionEntity?> CompleteSessionAsync(string id, CompleteTreatmentSessionRequest request)
    {
        var session = await _context.TreatmentSessions
            .Include(ts => ts.Station)
            .Include(ts => ts.Phases)
            .FirstOrDefaultAsync(ts => ts.Id == id);

        if (session == null) return null;

        session.Status = "completed";
        session.ActualEnd = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;

        // Release station
        session.Station.Status = "available";
        session.Station.CurrentPatientId = null;
        session.Station.CurrentTreatmentId = null;
        session.Station.UpdatedAt = DateTime.UtcNow;

        // Complete all pending phases
        foreach (var phase in session.Phases.Where(p => p.Status == "in-progress" || p.Status == "pending"))
        {
            phase.Status = "completed";
            phase.CompletedAt = DateTime.UtcNow;
            if (phase.StartedAt.HasValue)
            {
                phase.DurationMinutes = (int)(phase.CompletedAt - phase.StartedAt).Value.TotalMinutes;
            }
            phase.UpdatedAt = DateTime.UtcNow;
        }

        // Create outcome record
        var outcome = new TreatmentOutcomeEntity
        {
            Id = $"OUTCOME-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            TreatmentSessionId = id,
            OutcomeType = request.OutcomeType,
            Reason = request.Reason,
            Description = request.Description,
            WasAdverseEvent = request.WasAdverseEvent,
            AdverseEventDetails = request.AdverseEventDetails,
            RecordedAt = DateTime.UtcNow,
            RecordedBy = "System",
            CreatedAt = DateTime.UtcNow
        };

        _context.TreatmentOutcomes.Add(outcome);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Treatment session {SessionId} completed with outcome: {Outcome}", id, request.OutcomeType);
        return session;
    }

    public async Task<bool> CancelSessionAsync(string id)
    {
        var session = await _context.TreatmentSessions
            .Include(ts => ts.Station)
            .FirstOrDefaultAsync(ts => ts.Id == id);

        if (session == null) return false;

        if (session.Status == "in-progress")
        {
            throw new InvalidOperationException("Cannot cancel an in-progress session. Use interrupt instead.");
        }

        session.Status = "cancelled";
        session.UpdatedAt = DateTime.UtcNow;

        // Release station if assigned
        if (session.Station.CurrentTreatmentId == id)
        {
            session.Station.CurrentTreatmentId = null;
            session.Station.CurrentPatientId = null;
            session.Station.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    // Session Phases

    public async Task<TreatmentPhaseEntity?> AddPhaseAsync(string sessionId, string phaseName, int displayOrder)
    {
        var session = await _context.TreatmentSessions.FindAsync(sessionId);
        if (session == null) return null;

        var phase = new TreatmentPhaseEntity
        {
            Id = $"PHASE-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            TreatmentSessionId = sessionId,
            PhaseName = phaseName,
            Status = "pending",
            DisplayOrder = displayOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TreatmentPhases.Add(phase);
        await _context.SaveChangesAsync();
        return phase;
    }

    public async Task<TreatmentPhaseEntity?> UpdatePhaseAsync(string phaseId, string status, DateTime? startedAt, DateTime? completedAt)
    {
        var phase = await _context.TreatmentPhases.FindAsync(phaseId);
        if (phase == null) return null;

        phase.Status = status;
        phase.StartedAt = startedAt ?? phase.StartedAt;
        phase.CompletedAt = completedAt ?? phase.CompletedAt;

        if (phase.StartedAt.HasValue && phase.CompletedAt.HasValue)
        {
            phase.DurationMinutes = (int)(phase.CompletedAt - phase.StartedAt).Value.TotalMinutes;
        }

        phase.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return phase;
    }

    public async Task<TreatmentPhaseEntity?> GetCurrentPhaseAsync(string sessionId)
    {
        return await _context.TreatmentPhases
            .Where(p => p.TreatmentSessionId == sessionId && p.Status == "in-progress")
            .FirstOrDefaultAsync();
    }

    // Session Observations

    public async Task<TreatmentObservationEntity> AddObservationAsync(CreateTreatmentObservationRequest request)
    {
        var observation = new TreatmentObservationEntity
        {
            Id = $"TOBS-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            TreatmentSessionId = request.TreatmentSessionId,
            ObservationType = request.ObservationType,
            Code = request.Code,
            CodeDisplay = request.CodeDisplay,
            Value = request.Value,
            Unit = request.Unit,
            Notes = request.Notes,
            RecordedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.TreatmentObservations.Add(observation);
        await _context.SaveChangesAsync();
        return observation;
    }

    public async Task<List<TreatmentObservationEntity>> GetObservationsAsync(string sessionId)
    {
        return await _context.TreatmentObservations
            .Where(o => o.TreatmentSessionId == sessionId)
            .OrderByDescending(o => o.RecordedAt)
            .ToListAsync();
    }

    // Station Assignment

    public async Task<bool> AssignStationAsync(string sessionId, string stationId)
    {
        var session = await _context.TreatmentSessions
            .Include(ts => ts.Station)
            .FirstOrDefaultAsync(ts => ts.Id == sessionId);

        if (session == null) return false;

        // Check if new station is available
        var newStation = await _context.Stations.FindAsync(stationId);
        if (newStation == null || newStation.Status != "available")
            return false;

        // Release old station
        if (session.Station != null)
        {
            session.Station.CurrentTreatmentId = null;
            session.Station.CurrentPatientId = null;
        }

        // Assign new station
        session.StationId = stationId;
        session.UpdatedAt = DateTime.UtcNow;

        newStation.CurrentTreatmentId = sessionId;
        newStation.CurrentPatientId = session.PatientId;
        newStation.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReleaseStationAsync(string sessionId)
    {
        var session = await _context.TreatmentSessions
            .Include(ts => ts.Station)
            .FirstOrDefaultAsync(ts => ts.Id == sessionId);

        if (session == null || session.Station == null) return false;

        session.Station.CurrentTreatmentId = null;
        session.Station.CurrentPatientId = null;
        session.Station.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<StationEntity>> GetAvailableStationsAsync(DateTime? forDate = null)
    {
        return await _context.Stations
            .Where(s => s.Status == "available")
            .Include(s => s.Area)
            .Include(s => s.Configuration)
            .OrderBy(s => s.Area.DisplayOrder)
            .ThenBy(s => s.DisplayOrder)
            .ToListAsync();
    }
}
