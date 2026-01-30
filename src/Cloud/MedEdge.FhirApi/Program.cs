using Hl7.Fhir.Model;
using MedEdge.Core.DTOs;
using MedEdge.FhirApi.Data;
using MedEdge.FhirApi.Hubs;
using MedEdge.FhirApi.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/medEdge-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=medEdge.db";
    options.UseSqlite(connectionString);
});

builder.Services.AddScoped<IFhirRepository, FhirRepository>();
builder.Services.AddScoped<IFhirMappingService, FhirMappingService>();
builder.Services.AddScoped<ITreatmentCenterRepository, TreatmentCenterRepository>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(origin => true) // More robust for localhost/docker
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // Often needed for SignalR
    });
});

builder.Services.AddSignalR();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Apply migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseSerilogRequestLogging();

// Map SignalR hub
app.MapHub<TelemetryHub>("/hubs/telemetry");

// FHIR Endpoints

// GET /fhir/Patient
app.MapGet("/fhir/Patient", async (IFhirRepository repo, IFhirMappingService mapper) =>
{
    var patients = await repo.GetAllPatientsAsync();
    var bundle = new Bundle
    {
        Type = Bundle.BundleType.Searchset,
        Total = patients.Count,
        Entry = patients.Select(p => new Bundle.EntryComponent
        {
            Resource = mapper.MapPatientEntityToFhir(p),
            Search = new Bundle.SearchComponent { Mode = Bundle.SearchEntryMode.Match }
        }).ToList()
    };
    return Results.Ok(bundle);
})
.WithName("GetPatients")
;

// GET /fhir/Patient/{id}
app.MapGet("/fhir/Patient/{id}", async (string id, IFhirRepository repo, IFhirMappingService mapper) =>
{
    var patient = await repo.GetPatientByIdAsync(id);
    if (patient == null)
        return Results.NotFound(new OperationOutcome
        {
            Issue = new List<OperationOutcome.IssueComponent>
            {
                new()
                {
                    Severity = OperationOutcome.IssueSeverity.Error,
                    Code = OperationOutcome.IssueType.NotFound,
                    Diagnostics = $"Patient {id} not found"
                }
            }
        });

    return Results.Ok(mapper.MapPatientEntityToFhir(patient));
})
.WithName("GetPatientById")
;

// GET /fhir/Device
app.MapGet("/fhir/Device", async (IFhirRepository repo, IFhirMappingService mapper) =>
{
    var devices = await repo.GetAllDevicesAsync();
    var bundle = new Bundle
    {
        Type = Bundle.BundleType.Searchset,
        Total = devices.Count,
        Entry = devices.Select(d => new Bundle.EntryComponent
        {
            Resource = mapper.MapDeviceEntityToFhir(d),
            Search = new Bundle.SearchComponent { Mode = Bundle.SearchEntryMode.Match }
        }).ToList()
    };
    return Results.Ok(bundle);
})
.WithName("GetDevices")
;

// GET /fhir/Device/{id}
app.MapGet("/fhir/Device/{id}", async (string id, IFhirRepository repo, IFhirMappingService mapper) =>
{
    var device = await repo.GetDeviceByIdAsync(id);
    if (device == null)
        return Results.NotFound();

    return Results.Ok(mapper.MapDeviceEntityToFhir(device));
})
.WithName("GetDeviceById")
;

// POST /fhir/Observation
app.MapPost("/fhir/Observation", async (CreateObservationRequest request, IFhirRepository repo, IFhirMappingService mapper) =>
{
    var observation = await repo.CreateObservationAsync(request);
    return Results.Created($"/fhir/Observation/{observation.Id}", mapper.MapObservationEntityToFhir(observation));
})
.WithName("CreateObservation")
;

