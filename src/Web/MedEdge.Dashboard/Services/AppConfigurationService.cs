using Microsoft.JSInterop;

namespace MedEdge.Dashboard.Services;

public class AppConfigurationService : IAppConfigurationService
{
    private readonly IJSRuntime _jsRuntime;
    private AppConfiguration? _configuration;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public AppConfigurationService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        _configuration = new AppConfiguration(); // Fallback defaults
    }

    public AppConfiguration Configuration => _configuration ?? new AppConfiguration();

    public async Task<AppConfiguration> LoadConfigurationAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_configuration != null)
                return _configuration;

            // Try to load from window.MedEdgeConfig
            var config = await _jsRuntime.InvokeAsync<AppConfiguration>("eval",
                "() => { " +
                "  if (window.MedEdgeConfig) { " +
                "    return window.MedEdgeConfig; " +
                "  } " +
                "  return { " +
                "    apiBaseUrl: window.location.origin, " +
                "    fhirBaseUrl: window.location.origin, " +
                "    signalHubUrl: window.location.origin + '/hubs/telemetry', " +
                "    enableSignalR: true, " +
                "    enableFhirInspector: true, " +
                "    enableFleetView: true, " +
                "    requestTimeout: 30000, " +
                "    signalRTimeout: 60000 " +
                "  }; " +
                "}");

            _configuration = config;
            return _configuration;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}