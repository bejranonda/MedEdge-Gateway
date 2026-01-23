using MedEdge.IotHubSimulator.Endpoints;
using MedEdge.IotHubSimulator.Services;
using Serilog;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Serilog configuration
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddSerilog();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "MedEdge IoT Hub Simulator",
        Version = "v1",
        Description = """
        ## Simulated Azure IoT Hub for Medical Device Connectivity

        This service demonstrates Azure IoT Hub patterns for medical device connectivity:

        ### Key Concepts
        - **Device Registry**: Identity management for medical devices
        - **Device Twins**: Desired/reported property synchronization
        - **Direct Methods**: Cloud-to-device command execution
        - **Telemetry**: Device-to-cloud data ingestion
        - **DPS**: Device Provisioning Service patterns
        - **TPM Attestation**: Hardware-backed device identity

        ### Azure IoT Hub Mapping
        | This Simulator | Azure IoT Hub |
        |----------------|---------------|
        | `/iot/devices` | Device Registry API |
        | `/iot/devices/{id}/twin` | Device Twin API |
        | `/iot/devices/{id}/methods` | Direct Methods API |
        | `/iot/provisioning` | Device Provisioning Service |
        | `/iot/devices/{id}/tpm/attest` | Azure Device Provisioning with TPM |

        ### Demo Flow
        1. **Registry**: Show pre-populated medical devices
        2. **Provisioning**: Demonstrate DPS pattern with TPM attestation
        3. **Twins**: Update desired properties, show synchronization
        4. **Methods**: Invoke EmergencyStop, GetDiagnostics
        5. **Telemetry**: Send medical device data to cloud
        """
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddSingleton<IotHubService>();
builder.Services.AddSingleton<TpmSimulationService>();
builder.Services.AddSingleton<JwtAuthenticationService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "MedEdge IoT Hub Simulator v1");
    options.RoutePrefix = string.Empty; // Serve at root
});

app.UseCors();
app.UseSerilogRequestLogging();

app.MapIotHubEndpoints();
app.MapSecurityEndpoints();

// Health check
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "MedEdge IoT Hub Simulator",
    timestamp = DateTime.UtcNow,
    azureMapping = "Simulates Azure IoT Hub REST API"
}))
.WithTags("Health")
.WithOpenApi();

Log.Information("MedEdge IoT Hub Simulator starting...");
Log.Information("Swagger UI: http://localhost:6000");
Log.Information("");
Log.Information("╔════════════════════════════════════════════════════════════════╗");
Log.Information("║  AZURE IOT HUB SIMULATOR - Medical Device Connectivity        ║");
Log.Information("╠════════════════════════════════════════════════════════════════╣");
Log.Information("║  Demonstrates:                                               ║");
Log.Information("║  • Device Registry & Identity Management                     ║");
Log.Information("║  • Device Twins (Desired/Reported Properties)                ║");
Log.Information("║  • Direct Methods (Cloud-to-Device Commands)                 ║");
Log.Information("║  • Telemetry Ingestion (Device-to-Cloud Data)                ║");
Log.Information("║  • Device Provisioning Service (DPS) Patterns                ║");
Log.Information("║  • TPM 2.0 Hardware Security Attestation                     ║");
Log.Information("╠════════════════════════════════════════════════════════════════╣");
Log.Information("║  For Medical Devices:                                        ║");
Log.Information("║  • Legacy Device Integration Pattern                         ║");
Log.Information("║  • Modern Device with TPM Support                            ║");
Log.Information("║  • Azure IoT Hub Architecture without Azure Account          ║");
Log.Information("╚════════════════════════════════════════════════════════════════╝");

app.Run($"http://0.0.0.0:8080");
