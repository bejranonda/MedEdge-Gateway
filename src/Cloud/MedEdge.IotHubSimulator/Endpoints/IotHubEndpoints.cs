using MedEdge.IotHubSimulator.Models;
using MedEdge.IotHubSimulator.Services;

namespace MedEdge.IotHubSimulator.Endpoints;

/// <summary>
/// Azure IoT Hub REST API Simulation
/// Maps to Azure IoT Hub REST API endpoints
/// Documentation: https://learn.microsoft.com/en-us/rest/api/iothub/
/// </summary>
public static class IotHubEndpoints
{
    public static void MapIotHubEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/iot/devices")
            .WithTags("Devices")
            .WithOpenApi();

        // ===== Device Registry =====

        group.MapGet("/", (IotHubService iotHub) =>
        {
            return Results.Ok(iotHub.GetAllDevices());
        })
        .WithSummary("List all devices")
        .WithDescription("Azure IoT Hub equivalent: GET /devices?api-version=2020-09-01");

        group.MapGet("/{deviceId}", (string deviceId, IotHubService iotHub) =>
        {
            var device = iotHub.GetDevice(deviceId);
            return device is not null ? Results.Ok(device) : Results.NotFound();
        })
        .WithSummary("Get device by ID")
        .WithDescription("Returns device info including connection state and tags");

        group.MapPost("/", (DeviceCreateRequest request, IotHubService iotHub) =>
        {
            var device = iotHub.CreateDevice(request.DeviceId, request.TemplateId);
            return Results.Created($"/iot/devices/{device.DeviceId}", device);
        })
        .WithSummary("Create new device")
        .WithDescription("Provision a new device identity in the IoT Hub");

        group.MapDelete("/{deviceId}", (string deviceId, IotHubService iotHub) =>
        {
            return iotHub.DeleteDevice(deviceId) ? Results.NoContent() : Results.NotFound();
        })
        .WithSummary("Delete device")
        .WithDescription("Remove device from IoT Hub registry");

        // ===== Device Twins =====

        group.MapGet("/{deviceId}/twin", (string deviceId, IotHubService iotHub) =>
        {
            var twin = iotHub.GetTwin(deviceId);
            return twin is not null ? Results.Ok(twin) : Results.NotFound();
        })
        .WithSummary("Get device twin")
        .WithDescription("Returns desired and reported properties");

        group.MapPatch("/{deviceId}/twin/desired", (string deviceId, Dictionary<string, object> properties, IotHubService iotHub) =>
        {
            var twin = iotHub.UpdateTwinDesiredProperties(deviceId, properties);
            return twin is not null ? Results.Ok(twin) : Results.NotFound();
        })
        .WithSummary("Update desired properties")
        .WithDescription("Update device configuration - device will sync and apply changes");

        group.MapPatch("/{deviceId}/twin/reported", (string deviceId, Dictionary<string, object> properties, IotHubService iotHub) =>
        {
            var twin = iotHub.UpdateTwinReportedProperties(deviceId, properties);
            return twin is not null ? Results.Ok(twin) : Results.NotFound();
        })
        .WithSummary("Update reported properties")
        .WithDescription("Device reports its current state back to cloud");

        // ===== Direct Methods =====

        group.MapPost("/{deviceId}/methods", async (string deviceId, DirectMethod method, IotHubService iotHub) =>
        {
            var response = await iotHub.InvokeDirectMethodAsync(deviceId, method);
            return Results.Ok(response);
        })
        .WithSummary("Invoke direct method")
        .WithDescription("Send command to device (EmergencyStop, Reboot, GetDiagnostics, etc.)");

        // ===== Telemetry =====

        group.MapGet("/{deviceId}/telemetry", (string deviceId, int count, IotHubService iotHub) =>
        {
            return Results.Ok(iotHub.GetRecentTelemetry(deviceId, count));
        })
        .WithSummary("Get device telemetry")
        .WithDescription("Retrieve recent telemetry messages from the device");

        group.MapPost("/{deviceId}/telemetry", (string deviceId, Dictionary<string, object> data, IotHubService iotHub) =>
        {
            iotHub.IngestTelemetry(new TelemetryMessage
            {
                DeviceId = deviceId,
                Data = data
            });
            return Results.Accepted();
        })
        .WithSummary("Send telemetry")
        .WithDescription("Device sends telemetry to IoT Hub");

        // ===== Provisioning =====

        var provisioningGroup = app.MapGroup("/iot/provisioning")
            .WithTags("Provisioning")
            .WithOpenApi();

        provisioningGroup.MapPost("/", (ProvisioningRequest request, IotHubService iotHub) =>
        {
            var response = iotHub.ProvisionDevice(request);
            return Results.Ok(response);
        })
        .WithSummary("Provision device via DPS")
        .WithDescription("Device Provisioning Service: Auto-provision devices based on registration ID");

        // ===== TPM Attestation =====

        group.MapPost("/{deviceId}/tpm/attest", (string deviceId, TpmAttestation attestation, IotHubService iotHub) =>
        {
            var isValid = iotHub.ValidateTpmAttestation(deviceId, attestation);
            return Results.Ok(new { valid = isValid, deviceId });
        })
        .WithSummary("Validate TPM attestation")
        .WithDescription("Verify hardware-backed device identity using TPM 2.0");

        // ===== Statistics =====

        app.MapGet("/iot/stats", (IotHubService iotHub) =>
        {
            return Results.Ok(iotHub.GetTelemetryStats());
        })
        .WithTags("Statistics")
        .WithSummary("IoT Hub statistics")
        .WithDescription("Overall hub statistics including connected devices");
    }

    public record DeviceCreateRequest(string DeviceId, string? TemplateId);
}
