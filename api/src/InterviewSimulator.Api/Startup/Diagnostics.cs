namespace InterviewSimulator.Api.Startup;

public static class Diagnostics
{
    public static WebApplicationBuilder AddApplicationDiagnostics(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenApi();
        builder.Services.AddHealthChecks();
        builder.Services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
            };
        });

        return builder;
    }

    public static WebApplication UseApplicationDiagnostics(
        this WebApplication app,
        int slowRequestThresholdMs)
    {
        bool isDevelopment = app.Environment.IsDevelopment();

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

        return app;
    }
}