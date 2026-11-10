using Confluent.Kafka;
using EventsService;

var port = Environment.GetEnvironmentVariable("PORT");
var kafkaSettings = KafkaSettings.FromEnvironment();

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls($"http://0.0.0.0:{(string.IsNullOrWhiteSpace(port) ? "8082" : port)}");
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = KafkaSettings.JsonOptions.PropertyNamingPolicy;
    options.SerializerOptions.DefaultIgnoreCondition = KafkaSettings.JsonOptions.DefaultIgnoreCondition;
});
builder.Services.AddSingleton(kafkaSettings);
builder.Services.AddSingleton<EventProducer>();
builder.Services.AddHostedService<EventConsumer>();

var app = builder.Build();
app.Logger.LogInformation("Events service uses Kafka brokers {Brokers}", kafkaSettings.Brokers);

app.MapGet("/api/events/health", () => Results.Json(new { status = true }));

app.MapPost("/api/events/movie", (MovieEventRequest request, EventProducer producer, CancellationToken ct) =>
    request.Validate() is { } error
        ? BadRequest(error)
        : Publish(producer, KafkaSettings.MovieTopic, request.MovieId!.Value,
            Event.Create("movie", request.MovieId!.Value, request.Action!, DateTimeOffset.UtcNow, request), ct));

app.MapPost("/api/events/user", (UserEventRequest request, EventProducer producer, CancellationToken ct) =>
    request.Validate() is { } error
        ? BadRequest(error)
        : Publish(producer, KafkaSettings.UserTopic, request.UserId!.Value,
            Event.Create("user", request.UserId!.Value, request.Action!, request.Timestamp!.Value, request), ct));

app.MapPost("/api/events/payment", (PaymentEventRequest request, EventProducer producer, CancellationToken ct) =>
    request.Validate() is { } error
        ? BadRequest(error)
        : Publish(producer, KafkaSettings.PaymentTopic, request.PaymentId!.Value,
            Event.Create("payment", request.PaymentId!.Value, request.Status!, request.Timestamp!.Value, request), ct));

app.Run();

static Task<IResult> BadRequest(string error) =>
    Task.FromResult(Results.Json(new ErrorResponse(error), statusCode: StatusCodes.Status400BadRequest));

static async Task<IResult> Publish(
    EventProducer producer, string topic, int entityId, Event @event, CancellationToken ct)
{
    try
    {
        var result = await producer.ProduceAsync(topic, entityId.ToString(), @event, ct);
        return Results.Json(
            new EventResponse("success", result.Partition.Value, result.Offset.Value, @event),
            statusCode: StatusCodes.Status201Created);
    }
    catch (ProduceException<string, string> ex)
    {
        return Results.Json(new ErrorResponse($"Failed to publish event: {ex.Error.Reason}"),
            statusCode: StatusCodes.Status500InternalServerError);
    }
}
