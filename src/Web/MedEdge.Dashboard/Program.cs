using MudBlazor.Services;
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

builder.Services.AddMudServices();

var host = builder.Build();

// Load configuration immediately before running
var configService = host.Services.GetRequiredService<IAppConfigurationService>();
await configService.LoadConfigurationAsync();

await host.RunAsync();
