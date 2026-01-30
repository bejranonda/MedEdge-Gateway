using MedEdge.Core.Domain.Entities;
using MedEdge.Core.DTOs;
using MedEdge.FhirApi.Data;
using Microsoft.EntityFrameworkCore;

namespace MedEdge.FhirApi.Services;

/// <summary>
/// Repository for treatment center operations (zones and stations).
/// </summary>
public interface ITreatmentCenterRepository
{
    // Treatment Areas
    Task<List<TreatmentAreaEntity>> GetAllAreasAsync();
    Task<TreatmentAreaEntity?> GetAreaByIdAsync(string id);
    Task<List<TreatmentAreaEntity>> GetActiveAreasAsync();
    Task<TreatmentAreaEntity> CreateAreaAsync(CreateTreatmentAreaRequest request);
    Task<TreatmentAreaEntity?> UpdateAreaAsync(string id, UpdateTreatmentAreaRequest request);
    Task<bool> DeleteAreaAsync(string id);

    // Stations
    Task<List<StationEntity>> GetAllStationsAsync();
    Task<StationEntity?> GetStationByIdAsync(string id);
    Task<List<StationEntity>> GetStationsByAreaAsync(string areaId);
    Task<List<StationEntity>> GetAvailableStationsAsync(string? areaId = null);
    Task<List<StationEntity>> GetStationsByStatusAsync(string status);
    Task<StationEntity> CreateStationAsync(CreateStationRequest request);
    Task<StationEntity?> UpdateStationAsync(string id, UpdateStationRequest request);
    Task<StationEntity?> UpdateStationStatusAsync(string id, UpdateStationStatusRequest request);
    Task<bool> DeleteStationAsync(string id);

    // Dashboard Queries
    Task<List<StationSummaryDto>> GetStationSummariesAsync();
    Task<TreatmentAreaDto?> GetAreaWithStatsAsync(string id);
    Task<List<TreatmentAreaDto>> GetAreasWithStatsAsync();
}

public class TreatmentCenterRepository : ITreatmentCenterRepository
{
    private readonly ApplicationDbContext _context;

    public TreatmentCenterRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // Treatment Areas

    public async Task<List<TreatmentAreaEntity>> GetAllAreasAsync()
    {
        return await _context.TreatmentAreas
            .Include(a => a.SubAreas)
            .Include(a => a.Stations)
            .OrderBy(a => a.DisplayOrder)
            .ToListAsync();
    }

