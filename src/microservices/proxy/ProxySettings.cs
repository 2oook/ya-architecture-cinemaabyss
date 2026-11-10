namespace ProxyService;

public sealed record ProxySettings(
    string Port,
    string MonolithUrl,
    string MoviesServiceUrl,
    string EventsServiceUrl,
    bool GradualMigration,
    int MoviesMigrationPercent)
{
    public static ProxySettings FromEnvironment()
    {
        var percentRaw = Env("MOVIES_MIGRATION_PERCENT", "0");
        if (!int.TryParse(percentRaw, out var percent) || percent is < 0 or > 100)
        {
            throw new InvalidOperationException(
                $"MOVIES_MIGRATION_PERCENT must be an integer from 0 to 100, got '{percentRaw}'");
        }

        return new ProxySettings(
            Port: Env("PORT", "8000"),
            MonolithUrl: Env("MONOLITH_URL", "http://localhost:8080"),
            MoviesServiceUrl: Env("MOVIES_SERVICE_URL", "http://localhost:8081"),
            EventsServiceUrl: Env("EVENTS_SERVICE_URL", "http://localhost:8082"),
            GradualMigration: bool.TryParse(Env("GRADUAL_MIGRATION", "false"), out var gradual) && gradual,
            MoviesMigrationPercent: percent);
    }

    private static string Env(string name, string defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }
}
