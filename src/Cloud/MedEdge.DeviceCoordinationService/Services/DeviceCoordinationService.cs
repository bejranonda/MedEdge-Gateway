using MedEdge.Core.Domain.Entities;
using MedEdge.Core.DTOs;
using MedEdge.FhirApi.Data;
using Microsoft.EntityFrameworkCore;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using System.Text.Json;

namespace MedEdge.DeviceCoordinationService.Services;

/// <summary>
/// Service for coordinating multi-device operations at treatment stations.
/// </summary>
public interface IDeviceCoordinationService
{
    // Device Groups
    Task<List<DeviceGroupEntity>> GetStationGroupsAsync(string stationId);
    Task<DeviceGroupEntity?> CreateGroupAsync(CreateDeviceGroupRequest request);
    Task<DeviceGroupEntity?> UpdateGroupAsync(string groupId, UpdateDeviceGroupRequest request);
    Task<bool> DeleteGroupAsync(string groupId);

    // Coordinated Commands
    Task<DeviceCoordinationCommand> ExecuteStationCommandAsync(ExecuteStationCommandRequest request);
    Task<DeviceCoordinationCommand?> GetCommandAsync(string commandId);
    Task<List<DeviceCoordinationCommand>> GetStationCommandsAsync(string stationId);
    Task<bool> CancelCommandAsync(string commandId);

    // Bulk Operations
    Task<Dictionary<string, string>> StartAllDevicesAsync(string stationId, string? requestedBy);
    Task<Dictionary<string, string>> StopAllDevicesAsync(string stationId, string? requestedBy);
    Task<Dictionary<string, string>> EmergencyStopAllAsync(string stationId, string? requestedBy);
    Task<Dictionary<string, string>> SyncParametersAsync(string stationId, Dictionary<string, object> parameters);
}

public class DeviceCoordinationService : IDeviceCoordinationService
{
    private readonly ApplicationDbContext _context;
    private readonly IMqttClient _mqttClient;
    private readonly ILogger<DeviceCoordinationService> _logger;

    public DeviceCoordinationService(
        ApplicationDbContext context,
        IMqttClient mqttClient,
        ILogger<DeviceCoordinationService> logger)
    {
        _context = context;
        _mqttClient = mqttClient;
        _logger = logger;
    }

    // Device Groups

    public async Task<List<DeviceGroupEntity>> GetStationGroupsAsync(string stationId)
    {
        return await _context.DeviceGroups
            .Include(g => g.Station)
            .Where(g => g.StationId == stationId && g.IsActive)
            .OrderBy(g => g.DisplayOrder)
            .ToListAsync();
    }

