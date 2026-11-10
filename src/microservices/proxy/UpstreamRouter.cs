namespace ProxyService;

public sealed record Upstream(string Name, string Url);

/// <summary>
/// Strangler Fig routing: /api/movies is gradually moved from the monolith
/// to the movies service, /api/events goes to the events service,
/// everything else stays in the monolith.
/// </summary>
public sealed class UpstreamRouter(ProxySettings settings)
{
    private readonly Upstream _monolith = new("monolith", settings.MonolithUrl);
    private readonly Upstream _movies = new("movies", settings.MoviesServiceUrl);
    private readonly Upstream _events = new("events", settings.EventsServiceUrl);

    public Upstream Select(PathString path)
    {
        if (path.StartsWithSegments("/api/movies"))
        {
            return ShouldMigrateMovies() ? _movies : _monolith;
        }

        if (path.StartsWithSegments("/api/events"))
        {
            return _events;
        }

        return _monolith;
    }

    // Next(0, 100) returns 0..99, so 0% never migrates and 100% always does.
    private bool ShouldMigrateMovies() =>
        settings.GradualMigration && Random.Shared.Next(0, 100) < settings.MoviesMigrationPercent;
}
