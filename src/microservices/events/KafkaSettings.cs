using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventsService;

public sealed record KafkaSettings(string Brokers)
{
    public const string MovieTopic = "movie-events";
    public const string UserTopic = "user-events";
    public const string PaymentTopic = "payment-events";

    public static readonly string[] AllTopics = [MovieTopic, UserTopic, PaymentTopic];

    public const string ConsumerGroup = "events-service";

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static KafkaSettings FromEnvironment()
    {
        var brokers = Environment.GetEnvironmentVariable("KAFKA_BROKERS");
        return new KafkaSettings(string.IsNullOrWhiteSpace(brokers) ? "localhost:9092" : brokers);
    }
}
