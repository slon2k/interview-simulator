var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});

var app = builder.Build();
var isDevelopment = app.Environment.IsDevelopment();
const int slowRequestThresholdMs = 500;

if (isDevelopment)
{
    app.MapOpenApi();
}

app.Use(async (context, next) =>
{
    var startedAt = DateTimeOffset.UtcNow;
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    await next();

    stopwatch.Stop();

    var statusCode = context.Response.StatusCode;
    var durationMs = stopwatch.ElapsedMilliseconds;
    var shouldLog = isDevelopment || statusCode >= 400 || durationMs >= slowRequestThresholdMs;

    if (!shouldLog)
    {
        return;
    }

    var logLevel = statusCode >= 500
        ? LogLevel.Error
        : statusCode >= 400
            ? LogLevel.Warning
            : LogLevel.Information;

    app.Logger.Log(
        logLevel,
        "HTTP {Method} {Path} -> {StatusCode} in {DurationMs} ms (traceId: {TraceId}, startedAtUtc: {StartedAtUtc})",
        context.Request.Method,
        context.Request.Path,
        statusCode,
        durationMs,
        context.TraceIdentifier,
        startedAt);
});

app.UseExceptionHandler();
app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;

    if (response.HasStarted || response.StatusCode < 400)
    {
        return;
    }

    var problem = Results.Problem(
        statusCode: response.StatusCode,
        title: "Request failed",
        extensions: new Dictionary<string, object?>
        {
            ["traceId"] = context.HttpContext.TraceIdentifier
        });

    await problem.ExecuteAsync(context.HttpContext);
});

app.UseHttpsRedirection();

app.MapHealthChecks("/api/health").WithName("HealthCheck");

if (!isDevelopment)
{
    app.UseDefaultFiles();
    app.UseStaticFiles();

    // Catch unresolved API routes before SPA fallback.
    app.MapFallback("/api/{**catchAll}", (HttpContext context) =>
    {
        return Results.NotFound(new
        {
            error = "API endpoint not found",
            path = context.Request.Path.Value
        });
    });

    // React SPA fallback.
    app.MapFallbackToFile("index.html");
}

app.Run();

public partial class Program;
