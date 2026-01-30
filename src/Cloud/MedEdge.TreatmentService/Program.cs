using MedEdge.TreatmentService.Services;
using MedEdge.FhirApi.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/treatment-service-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=medEdge.db";
    options.UseSqlite(connectionString);
});

// Add services
builder.Services.AddScoped<ITreatmentSessionService, TreatmentSessionService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
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
app.UseSerilogRequestLogging();

// Treatment Session Endpoints

// GET /api/treatments - Get all treatment sessions
app.MapGet("/api/treatments", async (ITreatmentSessionService service) =>
{
    var sessions = await service.GetAllSessionsAsync();
    var dtos = sessions.Select(s => new TreatmentSessionDto(
        s.Id,
        s.PatientId,
        $"{s.Patient.GivenName} {s.Patient.FamilyName}",
        s.StationId,
        s.Station.StationNumber,
        s.ScheduledStart,
        s.ActualStart,
        s.ActualEnd,
        s.TreatmentType,
        s.Status,
        s.PrescribingPhysician,
        s.Notes,
        s.ActualStart.HasValue && s.ActualEnd.HasValue
            ? (int)(s.ActualEnd - s.ActualStart).Value.TotalMinutes
            : null,
        s.Phases.FirstOrDefault(p => p.Status == "in-progress")?.PhaseName
            ?? s.Phases.LastOrDefault()?.PhaseName ?? "unknown",
        s.Phases.Select(p => new TreatmentPhaseSummaryDto(
            p.Id,
            p.PhaseName,
            p.Status,
            p.StartedAt,
            p.CompletedAt,
            p.DurationMinutes
        )).ToList()
    ));
    return Results.Ok(dtos);
})
.WithName("GetAllTreatments")
.WithOpenApi();

// GET /api/treatments/active - Get active treatments
app.MapGet("/api/treatments/active", async (ITreatmentSessionService service) =>
{
    var sessions = await service.GetActiveSessionsAsync();
    return Results.Ok(sessions);
})
.WithName("GetActiveTreatments")
.WithOpenApi();

// GET /api/treatments/{id} - Get specific treatment session
app.MapGet("/api/treatments/{id}", async (string id, ITreatmentSessionService service) =>
{
    var session = await service.GetSessionByIdAsync(id);
    if (session == null)
        return Results.NotFound();
    return Results.Ok(session);
})
.WithName("GetTreatmentById")
.WithOpenApi();

// POST /api/treatments/schedule - Schedule new treatment session
app.MapPost("/api/treatments/schedule", async (
    CreateTreatmentSessionRequest request,
    ITreatmentSessionService service,
    ILogger<Program> logger) =>
{
    try
    {
        var session = await service.ScheduleSessionAsync(request);
        return Results.Created($"/api/treatments/{session.Id}", session);
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "Failed to schedule treatment session");
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("ScheduleTreatment")
.WithOpenApi();

// PUT /api/treatments/{id}/start - Start treatment session
app.MapPut("/api/treatments/{id}/start", async (
    string id,
    StartTreatmentSessionRequest request,
    ITreatmentSessionService service) =>
{
    var session = await service.StartSessionAsync(id, request);
    if (session == null)
        return Results.NotFound();
    return Results.Ok(session);
})
.WithName("StartTreatment")
.WithOpenApi();

// PUT /api/treatments/{id}/phase - Update treatment phase
app.MapPut("/api/treatments/{id}/phase", async (
    string id,
    UpdateTreatmentSessionRequest request,
    ITreatmentSessionService service) =>
{
    var session = await service.UpdateSessionAsync(id, request);
    if (session == null)
        return Results.NotFound();
    return Results.Ok(session);
})
.WithName("UpdateTreatmentPhase")
.WithOpenApi();

// PUT /api/treatments/{id}/interrupt - Interrupt treatment session
app.MapPut("/api/treatments/{id}/interrupt", async (
    string id,
    InterruptTreatmentSessionRequest request,
    ITreatmentSessionService service) =>
{
    var session = await service.InterruptSessionAsync(id, request);
    if (session == null)
        return Results.NotFound();
    return Results.Ok(session);
})
.WithName("InterruptTreatment")
.WithOpenApi();

// POST /api/treatments/{id}/complete - Complete treatment session
app.MapPost("/api/treatments/{id}/complete", async (
    string id,
    CompleteTreatmentSessionRequest request,
    ITreatmentSessionService service) =>
{
    var session = await service.CompleteSessionAsync(id, request);
    if (session == null)
        return Results.NotFound();
    return Results.Ok(session);
})
.WithName("CompleteTreatment")
.WithOpenApi();

// DELETE /api/treatments/{id} - Cancel treatment session
app.MapDelete("/api/treatments/{id}", async (string id, ITreatmentSessionService service) =>
{
    try
    {
        var success = await service.CancelSessionAsync(id);
        if (!success)
            return Results.NotFound();
        return Results.NoContent();
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("CancelTreatment")
.WithOpenApi();

// GET /api/treatments/{id}/observations - Get treatment observations
app.MapGet("/api/treatments/{id}/observations", async (
    string id,
    ITreatmentSessionService service) =>
{
    var observations = await service.GetObservationsAsync(id);
    return Results.Ok(observations);
})
.WithName("GetTreatmentObservations")
.WithOpenApi();

// POST /api/treatments/observations - Add treatment observation
app.MapPost("/api/treatments/observations", async (
    CreateTreatmentObservationRequest request,
    ITreatmentSessionService service) =>
{
    var observation = await service.AddObservationAsync(request);
    return Results.Created($"/api/treatments/observations/{observation.Id}", observation);
})
.WithName("AddTreatmentObservation")
.WithOpenApi();

// GET /api/stations/available - Get available stations
app.MapGet("/api/stations/available", async (
    DateTime? forDate,
    ITreatmentSessionService service) =>
{
    var stations = await service.GetAvailableStationsAsync(forDate);
    return Results.Ok(stations);
})
.WithName("GetAvailableStations")
.WithOpenApi();

// Health check
app.MapGet("/health", () => Results.Ok(new
{
    service = "TreatmentService",
    status = "healthy",
    timestamp = DateTime.UtcNow
}))
.WithName("HealthCheck");

try
{
    Log.Information("Starting Treatment Service");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Treatment Service failed to start");
}
finally
{
    Log.CloseAndFlush();
}
