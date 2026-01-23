using MedEdge.Dashboard.Services;

namespace MedEdge.Dashboard.Services;

public class AppConfiguration
{
    public string ApiBaseUrl { get; set; } = "http://localhost:5001";
    public string FhirBaseUrl { get; set; } = "http://localhost:5001";
    public string SignalHubUrl { get; set; } = "http://localhost:5001/hubs/telemetry";
    public bool EnableSignalR { get; set; } = true;
    public bool EnableFhirInspector { get; set; } = true;
    public bool EnableFleetView { get; set; } = true;
    public int RequestTimeout { get; set; } = 30000;
    public int SignalRTimeout { get; set; } = 60000;

    // Computed properties
    public string FhirPatientUrl => $"{FhirBaseUrl}/fhir/Patient";
    public string FhirDeviceUrl => $"{FhirBaseUrl}/fhir/Device";
    public string FhirObservationUrl => $"{FhirBaseUrl}/fhir/Observation";
    public string DevicesApiUrl => $"{ApiBaseUrl}/api/devices";
    public string EmergencyStopUrl => $"{ApiBaseUrl}/api/devices/{{deviceId}}/emergency-stop";
}