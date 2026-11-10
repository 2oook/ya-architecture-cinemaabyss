using System.Text.Json;
using Confluent.Kafka;

namespace EventsService;

public sealed class EventProducer : IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<EventProducer> _logger;

    public EventProducer(KafkaSettings settings, ILogger<EventProducer> logger)
    {
        _logger = logger;
        _producer = new ProducerBuilder<string, string>(new ProducerConfig
            {
                BootstrapServers = settings.Brokers,
                Acks = Acks.All,
                // Fail the HTTP request instead of hanging if Kafka is unavailable.
                MessageTimeoutMs = 10_000,
            })
            .SetErrorHandler((_, error) => _logger.LogWarning("Kafka producer error: {Reason}", error.Reason))
            .Build();
    }

    /// <summary>
    /// Sends the event and waits for the broker acknowledgement.
    /// The entity id is used as the key so events of one entity keep their order.
    /// </summary>
    public async Task<DeliveryResult<string, string>> ProduceAsync(
        string topic, string key, Event @event, CancellationToken cancellationToken)
    {
        var message = new Message<string, string>
        {
            Key = key,
            Value = JsonSerializer.Serialize(@event, KafkaSettings.JsonOptions),
        };

        var result = await _producer.ProduceAsync(topic, message, cancellationToken);
        _logger.LogInformation("Produced {Type} event {Id} to {Topic} [partition {Partition}, offset {Offset}]",
            @event.Type, @event.Id, result.Topic, result.Partition.Value, result.Offset.Value);
        return result;
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