// GET /fhir/Observation
app.MapGet("/fhir/Observation", async (
    string? patient,
    string? device,
    string? code,
    IFhirRepository repo,
    IFhirMappingService mapper) =>
{
    List<MedEdge.Core.Domain.Entities.FhirObservationEntity> observations;

    if (!string.IsNullOrEmpty(patient))
        observations = await repo.GetObservationsByPatientAsync(patient);
    else if (!string.IsNullOrEmpty(device))
        observations = await repo.GetObservationsByDeviceAsync(device);
    else if (!string.IsNullOrEmpty(code))
        observations = await repo.GetObservationsByCodeAsync(code);
    else
        observations = await repo.GetAllObservationsAsync();

    var bundle = new Bundle
    {
        Type = Bundle.BundleType.Searchset,
        Total = observations.Count,
        Entry = observations.Select(o => new Bundle.EntryComponent
        {
            Resource = mapper.MapObservationEntityToFhir(o),
            Search = new Bundle.SearchComponent { Mode = Bundle.SearchEntryMode.Match }
        }).ToList()
    };

    return Results.Ok(bundle);
})
.WithName("GetObservations")
;

// GET /fhir/Observation/{id}
app.MapGet("/fhir/Observation/{id}", async (string id, IFhirRepository repo, IFhirMappingService mapper) =>
{
    var observation = await repo.GetObservationByIdAsync(id);
    if (observation == null)
        return Results.NotFound();

    return Results.Ok(mapper.MapObservationEntityToFhir(observation));
})
.WithName("GetObservationById")
;

// Dashboard API Endpoints

// GET /api/devices - Get all devices with status for dashboard
app.MapGet("/api/devices", async (IFhirRepository repo, ILogger<Program> logger) =>
{
    try 
    {
        var devices = await repo.GetAllDevicesAsync();
        if (devices == null)
        {
            logger.LogWarning("No devices found in database");
            return Results.Ok(new List<object>());
        }

        var deviceStatuses = devices.Select(d => new
        {
            DeviceId = d.DeviceId ?? "Unknown",
            Type = d.Model ?? "Dialog+",
            Manufacturer = d.Manufacturer ?? "B. Braun",
            Model = d.Model ?? "Unknown",
            SerialNumber = d.SerialNumber ?? "SN-000",
            CurrentPatientId = d.AssignedPatientId ?? "N/A",
            IsOnline = true, 
            LastTelemetryTime = DateTime.UtcNow.AddSeconds(-30),
            ActiveAlarmCount = 0,
            RiskLevel = "Low"
        }).ToList();

        return Results.Ok(deviceStatuses);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error fetching device statuses for dashboard");
        return Results.Problem(
            detail: ex.Message,
            title: "Internal Server Error",
            statusCode: 500);
    }
})
.WithName("GetDevicesStatus")
;

// POST /api/devices/{deviceId}/emergency-stop - Send emergency stop command
app.MapPost("/api/devices/{deviceId}/emergency-stop", async (
    string deviceId,
    IHubContext<TelemetryHub> hubContext,
    ILogger<Program> logger) =>
{
    logger.LogWarning($"Emergency stop requested for device {deviceId}");

    // Broadcast emergency stop command to all subscribers
    await hubContext.Clients.Group(deviceId).SendAsync("EmergencyStop", new
    {
        deviceId,
        timestamp = DateTime.UtcNow,
        command = "STOP"
    });

    return Results.Ok(new
    {
        success = true,
        message = $"Emergency stop sent to {deviceId}",
        timestamp = DateTime.UtcNow
    });
})
.WithName("EmergencyStopDevice")
;

// POST /api/devices/{deviceId}/anomaly/hypotension - Trigger hypotension for demo
app.MapPost("/api/devices/{deviceId}/anomaly/hypotension", async (
    string deviceId,
    IHubContext<TelemetryHub> hubContext,
    ILogger<Program> logger) =>
{
    logger.LogInformation($"Anomaly injection: Hypotension for device {deviceId}");

    // Send hypotension event
    await hubContext.Clients.Group(deviceId).SendAsync("AnomalyInjected", new
    {
        deviceId,
        anomalyType = "Hypotension",
        timestamp = DateTime.UtcNow
    });

    return Results.Ok(new
    {
        success = true,
        message = "Hypotension anomaly injected",
        timestamp = DateTime.UtcNow
    });
})
.WithName("InjectHypotensionAnomaly")
;

// Treatment Center API Endpoints

