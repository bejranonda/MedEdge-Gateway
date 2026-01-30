using Microsoft.AspNetCore.SignalR;

namespace MedEdge.FhirApi.Hubs;

/// <summary>
/// SignalR Hub for real-time telemetry streaming to connected dashboard clients
/// </summary>
public class TelemetryHub : Hub
{
    private static readonly Dictionary<string, HashSet<string>> DeviceSubscriptions = new();
    private static readonly Dictionary<string, HashSet<string>> AreaSubscriptions = new();
    private static readonly Dictionary<string, HashSet<string>> StationSubscriptions = new();
    private readonly ILogger<TelemetryHub> _logger;

    public TelemetryHub(ILogger<TelemetryHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($"Client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");

        // Clean up device subscriptions
        foreach (var device in DeviceSubscriptions.Keys.ToList())
        {
            if (DeviceSubscriptions[device].Contains(Context.ConnectionId))
            {
                DeviceSubscriptions[device].Remove(Context.ConnectionId);
                if (DeviceSubscriptions[device].Count == 0)
                {
                    DeviceSubscriptions.Remove(device);
                }
            }
        }

        // Clean up area subscriptions
        foreach (var area in AreaSubscriptions.Keys.ToList())
        {
            if (AreaSubscriptions[area].Contains(Context.ConnectionId))
            {
                AreaSubscriptions[area].Remove(Context.ConnectionId);
                if (AreaSubscriptions[area].Count == 0)
                {
                    AreaSubscriptions.Remove(area);
                }
            }
        }

        // Clean up station subscriptions
        foreach (var station in StationSubscriptions.Keys.ToList())
        {
            if (StationSubscriptions[station].Contains(Context.ConnectionId))
            {
                StationSubscriptions[station].Remove(Context.ConnectionId);
                if (StationSubscriptions[station].Count == 0)
                {
                    StationSubscriptions.Remove(station);
                }
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to real-time telemetry updates for a specific device
    /// </summary>
    /// <param name="deviceId">Device identifier to subscribe to</param>
    public async Task SubscribeToDevice(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            await Clients.Caller.SendAsync("Error", "Device ID cannot be empty");
            return;
        }

        if (!DeviceSubscriptions.ContainsKey(deviceId))
        {
            DeviceSubscriptions[deviceId] = new HashSet<string>();
        }

        if (DeviceSubscriptions[deviceId].Add(Context.ConnectionId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, deviceId);
            _logger.LogInformation($"Client {Context.ConnectionId} subscribed to device {deviceId}");
            await Clients.Caller.SendAsync("SubscriptionConfirmed", new { deviceId });
        }
    }

    /// <summary>
    /// Unsubscribe from real-time telemetry updates for a specific device
    /// </summary>
    /// <param name="deviceId">Device identifier to unsubscribe from</param>
    public async Task UnsubscribeFromDevice(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            return;

        if (DeviceSubscriptions.ContainsKey(deviceId))
        {
            DeviceSubscriptions[deviceId].Remove(Context.ConnectionId);
            if (DeviceSubscriptions[deviceId].Count == 0)
            {
                DeviceSubscriptions.Remove(deviceId);
            }
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, deviceId);
        _logger.LogInformation($"Client {Context.ConnectionId} unsubscribed from device {deviceId}");
    }

    /// <summary>
    /// Broadcast a vital sign update to all subscribers of a device
    /// </summary>
    public async Task BroadcastVitalSignUpdate(string deviceId, object vitalSignUpdate)
    {
        await Clients.Group(deviceId).SendAsync("VitalSignUpdate", vitalSignUpdate);
    }

    /// <summary>
    /// Broadcast clinical alerts to all subscribers of a device
    /// </summary>
    public async Task BroadcastAlerts(string deviceId, object alerts)
    {
        await Clients.Group(deviceId).SendAsync("AlertsReceived", alerts);
    }

    /// <summary>
    /// Get list of all devices with active subscriptions
    /// </summary>
    public async Task<IEnumerable<string>> GetActiveDevices()
    {
        return await Task.FromResult(DeviceSubscriptions.Keys);
    }

    /// <summary>
    /// Get subscription count for a specific device
    /// </summary>
    public async Task<int> GetSubscriberCount(string deviceId)
    {
        return await Task.FromResult(DeviceSubscriptions.ContainsKey(deviceId) ? DeviceSubscriptions[deviceId].Count : 0);
    }

    // ========== Treatment Area (Zone) Methods ==========

    /// <summary>
    /// Subscribe to updates for a specific treatment area/zone
    /// </summary>
    /// <param name="areaId">Treatment area identifier</param>
    public async Task SubscribeToArea(string areaId)
    {
        if (string.IsNullOrWhiteSpace(areaId))
        {
            await Clients.Caller.SendAsync("Error", "Area ID cannot be empty");
            return;
        }

        if (!AreaSubscriptions.ContainsKey(areaId))
        {
            AreaSubscriptions[areaId] = new HashSet<string>();
        }

        if (AreaSubscriptions[areaId].Add(Context.ConnectionId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"area-{areaId}");
            _logger.LogInformation($"Client {Context.ConnectionId} subscribed to area {areaId}");
            await Clients.Caller.SendAsync("AreaSubscriptionConfirmed", new { areaId });
        }
    }

    /// <summary>
    /// Unsubscribe from updates for a specific treatment area
    /// </summary>
    /// <param name="areaId">Treatment area identifier</param>
    public async Task UnsubscribeFromArea(string areaId)
    {
        if (string.IsNullOrWhiteSpace(areaId))
            return;

        if (AreaSubscriptions.ContainsKey(areaId))
        {
            AreaSubscriptions[areaId].Remove(Context.ConnectionId);
            if (AreaSubscriptions[areaId].Count == 0)
            {
                AreaSubscriptions.Remove(areaId);
            }
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"area-{areaId}");
        _logger.LogInformation($"Client {Context.ConnectionId} unsubscribed from area {areaId}");
    }

    /// <summary>
    /// Broadcast area status update to all subscribers
    /// </summary>
    public async Task BroadcastAreaUpdate(string areaId, object update)
    {
        await Clients.Group($"area-{areaId}").SendAsync("AreaUpdated", update);
    }

    // ========== Station Methods ==========

    /// <summary>
    /// Subscribe to updates for a specific treatment station
    /// </summary>
    /// <param name="stationId">Station identifier</param>
    public async Task SubscribeToStation(string stationId)
    {
        if (string.IsNullOrWhiteSpace(stationId))
        {
            await Clients.Caller.SendAsync("Error", "Station ID cannot be empty");
            return;
        }

        if (!StationSubscriptions.ContainsKey(stationId))
        {
            StationSubscriptions[stationId] = new HashSet<string>();
        }

        if (StationSubscriptions[stationId].Add(Context.ConnectionId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"station-{stationId}");
            _logger.LogInformation($"Client {Context.ConnectionId} subscribed to station {stationId}");
            await Clients.Caller.SendAsync("StationSubscriptionConfirmed", new { stationId });
        }
    }

    /// <summary>
    /// Unsubscribe from updates for a specific treatment station
    /// </summary>
    /// <param name="stationId">Station identifier</param>
    public async Task UnsubscribeFromStation(string stationId)
    {
        if (string.IsNullOrWhiteSpace(stationId))
            return;

        if (StationSubscriptions.ContainsKey(stationId))
        {
            StationSubscriptions[stationId].Remove(Context.ConnectionId);
            if (StationSubscriptions[stationId].Count == 0)
            {
                StationSubscriptions.Remove(stationId);
            }
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"station-{stationId}");
        _logger.LogInformation($"Client {Context.ConnectionId} unsubscribed from station {stationId}");
    }

    /// <summary>
    /// Broadcast station status update to all subscribers
    /// </summary>
    public async Task BroadcastStationUpdate(string stationId, object update)
    {
        await Clients.Group($"station-{stationId}").SendAsync("StationUpdated", update);
    }

    /// <summary>
    /// Broadcast station alert to all subscribers
    /// </summary>
    public async Task BroadcastStationAlert(string stationId, object alert)
    {
        await Clients.Group($"station-{stationId}").SendAsync("StationAlert", alert);
    }

    /// <summary>
    /// Broadcast to all stations (fleet-wide update)
    /// </summary>
    public async Task BroadcastToAllStations(object update)
    {
        await Clients.Group("stations").SendAsync("AllStationsUpdated", update);
    }
}
