using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using System.Text.Json;
using MedEdge.EdgeGateway.Models;
using System.Threading.Channels;

namespace MedEdge.TransformService.Services;

public class MqttSubscriberOptions
{
    public string BrokerHost { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
    public string TopicPrefix { get; set; } = "mededge/dialysis";
}

public class MqttSubscriberService : BackgroundService
{
    private readonly ILogger<MqttSubscriberService> _logger;
    private readonly MqttSubscriberOptions _options;
    private readonly Channel<TelemetryMessage> _telemetryChannel;
    private IMqttClient? _mqttClient;

    public MqttSubscriberService(
        ILogger<MqttSubscriberService> logger,
        IOptions<MqttSubscriberOptions> options,
        Channel<TelemetryMessage> telemetryChannel)
    {
        _logger = logger;
        _options = options.Value;
        _telemetryChannel = telemetryChannel;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting MQTT subscriber service");

        try
        {
            await SubscribeAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("MQTT subscriber service stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in MQTT subscriber service");
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

    private async Task SubscribeAsync(CancellationToken stoppingToken)
    {
        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_options.BrokerHost, _options.Port)
            .WithClientId("MedEdge-TransformService")
            .WithCleanSession(true)
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

        // Subscribe to all telemetry topics
        var topic = $"{_options.TopicPrefix}/+/telemetry";
        var subscriptionFilter = new MqttTopicFilterBuilder()
            .WithTopic(topic)
            .Build();

        await _mqttClient.SubscribeAsync(subscriptionFilter);
        _logger.LogInformation("Subscribed to topic: {Topic}", topic);

        _mqttClient.ApplicationMessageReceivedAsync += async e =>
        {
            try
            {
                var payload = e.ApplicationMessage.ConvertPayloadToString();
                var message = JsonSerializer.Deserialize<TelemetryMessage>(payload);

                if (message != null)
                {
                    await _telemetryChannel.Writer.WriteAsync(message);
                    _logger.LogDebug("Received telemetry from {DeviceId}", message.DeviceId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MQTT message");
            }
        };

        // Keep subscription alive
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping MQTT subscriber service");
        _telemetryChannel.Writer.TryComplete();
        await base.StopAsync(cancellationToken);
    }
}