// GET /api/areas - Get all treatment areas with statistics
app.MapGet("/api/areas", async (ITreatmentCenterRepository repo) =>
{
    var areas = await repo.GetAreasWithStatsAsync();
    return Results.Ok(areas);
})
.WithName("GetAreas")
.WithOpenApi();

// GET /api/areas/{id} - Get specific area with statistics
app.MapGet("/api/areas/{id}", async (string id, ITreatmentCenterRepository repo) =>
{
    var area = await repo.GetAreaWithStatsAsync(id);
    if (area == null)
        return Results.NotFound();
    return Results.Ok(area);
})
.WithName("GetAreaById")
.WithOpenApi();

// POST /api/areas - Create new treatment area
app.MapPost("/api/areas", async (CreateTreatmentAreaRequest request, ITreatmentCenterRepository repo) =>
{
    var area = await repo.CreateAreaAsync(request);
    return Results.Created($"/api/areas/{area.Id}", area);
})
.WithName("CreateArea")
.WithOpenApi();

// PUT /api/areas/{id} - Update treatment area
app.MapPut("/api/areas/{id}", async (string id, UpdateTreatmentAreaRequest request, ITreatmentCenterRepository repo) =>
{
    var area = await repo.UpdateAreaAsync(id, request);
    if (area == null)
        return Results.NotFound();
    return Results.Ok(area);
})
.WithName("UpdateArea")
.WithOpenApi();

// DELETE /api/areas/{id} - Delete treatment area
app.MapDelete("/api/areas/{id}", async (string id, ITreatmentCenterRepository repo) =>
{
    var success = await repo.DeleteAreaAsync(id);
    if (!success)
        return Results.NotFound();
    return Results.NoContent();
})
.WithName("DeleteArea")
.WithOpenApi();

// GET /api/stations - Get all stations
app.MapGet("/api/stations", async (ITreatmentCenterRepository repo) =>
{
    var stations = await repo.GetAllStationsAsync();
    var dtos = stations.Select(s => new StationDto(
        s.Id,
        s.StationNumber,
        s.Status,
        s.AreaId,
        s.Area.Name,
        s.CurrentTreatmentId,
        s.CurrentPatientId,
        s.PhysicalLocation,
        s.Devices.Count,
        new StationConfigurationDto(
            s.Configuration.Id,
            s.Configuration.StationId,
            s.Configuration.HasWaterSupply,
            s.Configuration.HasPowerBackup,
            s.Configuration.HasOxygenSupply,
            s.Configuration.HasVacuumSupply,
            s.Configuration.MaxDeviceSlots,
            s.Configuration.DeviceSlots,
            s.Configuration.TreatmentTypes
        )
    ));
    return Results.Ok(dtos);
})
.WithName("GetStations")
.WithOpenApi();

// GET /api/stations/summary - Get station summaries for dashboard
app.MapGet("/api/stations/summary", async (ITreatmentCenterRepository repo) =>
{
    var summaries = await repo.GetStationSummariesAsync();
    return Results.Ok(summaries);
})
.WithName("GetStationSummaries")
.WithOpenApi();

// GET /api/stations/{id} - Get specific station
app.MapGet("/api/stations/{id}", async (string id, ITreatmentCenterRepository repo) =>
{
    var station = await repo.GetStationByIdAsync(id);
    if (station == null)
        return Results.NotFound();

    var dto = new StationDto(
        station.Id,
        station.StationNumber,
        station.Status,
        station.AreaId,
        station.Area.Name,
        station.CurrentTreatmentId,
        station.CurrentPatientId,
        station.PhysicalLocation,
        station.Devices.Count,
        new StationConfigurationDto(
            station.Configuration.Id,
            station.Configuration.StationId,
            station.Configuration.HasWaterSupply,
            station.Configuration.HasPowerBackup,
            station.Configuration.HasOxygenSupply,
            station.Configuration.HasVacuumSupply,
            station.Configuration.MaxDeviceSlots,
            station.Configuration.DeviceSlots,
            station.Configuration.TreatmentTypes
        )
    );
    return Results.Ok(dto);
})
.WithName("GetStationById")
.WithOpenApi();

