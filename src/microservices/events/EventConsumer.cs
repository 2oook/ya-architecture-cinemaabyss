using Confluent.Kafka;

namespace EventsService;

/// <summary>Reads events from all topics and writes them to the log.</summary>
public sealed class EventConsumer(KafkaSettings settings, ILogger<EventConsumer> logger) : BackgroundService
{
    // Consume() blocks, so the loop runs on a separate thread.
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);

    private void ConsumeLoop(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = settings.Brokers,
            GroupId = KafkaSettings.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
        };

        // librdkafka reconnects on its own, so broker errors are only logged.
        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, error) => logger.LogWarning("Kafka consumer error: {Reason}", error.Reason))
            .Build();

        consumer.Subscribe(KafkaSettings.AllTopics);
        logger.LogInformation("Consumer subscribed to {Topics}", string.Join(", ", KafkaSettings.AllTopics));

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);
                    logger.LogInformation(
                        "Processed event from {Topic} [partition {Partition}, offset {Offset}], key {Key}: {Value}",
                        result.Topic, result.Partition.Value, result.Offset.Value, result.Message.Key, result.Message.Value);
                }
                catch (ConsumeException ex) when (!ex.Error.IsFatal)
                {
                    // For example, a topic is not created yet while Kafka is starting.
                    logger.LogWarning("Consume error: {Reason}. Retrying", ex.Error.Reason);
                    stoppingToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(2));
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
        finally
        {
            consumer.Close();
        }
    }
}
