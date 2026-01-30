using MedEdge.Core.DTOs;
using MedEdge.DeviceCoordinationService.Services;
using MedEdge.FhirApi.Data;
using Microsoft.EntityFrameworkCore;
using MQTTnet;
using MQTTnet.Client;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/coordination-service-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=medEdge.db";
    options.UseSqlite(connectionString);
});

// Add MQTT client
var mqttFactory = new MqttFactory();
builder.Services.AddSingleton<IMqttClient>(mqttFactory.CreateMqttClient());

// Add services
builder.Services.AddScoped<IDeviceCoordinationService, DeviceCoordinationService>();

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

// Initialize MQTT client
var mqttClient = app.Services.GetRequiredService<IMqttClient>();
var mqttOptions = new MqttClientOptionsBuilder()
    .WithTcpServer(builder.Configuration["Mqtt:BrokerHost"] ?? "localhost", 1883)
    .WithClientId($"coordination-service-{Guid.NewGuid()}")
    .WithCleanSession()
    .Build();

try
{
    await mqttClient.ConnectAsync(mqttOptions);
    Log.Information("Connected to MQTT broker");
}
catch (Exception ex)
{
    Log.Warning(ex, "Failed to connect to MQTT broker");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseSerilogRequestLogging();

// Device Coordination Endpoints

// GET /api/coordination/station/{stationId}/groups - Get device groups for a station
app.MapGet("/api/coordination/station/{stationId}/groups", async (
    string stationId,
    IDeviceCoordinationService service) =>
{
    var groups = await service.GetStationGroupsAsync(stationId);
    var dtos = groups.Select(g => new DeviceGroupDto(
        g.Id,
        g.StationId,
        g.GroupName,
        g.GroupType,
        g.DeviceIds,
        g.DeviceIds.Count,
        g.Description,
        g.IsActive
    ));
    return Results.Ok(dtos);
})
.WithName("GetStationGroups")
.WithOpenApi();

// POST /api/coordination/groups - Create a new device group
app.MapPost("/api/coordination/groups", async (
    CreateDeviceGroupRequest request,
    IDeviceCoordinationService service) =>
{
    try
    {
        var group = await service.CreateGroupAsync(request);
        return Results.Created($"/api/coordination/groups/{group.Id}", group);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("CreateDeviceGroup")
.WithOpenApi();

// PUT /api/coordination/groups/{groupId} - Update device group
app.MapPut("/api/coordination/groups/{groupId}", async (
    string groupId,
    UpdateDeviceGroupRequest request,
    IDeviceCoordinationService service) =>
{
    var group = await service.UpdateGroupAsync(groupId, request);
    if (group == null)
        return Results.NotFound();
    return Results.Ok(group);
})
.WithName("UpdateDeviceGroup")
.WithOpenApi();

// DELETE /api/coordination/groups/{groupId} - Delete device group
app.MapDelete("/api/coordination/groups/{groupId}", async (
    string groupId,
    IDeviceCoordinationService service) =>
{
    var success = await service.DeleteGroupAsync(groupId);
    if (!success)
        return Results.NotFound();
    return Results.NoContent();
})
.WithName("DeleteDeviceGroup")
.WithOpenApi();

// POST /api/coordination/station/{stationId}/start-all - Start all devices at a station
app.MapPost("/api/coordination/station/{stationId}/start-all", async (
    string stationId,
    string? requestedBy,
    IDeviceCoordinationService service) =>
{
    var results = await service.StartAllDevicesAsync(stationId, requestedBy);
    return Results.Ok(new
    {
        stationId,
        operation = "start-all",
        results,
        timestamp = DateTime.UtcNow
    });
})
.WithName("StartAllDevices")
.WithOpenApi();

// POST /api/coordination/station/{stationId}/stop-all - Stop all devices at a station
app.MapPost("/api/coordination/station/{stationId}/stop-all", async (
    string stationId,
    string? requestedBy,
    IDeviceCoordinationService service) =>
{
    var results = await service.StopAllDevicesAsync(stationId, requestedBy);
    return Results.Ok(new
    {
        stationId,
        operation = "stop-all",
        results,
        timestamp = DateTime.UtcNow
    });
})
.WithName("StopAllDevices")
.WithOpenApi();

// POST /api/coordination/station/{stationId}/emergency-stop - Emergency stop all devices at a station
app.MapPost("/api/coordination/station/{stationId}/emergency-stop", async (
    string stationId,
    string? requestedBy,
    IDeviceCoordinationService service,
    ILogger<Program> logger) =>
{
    logger.LogWarning("EMERGENCY STOP requested for station {StationId} by {RequestedBy}", stationId, requestedBy ?? "unknown");
    var results = await service.EmergencyStopAllAsync(stationId, requestedBy);
    return Results.Ok(new
    {
        stationId,
        operation = "emergency-stop",
        results,
        timestamp = DateTime.UtcNow
    });
})
.WithName("EmergencyStopAll")
.WithOpenApi();

// POST /api/coordination/sync-parameters - Sync parameters across devices
app.MapPost("/api/coordination/sync-parameters", async (
    string stationId,
    Dictionary<string, object> parameters,
    IDeviceCoordinationService service) =>
{
    var results = await service.SyncParametersAsync(stationId, parameters);
    return Results.Ok(new
    {
        stationId,
        operation = "sync-parameters",
        parameters,
        results,
        timestamp = DateTime.UtcNow
    });
})
.WithName("SyncParameters")
.WithOpenApi();

// POST /api/coordination/execute - Execute custom station command
app.MapPost("/api/coordination/execute", async (
    ExecuteStationCommandRequest request,
    IDeviceCoordinationService service) =>
{
    try
    {
        var command = await service.ExecuteStationCommandAsync(request);
        return Results.Accepted($"/api/coordination/commands/{command.Id}", command);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("ExecuteStationCommand")
.WithOpenApi();

// GET /api/coordination/commands/{commandId} - Get command status
app.MapGet("/api/coordination/commands/{commandId}", async (
    string commandId,
    IDeviceCoordinationService service) =>
{
    var command = await service.GetCommandAsync(commandId);
    if (command == null)
        return Results.NotFound();

    var successfulCount = command.DeviceResults.Count(r => r.Value == "sent");
    var failedCount = command.DeviceResults.Count(r => r.Value.StartsWith("failed"));

    var dto = new CoordinationCommandDto(
        command.Id,
        command.StationId,
        command.Operation,
        command.Parameters,
        command.TargetDevices.Count,
        command.ScheduledExecutionTime,
        command.ActualExecutionTime,
        command.Status,
        command.ResultSummary,
        successfulCount,
        failedCount,
        command.RequestedBy
    );
    return Results.Ok(dto);
})
.WithName("GetCommandStatus")
.WithOpenApi();

// GET /api/coordination/station/{stationId}/commands - Get commands for a station
app.MapGet("/api/coordination/station/{stationId}/commands", async (
    string stationId,
    IDeviceCoordinationService service) =>
{
    var commands = await service.GetStationCommandsAsync(stationId);
    var dtos = commands.Select(c => new CoordinationCommandDto(
        c.Id,
        c.StationId,
        c.Operation,
        c.Parameters,
        c.TargetDevices.Count,
        c.ScheduledExecutionTime,
        c.ActualExecutionTime,
        c.Status,
        c.ResultSummary,
        c.DeviceResults.Count(r => r.Value == "sent"),
        c.DeviceResults.Count(r => r.Value.StartsWith("failed")),
        c.RequestedBy
    ));
    return Results.Ok(dtos);
})
.WithName("GetStationCommands")
.WithOpenApi();

// DELETE /api/coordination/commands/{commandId} - Cancel pending command
app.MapDelete("/api/coordination/commands/{commandId}", async (
    string commandId,
    IDeviceCoordinationService service) =>
{
    var success = await service.CancelCommandAsync(commandId);
    if (!success)
        return Results.NotFound();
    return Results.NoContent();
})
.WithName("CancelCommand")
.WithOpenApi();

// Health check
app.MapGet("/health", () => Results.Ok(new
{
    service = "DeviceCoordinationService",
    status = "healthy",
    mqttConnected = mqttClient.IsConnected,
    timestamp = DateTime.UtcNow
}))
.WithName("HealthCheck");

try
{
    Log.Information("Starting Device Coordination Service");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Device Coordination Service failed to start");
}
finally
{
    Log.CloseAndFlush();
}
