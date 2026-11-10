namespace EventsService;

// Request models follow api-specification.yaml. Fields are nullable
// so that missing required fields can be reported as 400.

public sealed record MovieEventRequest(
    int? MovieId,
    string? Title,
    string? Action,
    int? UserId,
    double? Rating,
    string[]? Genres,
    string? Description)
{
    public string? Validate() =>
        MovieId is null ? "movie_id is required"
        : string.IsNullOrWhiteSpace(Title) ? "title is required"
        : string.IsNullOrWhiteSpace(Action) ? "action is required"
        : null;
}

public sealed record UserEventRequest(
    int? UserId,
    string? Username,
    string? Email,
    string? Action,
    DateTimeOffset? Timestamp)
{
    public string? Validate() =>
        UserId is null ? "user_id is required"
        : string.IsNullOrWhiteSpace(Action) ? "action is required"
        : Timestamp is null ? "timestamp is required"
        : null;
}

public sealed record PaymentEventRequest(
    int? PaymentId,
    int? UserId,
    double? Amount,
    string? Status,
    DateTimeOffset? Timestamp,
    string? MethodType)
{
    public string? Validate() =>
        PaymentId is null ? "payment_id is required"
        : UserId is null ? "user_id is required"
        : Amount is null ? "amount is required"
        : string.IsNullOrWhiteSpace(Status) ? "status is required"
        : Timestamp is null ? "timestamp is required"
        : null;
}

/// <summary>Envelope that is written to Kafka and returned to the client.</summary>
public sealed record Event(string Id, string Type, DateTimeOffset Timestamp, object Payload)
{
    // Spec example is "movie-1-viewed"; a Unix-ms suffix makes the id unique.
    public static Event Create(string type, int entityId, string action, DateTimeOffset timestamp, object payload) =>
        new($"{type}-{entityId}-{action}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}", type, timestamp, payload);
}

public sealed record EventResponse(string Status, int Partition, long Offset, Event Event);

public sealed record ErrorResponse(string Error);
