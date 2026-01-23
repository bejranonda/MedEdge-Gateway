using Microsoft.Extensions.Logging;
using MedEdge.Core.DTOs;
using MedEdge.EdgeGateway.Models;
using System.Threading.Channels;

namespace MedEdge.TransformService.Services;

public interface IFhirTransformService
{
    CreateObservationRequest TransformTelemetryToObservation(TelemetryMessage telemetry, string code, string codeDisplay, string unit);
}

public class FhirTransformService : IFhirTransformService
{
    private readonly ILogger<FhirTransformService> _logger;

    public FhirTransformService(ILogger<FhirTransformService> logger)
    {
        _logger = logger;
    }

    public CreateObservationRequest TransformTelemetryToObservation(
        TelemetryMessage telemetry,
        string code,
        string codeDisplay,
        string unit)
    {
        return new CreateObservationRequest(
            telemetry.DeviceId,
            telemetry.DeviceId,
            code,
            codeDisplay,
            0, // Will be replaced with actual measurement
            unit,
            telemetry.Timestamp
        );
    }
}

public class TelemetryToObservationMapper
{
    private readonly ILogger<TelemetryToObservationMapper> _logger;
    private readonly IFhirTransformService _transformService;
    private readonly IFhirApiClient _fhirApiClient;
    private readonly Channel<TelemetryMessage> _telemetryChannel;

    public TelemetryToObservationMapper(
        ILogger<TelemetryToObservationMapper> logger,
        IFhirTransformService transformService,
        IFhirApiClient fhirApiClient,
        Channel<TelemetryMessage> telemetryChannel)
    {
        _logger = logger;
        _transformService = transformService;
        _fhirApiClient = fhirApiClient;
        _telemetryChannel = telemetryChannel;
    }

    public async Task MapAndPersistAsync(CancellationToken stoppingToken)
    {
        await foreach (var telemetry in _telemetryChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessTelemetryAsync(telemetry, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing telemetry from {DeviceId}", telemetry.DeviceId);
            }
        }
    }

    private async Task ProcessTelemetryAsync(TelemetryMessage telemetry, CancellationToken stoppingToken)
    {
        // Extract patient ID from device (would normally look up from device registry)
        var patientId = ExtractPatientIdFromDevice(telemetry.DeviceId);
        if (string.IsNullOrEmpty(patientId))
        {
            _logger.LogWarning("Could not determine patient for device {DeviceId}", telemetry.DeviceId);
            return;
        }

        // Transform each measurement to FHIR Observations
        var observations = new List<CreateObservationRequest>();

        if (telemetry.Measurements.TryGetValue("bloodFlow", out var bloodFlow))
        {
            var obs = MapMeasurement(
                patientId,
                telemetry.DeviceId,
                "33438-3",
                "Blood Flow Rate",
                bloodFlow,
                "mL/min",
                telemetry.Timestamp
            );
            observations.Add(obs);
        }

        if (telemetry.Measurements.TryGetValue("arterialPressure", out var apPressure))
        {
            var obs = MapMeasurement(
                patientId,
                telemetry.DeviceId,
                "75992-9",
                "Arterial Pressure",
                apPressure,
                "mmHg",
                telemetry.Timestamp
            );
            observations.Add(obs);
        }

        if (telemetry.Measurements.TryGetValue("venousPressure", out var vpPressure))
        {
            var obs = MapMeasurement(
                patientId,
                telemetry.DeviceId,
                "60956-0",
                "Venous Pressure",
                vpPressure,
                "mmHg",
                telemetry.Timestamp
            );
            observations.Add(obs);
        }

        if (telemetry.Measurements.TryGetValue("dialysateTemperature", out var temperature))
        {
            var obs = MapMeasurement(
                patientId,
                telemetry.DeviceId,
                "8310-5",
                "Body Temperature",
                temperature,
                "°C",
                telemetry.Timestamp
            );
            observations.Add(obs);
        }

        if (telemetry.Measurements.TryGetValue("conductivity", out var conductivity))
        {
            var obs = MapMeasurement(
                patientId,
                telemetry.DeviceId,
                "2164-2",
                "Conductivity",
                conductivity,
                "mS/cm",
                telemetry.Timestamp
            );
            observations.Add(obs);
        }

        // Persist observations
        foreach (var observation in observations)
        {
            await _fhirApiClient.CreateObservationAsync(observation, stoppingToken);
            _logger.LogDebug("Created observation {Code} for patient {PatientId}", observation.Code, patientId);
        }
    }

    private CreateObservationRequest MapMeasurement(
        string patientId,
        string deviceId,
        string code,
        string codeDisplay,
        double value,
        string unit,
        DateTime timestamp)
    {
        return new CreateObservationRequest(
            patientId,
            deviceId,
            code,
            codeDisplay,
            value,
            unit,
            timestamp
        );
    }

    private string ExtractPatientIdFromDevice(string deviceId)
    {
        // Simple mapping for demo: Device-001 → P001, Device-002 → P002, etc.
        return deviceId switch
        {
            "Device-001" => "P001",
            "Device-002" => "P002",
            "Device-003" => "P003",
            _ => string.Empty
        };
    }
}
