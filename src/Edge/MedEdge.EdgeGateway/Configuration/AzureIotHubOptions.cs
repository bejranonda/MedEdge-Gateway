namespace MedEdge.EdgeGateway.Configuration;

public class AzureIotHubOptions
{
    public bool Enabled { get; set; } = false;
    public string DeviceConnectionString { get; set; } = string.Empty;
    public int SendIntervalMs { get; set; } = 1000;
    public string DeviceId { get; set; } = "edge-gateway-facility-001";
}
