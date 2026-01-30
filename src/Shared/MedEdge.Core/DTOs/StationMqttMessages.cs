namespace MedEdge.Core.DTOs;

/// <summary>
/// MQTT message schema for station status updates.
/// Topic: treatment/{areaId}/station/{stationId}/status
/// </summary>
public record StationStatusMessage(
    string StationId,
    string AreaId,
    string Status,
    DateTime Timestamp,
    string? CurrentTreatmentId,
    string? CurrentPatientId,
    int ActiveDeviceCount,
    int ActiveAlarmCount,
    string? Notes
);

/// <summary>
/// MQTT message schema for aggregated station telemetry.
/// Topic: treatment/{areaId}/station/{stationId}/telemetry
/// </summary>
public record StationTelemetryMessage(
    string StationId,
    string AreaId,
    DateTime Timestamp,
    Dictionary<string, DeviceTelemetryData> DeviceData,
    StationAggregatedMetrics AggregatedMetrics
);

/// <summary>
/// Individual device telemetry data within a station.
/// </summary>
public record DeviceTelemetryData(
    string DeviceId,
    string DeviceType,
    string Status,
    Dictionary<string, double> Measurements,
    Dictionary<string, bool> Alarms
);

/// <summary>
/// Aggregated metrics across all devices in a station.
/// </summary>
public record StationAggregatedMetrics(
    double? AverageBloodFlow,
    double? AverageVenousPressure,
    double? AverageArterialPressure,
    double? AverageTemperature,
    int TotalAlarms,
    int CriticalAlarmCount,
    int WarningAlarmCount
);

/// <summary>
/// MQTT message schema for station alarms.
/// Topic: treatment/{areaId}/station/{stationId}/alarms
/// </summary>
public record StationAlarmMessage(
    string StationId,
    string AreaId,
    DateTime Timestamp,
    string AlarmSeverity, // critical, warning, info
    string AlarmCode,
    string AlarmMessage,
    string? DeviceId,
    string? DeviceType,
    bool RequiresAction,
    string? SuggestedAction
);

/// <summary>
/// MQTT message schema for station commands.
/// Topic: treatment/{areaId}/station/{stationId}/command
/// </summary>
public record StationCommandMessage(
    string StationId,
    string Command, // start-all, stop-all, emergency-stop, sync-parameters
    DateTime Timestamp,
    Dictionary<string, object>? Parameters,
    string? RequestedBy
);

/// <summary>
/// MQTT message schema for zone-level updates.
/// Topic: treatment/{areaId}/status
/// </summary>
public record ZoneStatusMessage(
    string AreaId,
    DateTime Timestamp,
    int TotalStations,
    int AvailableStations,
    int OccupiedStations,
    int MaintenanceStations,
    int OfflineStations,
    List<string> ActiveStationIds
);
