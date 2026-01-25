using Hl7.Fhir.Model;
using MedEdge.Core.DTOs;
using MedEdge.FhirApi.Data;
using MedEdge.FhirApi.Hubs;
using MedEdge.FhirApi.Services;
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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddSignalR();
builder.Services.AddOpenApi();
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
    app.MapOpenApi();
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
        Total = (uint)patients.Count,
        Entry = patients.Select(p => new Bundle.EntryComponent
        {
            Resource = mapper.MapPatientEntityToFhir(p),
            Search = new Bundle.SearchComponent { Mode = Bundle.SearchEntryMode.Match }
        }).ToList()
    };
    return Results.Ok(bundle);
})
.WithName("GetPatients")
.WithOpenApi();

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
.WithOpenApi();

// GET /fhir/Device
app.MapGet("/fhir/Device", async (IFhirRepository repo, IFhirMappingService mapper) =>
{
    var devices = await repo.GetAllDevicesAsync();
    var bundle = new Bundle
    {
        Type = Bundle.BundleType.Searchset,
        Total = (uint)devices.Count,
        Entry = devices.Select(d => new Bundle.EntryComponent
        {
            Resource = mapper.MapDeviceEntityToFhir(d),
            Search = new Bundle.SearchComponent { Mode = Bundle.SearchEntryMode.Match }
        }).ToList()
    };
    return Results.Ok(bundle);
})
.WithName("GetDevices")
.WithOpenApi();

// GET /fhir/Device/{id}
app.MapGet("/fhir/Device/{id}", async (string id, IFhirRepository repo, IFhirMappingService mapper) =>
{
    var device = await repo.GetDeviceByIdAsync(id);
    if (device == null)
        return Results.NotFound();

    return Results.Ok(mapper.MapDeviceEntityToFhir(device));
})
.WithName("GetDeviceById")
.WithOpenApi();

// POST /fhir/Observation
app.MapPost("/fhir/Observation", async (CreateObservationRequest request, IFhirRepository repo, IFhirMappingService mapper) =>
{
    var observation = await repo.CreateObservationAsync(request);
    return Results.Created($"/fhir/Observation/{observation.Id}", mapper.MapObservationEntityToFhir(observation));
})
.WithName("CreateObservation")
.WithOpenApi();

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
        Total = (uint)observations.Count,
        Entry = observations.Select(o => new Bundle.EntryComponent
        {
            Resource = mapper.MapObservationEntityToFhir(o),
            Search = new Bundle.SearchComponent { Mode = Bundle.SearchEntryMode.Match }
        }).ToList()
    };

    return Results.Ok(bundle);
})
.WithName("GetObservations")
.WithOpenApi();

// GET /fhir/Observation/{id}
app.MapGet("/fhir/Observation/{id}", async (string id, IFhirRepository repo, IFhirMappingService mapper) =>
{
    var observation = await repo.GetObservationByIdAsync(id);
    if (observation == null)
        return Results.NotFound();

    return Results.Ok(mapper.MapObservationEntityToFhir(observation));
})
.WithName("GetObservationById")
.WithOpenApi();

// Dashboard API Endpoints

// GET /api/devices - Get all devices with status for dashboard
app.MapGet("/api/devices", async (IFhirRepository repo) =>
{
    var devices = await repo.GetAllDevicesAsync();
    var deviceStatuses = devices.Select(d => new
    {
        d.DeviceId,
        Type = d.ModelNumber ?? "Dialog+",
        d.Manufacturer,
        Model = d.ModelNumber,
        SerialNumber = d.SerialNumber,
        CurrentPatientId = "P001", // Would be fetched from relationship
        IsOnline = true, // Would check last telemetry timestamp
        LastTelemetryTime = DateTime.UtcNow.AddSeconds(-30),
        ActiveAlarmCount = 0, // Would count active alarms
        RiskLevel = "Low" // Would calculate from anomaly detection
    }).ToList();

    return Results.Ok(deviceStatuses);
})
.WithName("GetDevicesStatus")
.WithOpenApi();

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
.WithOpenApi();

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
.WithOpenApi();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("HealthCheck")
    .WithOpenApi();

app.Run();