    public async Task<DeviceGroupEntity?> CreateGroupAsync(CreateDeviceGroupRequest request)
    {
        var station = await _context.Stations.FindAsync(request.StationId);
        if (station == null)
            throw new InvalidOperationException($"Station {request.StationId} not found");

        // Verify all devices exist at the station
        foreach (var deviceId in request.DeviceIds)
        {
            var device = await _context.Devices.FindAsync(deviceId);
            if (device == null || device.StationId != request.StationId)
            {
                throw new InvalidOperationException($"Device {deviceId} not found at station {request.StationId}");
            }
        }

        var group = new DeviceGroupEntity
        {
            Id = $"DG-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            StationId = request.StationId,
            GroupName = request.GroupName,
            GroupType = request.GroupType,
            DeviceIds = request.DeviceIds,
            CoordinationRules = request.CoordinationRules,
            Description = request.Description,
            IsActive = true,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.DeviceGroups.Add(group);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created device group {GroupId} at station {StationId}", group.Id, request.StationId);
        return group;
    }

    public async Task<DeviceGroupEntity?> UpdateGroupAsync(string groupId, UpdateDeviceGroupRequest request)
    {
        var group = await _context.DeviceGroups.FindAsync(groupId);
        if (group == null) return null;

        group.GroupName = request.GroupName;
        group.GroupType = request.GroupType;
        group.DeviceIds = request.DeviceIds;
        group.CoordinationRules = request.CoordinationRules;
        group.Description = request.Description;
        group.DisplayOrder = request.DisplayOrder;
        group.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return group;
    }

    public async Task<bool> DeleteGroupAsync(string groupId)
    {
        var group = await _context.DeviceGroups.FindAsync(groupId);
        if (group == null) return false;

        group.IsActive = false;
        group.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    // Coordinated Commands

    public async Task<DeviceCoordinationCommand> ExecuteStationCommandAsync(ExecuteStationCommandRequest request)
    {
        var station = await _context.Stations.FindAsync(request.StationId);
        if (station == null)
            throw new InvalidOperationException($"Station {request.StationId} not found");

        // Determine target devices
        var targetDevices = new List<string>();
        if (request.TargetDevices.Any())
        {
            targetDevices = request.TargetDevices;
        }
        else if (request.TargetGroups.Any())
        {
            var groups = await _context.DeviceGroups
                .Where(g => request.TargetGroups.Contains(g.Id) && g.IsActive)
                .ToListAsync();
            foreach (var group in groups)
            {
                targetDevices.AddRange(group.DeviceIds);
            }
        }
        else
        {
            // All devices at station
            var devices = await _context.Devices
                .Where(d => d.StationId == request.StationId)
                .Select(d => d.Id)
                .ToListAsync();
            targetDevices = devices;
        }

        var command = new DeviceCoordinationCommand
        {
            Id = $"CMD-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            StationId = request.StationId,
            Operation = request.Operation,
            Parameters = request.Parameters,
            TargetDevices = targetDevices.Distinct().ToList(),
            TargetGroups = request.TargetGroups,
            ScheduledExecutionTime = request.ScheduledExecutionTime ?? DateTime.UtcNow,
            Status = "pending",
            RequestedBy = request.RequestedBy,
            CreatedAt = DateTime.UtcNow
        };

        _context.CoordinationCommands.Add(command);
        await _context.SaveChangesAsync();

        // Execute command asynchronously
        _ = Task.Run(() => ExecuteCommandAsync(command));

        return command;
    }

    public async Task<DeviceCoordinationCommand?> GetCommandAsync(string commandId)
    {
        return await _context.CoordinationCommands
            .Include(c => c.Station)
            .FirstOrDefaultAsync(c => c.Id == commandId);
    }

    public async Task<List<DeviceCoordinationCommand>> GetStationCommandsAsync(string stationId)
    {
        return await _context.CoordinationCommands
            .Where(c => c.StationId == stationId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> CancelCommandAsync(string commandId)
    {
        var command = await _context.CoordinationCommands.FindAsync(commandId);
        if (command == null || command.Status != "pending") return false;

        command.Status = "cancelled";
        await _context.SaveChangesAsync();
        return true;
    }

    // Bulk Operations

    public async Task<Dictionary<string, string>> StartAllDevicesAsync(string stationId, string? requestedBy)
    {
        return await ExecuteBulkOperationAsync(stationId, "start-all", new(), requestedBy);
    }

    public async Task<Dictionary<string, string>> StopAllDevicesAsync(string stationId, string? requestedBy)
    {
        return await ExecuteBulkOperationAsync(stationId, "stop-all", new(), requestedBy);
    }

    public async Task<Dictionary<string, string>> EmergencyStopAllAsync(string stationId, string? requestedBy)
    {
        _logger.LogWarning("EMERGENCY STOP requested for station {StationId}", stationId);
        return await ExecuteBulkOperationAsync(stationId, "emergency-stop", new(), requestedBy);
    }

    public async Task<Dictionary<string, string>> SyncParametersAsync(string stationId, Dictionary<string, object> parameters)
    {
        return await ExecuteBulkOperationAsync(stationId, "sync-parameters", parameters, "System");
    }

    // Private Methods

    private async Task<Dictionary<string, string>> ExecuteBulkOperationAsync(
        string stationId,
        string operation,
        Dictionary<string, object> parameters,
        string? requestedBy)
    {
        var devices = await _context.Devices
            .Where(d => d.StationId == stationId)
            .Select(d => new { d.Id, d.DeviceId })
            .ToListAsync();

        var results = new Dictionary<string, string>();

        var command = new DeviceCoordinationCommand
        {
            Id = $"CMD-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            StationId = stationId,
            Operation = operation,
            Parameters = parameters,
            TargetDevices = devices.Select(d => d.Id).ToList(),
            ScheduledExecutionTime = DateTime.UtcNow,
            RequestedBy = requestedBy,
            CreatedAt = DateTime.UtcNow
        };

        _context.CoordinationCommands.Add(command);
        await _context.SaveChangesAsync();

        // Execute and return immediately with pending command ID
        _ = Task.Run(() => ExecuteCommandAsync(command));

        foreach (var device in devices)
        {
            results[device.Id] = "pending";
        }

        return results;
    }

    private async Task ExecuteCommandAsync(DeviceCoordinationCommand command)
    {
        try
        {
            command.Status = "executing";
            command.ActualExecutionTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var results = new Dictionary<string, string>();

            // Publish MQTT commands to each device
            foreach (var deviceId in command.TargetDevices)
            {
                try
                {
                    var device = await _context.Devices.FindAsync(deviceId);
                    if (device?.DeviceId == null)
                    {
                        results[deviceId] = "failed: device not found";
                        continue;
                    }

                    var mqttMessage = new
                    {
                        commandId = command.Id,
                        operation = command.Operation,
                        parameters = command.Parameters,
                        timestamp = DateTime.UtcNow
                    };

                    var topic = $"treatment/command/{device.DeviceId}";
                    var payload = JsonSerializer.Serialize(mqttMessage);

                    if (_mqttClient.IsConnected)
                    {
                        var mqttMessageBuilder = new MqttApplicationMessageBuilder()
                            .WithTopic(topic)
                            .WithPayload(payload)
                            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce);

                        await _mqttClient.PublishAsync(mqttMessageBuilder.Build());
                        results[deviceId] = "sent";
                    }
                    else
                    {
                        results[deviceId] = "failed: mqtt not connected";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send command to device {DeviceId}", deviceId);
                    results[deviceId] = $"failed: {ex.Message}";
                }

                // Small delay between commands
                await Task.Delay(100);
            }

            command.DeviceResults = results;
            command.Status = results.All(r => r.Value == "sent") ? "completed" : "completed_with_errors";
            command.ResultSummary = $"{results.Count(r => r.Value == "sent")} of {results.Count} commands sent successfully";
            command.ActualExecutionTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Command {CommandId} completed: {Summary}", command.Id, command.ResultSummary);
        }
        catch (Exception ex)
        {
            command.Status = "failed";
            command.ErrorMessage = ex.Message;
            await _context.SaveChangesAsync();

            _logger.LogError(ex, "Failed to execute command {CommandId}", command.Id);
        }
    }
}
