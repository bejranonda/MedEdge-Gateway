using MedEdge.Core.DTOs;
using MedEdge.AnalyticsService.Services;
using MedEdge.FhirApi.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/analytics-service-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=medEdge.db";
    options.UseSqlite(connectionString);
});

// Add services
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

// Add background service for daily metric generation
builder.Services.AddHostedService<DailyMetricGenerator>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Apply migrations (or create database if no migrations exist)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    // TODO: Replace with MigrateAsync() after creating proper migrations
    await context.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseSerilogRequestLogging();

// Analytics Endpoints

// GET /api/analytics/summary/{areaId?} - Get latest metrics summary
app.MapGet("/api/analytics/summary", async (
    string? areaId,
    string? stationId,
    IAnalyticsService service) =>
{
    var metrics = await service.GetLatestMetricsAsync(areaId, stationId);
    if (metrics == null)
        return Results.NotFound(new { error = "No metrics found" });
    return Results.Ok(metrics);
})
.WithName("GetAnalyticsSummary")
.WithOpenApi();

// GET /api/analytics/trends - Get treatment trends
app.MapGet("/api/analytics/trends", async (
    IAnalyticsService service,
    int days = 30) =>
{
    if (days < 1 || days > 365)
        return Results.BadRequest(new { error = "Days must be between 1 and 365" });

    var trends = await service.GetTreatmentTrendsAsync(days);
    return Results.Ok(trends);
})
.WithName("GetTreatmentTrends")
.WithOpenApi();

// GET /api/analytics/station-performance - Get station performance
app.MapGet("/api/analytics/station-performance", async (
    IAnalyticsService service,
    string stationId,
    int days = 30) =>
{
    if (string.IsNullOrEmpty(stationId))
        return Results.BadRequest(new { error = "Station ID is required" });

    var performance = await service.GetStationPerformanceAsync(stationId, days);
    return Results.Ok(performance);
})
.WithName("GetStationPerformance")
.WithOpenApi();

// GET /api/analytics/area-comparison - Compare area performance
app.MapGet("/api/analytics/area-comparison", async (
    IAnalyticsService service,
    DateTime? date) =>
{
    var targetDate = date.HasValue ? DateOnly.FromDateTime(date.Value) : DateOnly.FromDateTime(DateTime.UtcNow);
    var comparison = await service.GetAreaComparisonAsync(targetDate);
    return Results.Ok(comparison);
})
.WithName("GetAreaComparison")
.WithOpenApi();

// POST /api/analytics/generate - Trigger metric generation
app.MapPost("/api/analytics/generate", async (
    DateOnly? date,
    IAnalyticsService service,
    ILogger<Program> logger) =>
{
    var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
    await service.GenerateDailyMetricsAsync(targetDate);
    logger.LogInformation("Generated metrics for {Date}", targetDate);
    return Results.Ok(new
    {
        date = targetDate,
        status = "generated",
        timestamp = DateTime.UtcNow
    });
})
.WithName("GenerateMetrics")
.WithOpenApi();

// Health check
app.MapGet("/health", () => Results.Ok(new
{
    service = "AnalyticsService",
    status = "healthy",
    timestamp = DateTime.UtcNow
}))
.WithName("HealthCheck");

try
{
    Log.Information("Starting Analytics Service");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Analytics Service failed to start");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Background service for daily metric generation.
/// </summary>
public class DailyMetricGenerator : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DailyMetricGenerator> _logger;

    public DailyMetricGenerator(IServiceProvider serviceProvider, ILogger<DailyMetricGenerator> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run once on startup for yesterday's data
        await GenerateMetricsForYesterday();

        // Schedule daily runs at 1 AM
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRun = new DateTime(now.Year, now.Month, now.Day, 1, 0, 0, DateTimeKind.Utc).AddDays(1);
            var delay = nextRun - now;

            _logger.LogInformation("Next metric generation scheduled for {NextRun}", nextRun);

            await Task.Delay(delay, stoppingToken);

            if (!stoppingToken.IsCancellationRequested)
            {
                await GenerateMetricsForYesterday();
            }
        }
    }

    private async Task GenerateMetricsForYesterday()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();
            var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
            await service.GenerateDailyMetricsAsync(yesterday);
            _logger.LogInformation("Daily metrics generated for {Date}", yesterday);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate daily metrics");
        }
    }
}
