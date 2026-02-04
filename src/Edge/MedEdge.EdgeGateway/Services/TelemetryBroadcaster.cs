using MedEdge.Core.DTOs;
using System.Threading.Channels;

namespace MedEdge.EdgeGateway.Services;

/// <summary>
/// Broadcasts telemetry messages to multiple subscribers (MQTT and Azure IoT Hub).
/// This allows both publishers to receive the same telemetry data.
/// </summary>
public class TelemetryBroadcaster
{
    private readonly List<Channel<TelemetryMessage>> _subscribers = new();
    private readonly object _lock = new();

    /// <summary>
    /// Creates a new subscriber channel and returns it.
    /// The channel will receive copies of all telemetry messages.
    /// </summary>
    public Channel<TelemetryMessage> Subscribe()
    {
        var channel = Channel.CreateUnbounded<TelemetryMessage>();
        lock (_lock)
        {
            _subscribers.Add(channel);
        }
        return channel;
    }

    /// <summary>
    /// Broadcasts a telemetry message to all subscribers.
    /// </summary>
    public async Task BroadcastAsync(TelemetryMessage message, CancellationToken cancellationToken = default)
    {
        List<Channel<TelemetryMessage>> currentSubscribers;
        lock (_lock)
        {
            currentSubscribers = _subscribers.ToList();
        }

        foreach (var subscriber in currentSubscribers)
        {
            try
            {
                await subscriber.Writer.WriteAsync(message, cancellationToken);
            }
            catch (ChannelClosedException)
            {
                // Subscriber has been disposed, remove it
                lock (_lock)
                {
                    _subscribers.Remove(subscriber);
                }
            }
        }
    }

    /// <summary>
    /// Completes all subscriber channels.
    /// </summary>
    public void Complete()
    {
        lock (_lock)
        {
            foreach (var subscriber in _subscribers)
            {
                subscriber.Writer.TryComplete();
            }
        }
    }
}
