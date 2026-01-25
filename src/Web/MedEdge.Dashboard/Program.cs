using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using MudBlazor.Services;
using MedEdge.Dashboard;
using MedEdge.Dashboard.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Register configuration service
builder.Services.AddSingleton<IAppConfigurationService, AppConfigurationService>();

// Register HttpClient with base address from configuration
builder.Services.AddScoped<HttpClient>(sp =>
{
    var configService = sp.GetRequiredService<IAppConfigurationService>();
    return new HttpClient { BaseAddress = new Uri(configService.Configuration.ApiBaseUrl) };
});

builder.Services.AddMudServices(options =>
{
    options.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    options.ResizeObserverOptions = new ResizeObserverOptions
    {
        EnableLogging = false,
        ReportRate = 100
    };
});

var host = builder.Build();

// Load configuration immediately before running
var configService = host.Services.GetRequiredService<IAppConfigurationService>();
await configService.LoadConfigurationAsync();

await host.RunAsync();
