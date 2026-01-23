using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Exceptions;
using System.Text.Json;
using MedEdge.EdgeGateway.Models;
using Polly;
using System.Threading.Channels;

namespace MedEdge.EdgeGateway.Services;

public class MqttPublisherOptions
{
    public string BrokerHost { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
    public bool UseTls { get; set; } = false;
    public string ClientId { get; set; } = "MedEdge-Gateway";
    public string TopicPrefix { get; set; } = "mededge/dialysis";
}

public class MqttPublisherService : BackgroundService
{
    private readonly ILogger<MqttPublisherService> _logger;
    private readonly MqttPublisherOptions _options;
    private readonly Channel<TelemetryMessage> _telemetryChannel;
    private IMqttClient? _mqttClient;
    private IAsyncPolicy<bool> _publishPolicy = null!;

    public MqttPublisherService(
        ILogger<MqttPublisherService> logger,
        IOptions<MqttPublisherOptions> options,
        Channel<TelemetryMessage> telemetryChannel)
    {
        _logger = logger;
        _options = options.Value;
        _telemetryChannel = telemetryChannel;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        InitializeResiliencePolicy();
        await base.StartAsync(cancellationToken);
    }

    private void InitializeResiliencePolicy()
    {
        var circuitBreakerPolicy = Policy<bool>
            .Handle<MqttCommunicationException>()
            .OrResult(r => !r)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, duration) =>
                {
                    _logger.LogWarning("MQTT circuit breaker opened for {Duration}", duration);
                },
                onReset: () =>
                {
                    _logger.LogInformation("MQTT circuit breaker reset");
                }
            );

        var retryPolicy = Policy<bool>
            .Handle<MqttCommunicationException>()
            .OrResult(r => !r)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (outcome, duration, attempt, _) =>
                {
                    _logger.LogWarning("MQTT publish retry attempt {Attempt} after {Duration}", attempt, duration);
                }
            );

        _publishPolicy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting MQTT publisher service");

        try
        {
            await ConnectAsync(stoppingToken);
            await ConsumeAndPublishTelemetryAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("MQTT publisher service stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in MQTT publisher service");
        }
        finally
        {
            if (_mqttClient?.IsConnected == true)
            {
                await _mqttClient.DisconnectAsync();
            }
            _mqttClient?.Dispose();
        }
    }

    private async Task ConnectAsync(CancellationToken stoppingToken)
    {
        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_options.BrokerHost, _options.Port)
            .WithClientId(_options.ClientId)
            .WithCleanSession(true)
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(60))
            .Build();

        try
        {
            await _mqttClient.ConnectAsync(options, stoppingToken);
            _logger.LogInformation("Connected to MQTT broker at {Host}:{Port}", _options.BrokerHost, _options.Port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to MQTT broker");
            throw;
        }
    }

    private async Task ConsumeAndPublishTelemetryAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _telemetryChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                var topic = $"{_options.TopicPrefix}/{message.DeviceId}/telemetry";
                var payload = JsonSerializer.Serialize(message);

                var success = await _publishPolicy.ExecuteAsync(async () =>
                {
                    if (_mqttClient?.IsConnected != true)
                    {
                        _logger.LogWarning("MQTT client not connected, attempting reconnection");
                        await ConnectAsync(stoppingToken);
                    }

                    var mqttMessage = new MqttApplicationMessageBuilder()
                        .WithTopic(topic)
                        .WithPayload(payload)
                        .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                        .Build();

                    await _mqttClient!.PublishAsync(mqttMessage, stoppingToken);
                    return true;
                });

                if (success)
                {
                    _logger.LogDebug("Published telemetry for {DeviceId} to {Topic}", message.DeviceId, topic);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing telemetry message");
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping MQTT publisher service");
        _telemetryChannel.Writer.TryComplete();
        await base.StopAsync(cancellationToken);
    }
}