    public async Task<TreatmentAreaEntity?> GetAreaByIdAsync(string id)
    {
        return await _context.TreatmentAreas
            .Include(a => a.ParentArea)
            .Include(a => a.SubAreas)
            .Include(a => a.Stations)
            .ThenInclude(s => s.Configuration)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<List<TreatmentAreaEntity>> GetActiveAreasAsync()
    {
        return await _context.TreatmentAreas
            .Where(a => a.IsActive)
            .OrderBy(a => a.DisplayOrder)
            .ToListAsync();
    }

    public async Task<TreatmentAreaEntity> CreateAreaAsync(CreateTreatmentAreaRequest request)
    {
        var area = new TreatmentAreaEntity
        {
            Id = $"AREA-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            Name = request.Name,
            AreaType = request.AreaType,
            Capacity = request.Capacity,
            ParentAreaId = request.ParentAreaId,
            Description = request.Description,
            DisplayOrder = request.DisplayOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TreatmentAreas.Add(area);
        await _context.SaveChangesAsync();
        return area;
    }

    public async Task<TreatmentAreaEntity?> UpdateAreaAsync(string id, UpdateTreatmentAreaRequest request)
    {
        var area = await _context.TreatmentAreas.FindAsync(id);
        if (area == null) return null;

        area.Name = request.Name;
        area.AreaType = request.AreaType;
        area.Capacity = request.Capacity;
        area.Description = request.Description;
        area.DisplayOrder = request.DisplayOrder;
        area.IsActive = request.IsActive;
        area.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return area;
    }

    public async Task<bool> DeleteAreaAsync(string id)
    {
        var area = await _context.TreatmentAreas.FindAsync(id);
        if (area == null) return false;

        _context.TreatmentAreas.Remove(area);
        await _context.SaveChangesAsync();
        return true;
    }

    // Stations

    public async Task<List<StationEntity>> GetAllStationsAsync()
    {
        return await _context.Stations
            .Include(s => s.Area)
            .Include(s => s.Configuration)
            .Include(s => s.Devices)
            .Include(s => s.Supplies)
            .OrderBy(s => s.AreaId)
            .ThenBy(s => s.DisplayOrder)
            .ToListAsync();
    }

    public async Task<StationEntity?> GetStationByIdAsync(string id)
    {
        return await _context.Stations
            .Include(s => s.Area)
            .Include(s => s.Configuration)
            .Include(s => s.Devices)
            .Include(s => s.Supplies)
            .ThenInclude(ss => ss.Supply)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<StationEntity>> GetStationsByAreaAsync(string areaId)
    {
        return await _context.Stations
            .Include(s => s.Configuration)
            .Include(s => s.Devices)
            .Where(s => s.AreaId == areaId)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync();
    }

    public async Task<List<StationEntity>> GetAvailableStationsAsync(string? areaId = null)
    {
        var query = _context.Stations
            .Include(s => s.Area)
            .Include(s => s.Configuration)
            .Where(s => s.Status == "available");

        if (!string.IsNullOrEmpty(areaId))
        {
            query = query.Where(s => s.AreaId == areaId);
        }

        return await query
            .OrderBy(s => s.Area.DisplayOrder)
            .ThenBy(s => s.DisplayOrder)
            .ToListAsync();
    }

    public async Task<List<StationEntity>> GetStationsByStatusAsync(string status)
    {
        return await _context.Stations
            .Include(s => s.Area)
            .Include(s => s.Configuration)
            .Where(s => s.Status == status)
            .OrderBy(s => s.AreaId)
            .ThenBy(s => s.DisplayOrder)
            .ToListAsync();
    }

    public async Task<StationEntity> CreateStationAsync(CreateStationRequest request)
    {
        var stationId = $"STATION-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        // Create configuration first
        var configuration = new StationConfigurationEntity
        {
            Id = $"CONFIG-{stationId}",
            StationId = stationId,
            HasWaterSupply = false,
            HasPowerBackup = true,
            HasOxygenSupply = false,
            HasVacuumSupply = false,
            MaxDeviceSlots = 5,
            DeviceSlots = new Dictionary<string, string>(),
            TreatmentTypes = "general",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.StationConfigurations.Add(configuration);
        await _context.SaveChangesAsync();

        // Create station
        var station = new StationEntity
        {
            Id = stationId,
            StationNumber = request.StationNumber,
            Status = "available",
            AreaId = request.AreaId,
            PhysicalLocation = request.PhysicalLocation,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Stations.Add(station);
        await _context.SaveChangesAsync();
        return station;
    }

    public async Task<StationEntity?> UpdateStationAsync(string id, UpdateStationRequest request)
    {
        var station = await _context.Stations.FindAsync(id);
        if (station == null) return null;

        station.StationNumber = request.StationNumber;
        station.Status = request.Status;
        station.PhysicalLocation = request.PhysicalLocation;
        station.Notes = request.Notes;
        station.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return station;
    }

    public async Task<StationEntity?> UpdateStationStatusAsync(string id, UpdateStationStatusRequest request)
    {
        var station = await _context.Stations.FindAsync(id);
        if (station == null) return null;

        station.Status = request.Status;
        station.CurrentTreatmentId = request.CurrentTreatmentId;
        station.CurrentPatientId = request.CurrentPatientId;
        station.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return station;
    }

    public async Task<bool> DeleteStationAsync(string id)
    {
        var station = await _context.Stations.FindAsync(id);
        if (station == null) return false;

        _context.Stations.Remove(station);
        await _context.SaveChangesAsync();
        return true;
    }

    // Dashboard Queries

    public async Task<List<StationSummaryDto>> GetStationSummariesAsync()
    {
        return await _context.Stations
            .Join(_context.TreatmentAreas,
                s => s.AreaId,
                a => a.Id,
                (s, a) => new { s, a })
            .Join(_context.Patients,
                x => x.s.CurrentPatientId,
                p => p.Id,
                (x, p) => new { x.s, x.a, Patient = p })
            .DefaultIfEmpty()
            .Select(x => new StationSummaryDto(
                x.s.Id,
                x.s.StationNumber,
                x.s.Status,
                x.s.AreaId,
                x.a.Name,
                x.Patient != null ? $"{x.Patient.GivenName} {x.Patient.FamilyName}" : null
            ))
            .OrderBy(s => s.AreaId)
            .ThenBy(s => s.StationNumber)
            .ToListAsync();
    }

    public async Task<TreatmentAreaDto?> GetAreaWithStatsAsync(string id)
    {
        var area = await _context.TreatmentAreas
            .FirstOrDefaultAsync(a => a.Id == id);

        if (area == null) return null;

        var occupiedCount = await _context.Stations
            .CountAsync(s => s.AreaId == id && s.Status == "occupied");

        var availableCount = await _context.Stations
            .CountAsync(s => s.AreaId == id && s.Status == "available");

        return new TreatmentAreaDto(
            area.Id,
            area.Name,
            area.AreaType,
            area.Capacity,
            area.ParentAreaId,
            area.Description,
            area.DisplayOrder,
            area.IsActive,
            occupiedCount,
            availableCount
        );
    }

    public async Task<List<TreatmentAreaDto>> GetAreasWithStatsAsync()
    {
        var areas = await _context.TreatmentAreas
            .Where(a => a.IsActive)
            .OrderBy(a => a.DisplayOrder)
            .ToListAsync();

        var result = new List<TreatmentAreaDto>();

        foreach (var area in areas)
        {
            var occupiedCount = await _context.Stations
                .CountAsync(s => s.AreaId == area.Id && s.Status == "occupied");

            var availableCount = await _context.Stations
                .CountAsync(s => s.AreaId == area.Id && s.Status == "available");

            result.Add(new TreatmentAreaDto(
                area.Id,
                area.Name,
                area.AreaType,
                area.Capacity,
                area.ParentAreaId,
                area.Description,
                area.DisplayOrder,
                area.IsActive,
                occupiedCount,
                availableCount
            ));
        }

        return result;
    }
}