// GET /api/stations/area/{areaId} - Get stations by area
app.MapGet("/api/stations/area/{areaId}", async (string areaId, ITreatmentCenterRepository repo) =>
{
    var stations = await repo.GetStationsByAreaAsync(areaId);
    var dtos = stations.Select(s => new StationDto(
        s.Id,
        s.StationNumber,
        s.Status,
        s.AreaId,
        s.Area.Name,
        s.CurrentTreatmentId,
        s.CurrentPatientId,
        s.PhysicalLocation,
        s.Devices.Count,
        new StationConfigurationDto(
            s.Configuration.Id,
            s.Configuration.StationId,
            s.Configuration.HasWaterSupply,
            s.Configuration.HasPowerBackup,
            s.Configuration.HasOxygenSupply,
            s.Configuration.HasVacuumSupply,
            s.Configuration.MaxDeviceSlots,
            s.Configuration.DeviceSlots,
            s.Configuration.TreatmentTypes
        )
    ));
    return Results.Ok(dtos);
})
.WithName("GetStationsByArea")
.WithOpenApi();

// GET /api/stations/available - Get available stations
app.MapGet("/api/stations/available", async (string? areaId, ITreatmentCenterRepository repo) =>
{
    var stations = await repo.GetAvailableStationsAsync(areaId);
    var dtos = stations.Select(s => new StationDto(
        s.Id,
        s.StationNumber,
        s.Status,
        s.AreaId,
        s.Area.Name,
        s.CurrentTreatmentId,
        s.CurrentPatientId,
        s.PhysicalLocation,
        s.Devices.Count,
        new StationConfigurationDto(
            s.Configuration.Id,
            s.Configuration.StationId,
            s.Configuration.HasWaterSupply,
            s.Configuration.HasPowerBackup,
            s.Configuration.HasOxygenSupply,
            s.Configuration.HasVacuumSupply,
            s.Configuration.MaxDeviceSlots,
            s.Configuration.DeviceSlots,
            s.Configuration.TreatmentTypes
        )
    ));
    return Results.Ok(dtos);
})
.WithName("GetAvailableStations")
.WithOpenApi();

// POST /api/stations - Create new station
app.MapPost("/api/stations", async (CreateStationRequest request, ITreatmentCenterRepository repo) =>
{
    var station = await repo.CreateStationAsync(request);
    return Results.Created($"/api/stations/{station.Id}", station);
})
.WithName("CreateStation")
.WithOpenApi();

// PUT /api/stations/{id} - Update station
app.MapPut("/api/stations/{id}", async (string id, UpdateStationRequest request, ITreatmentCenterRepository repo) =>
{
    var station = await repo.UpdateStationAsync(id, request);
    if (station == null)
        return Results.NotFound();
    return Results.Ok(station);
})
.WithName("UpdateStation")
.WithOpenApi();

// PATCH /api/stations/{id}/status - Update station status
app.MapPatch("/api/stations/{id}/status", async (
    string id,
    UpdateStationStatusRequest request,
    ITreatmentCenterRepository repo,
    IHubContext<TelemetryHub> hubContext,
    ILogger<Program> logger) =>
{
    var station = await repo.UpdateStationStatusAsync(id, request);
    if (station == null)
        return Results.NotFound();

    // Broadcast status change to all connected clients
    await hubContext.Clients.Group("stations").SendAsync("StationStatusChanged", new
    {
        stationId = id,
        status = request.Status,
        timestamp = DateTime.UtcNow
    });

    logger.LogInformation($"Station {id} status changed to {request.Status}");
    return Results.Ok(station);
})
.WithName("UpdateStationStatus")
.WithOpenApi();

// DELETE /api/stations/{id} - Delete station
app.MapDelete("/api/stations/{id}", async (string id, ITreatmentCenterRepository repo) =>
{
    var success = await repo.DeleteStationAsync(id);
    if (!success)
        return Results.NotFound();
    return Results.NoContent();
})
.WithName("DeleteStation")
.WithOpenApi();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("HealthCheck")
    ;

app.Run();
