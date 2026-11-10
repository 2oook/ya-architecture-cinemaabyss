using System.Diagnostics;
using System.Net;
using ProxyService;
using Yarp.ReverseProxy.Forwarder;

var settings = ProxySettings.FromEnvironment();

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls($"http://0.0.0.0:{settings.Port}");
builder.Services.AddSingleton(settings);
builder.Services.AddSingleton<UpstreamRouter>();
builder.Services.AddHttpForwarder();

var app = builder.Build();

app.Logger.LogInformation(
    "Proxy started: monolith={Monolith}, movies={Movies}, events={Events}, gradualMigration={Gradual}, moviesMigrationPercent={Percent}",
    settings.MonolithUrl, settings.MoviesServiceUrl, settings.EventsServiceUrl,
    settings.GradualMigration, settings.MoviesMigrationPercent);

var httpClient = new HttpMessageInvoker(new SocketsHttpHandler
{
    UseProxy = false,
    AllowAutoRedirect = false,
    AutomaticDecompression = DecompressionMethods.None,
    UseCookies = false,
    ActivityHeadersPropagator = new ReverseProxyPropagator(DistributedContextPropagator.Current),
    ConnectTimeout = TimeSpan.FromSeconds(15),
});
var requestConfig = new ForwarderRequestConfig { ActivityTimeout = TimeSpan.FromSeconds(100) };

app.MapGet("/health", () => Results.Json(new { status = true }));

app.Map("/{**catch-all}", async (
    HttpContext context,
    IHttpForwarder forwarder,
    UpstreamRouter router,
    ILogger<Program> logger) =>
{
    var upstream = router.Select(context.Request.Path);
    logger.LogInformation("{Method} {Path}{Query} -> {Upstream}",
        context.Request.Method, context.Request.Path, context.Request.QueryString, upstream.Name);

    var error = await forwarder.SendAsync(
        context, upstream.Url, httpClient, requestConfig, new UpstreamHeaderTransformer(upstream.Name));

    if (error != ForwarderError.None)
    {
        var exception = context.GetForwarderErrorFeature()?.Exception;
        logger.LogError(exception, "Forwarding to {Upstream} failed: {Error}", upstream.Name, error);
    }
});

app.Run();

/// <summary>Adds X-Upstream to every proxied response to show where the request went.</summary>
internal sealed class UpstreamHeaderTransformer(string upstreamName) : HttpTransformer
{
    public override async ValueTask<bool> TransformResponseAsync(
        HttpContext httpContext, HttpResponseMessage? proxyResponse, CancellationToken cancellationToken)
    {
        var result = await base.TransformResponseAsync(httpContext, proxyResponse, cancellationToken);
        httpContext.Response.Headers["X-Upstream"] = upstreamName;
        return result;
    }
}
