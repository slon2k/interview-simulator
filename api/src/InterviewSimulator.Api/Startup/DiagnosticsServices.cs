using InterviewSimulator.Api.Features.Common;

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

using Scalar.AspNetCore;

namespace InterviewSimulator.Api.Startup;

public static class DiagnosticsServices
{
    public const int DefaultSlowRequestThresholdMs = 500;

    public static WebApplicationBuilder AddDiagnosticsServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenApi(options =>
        {
            options.AddSchemaTransformer((schema, context, ct) =>
            {
                if (schema.Format == "int32" && schema.Type is { } type && type.HasFlag(JsonSchemaType.String))
                {
                    schema.Type = type & ~JsonSchemaType.String;
                    schema.Pattern = null;
                }

                return Task.CompletedTask;
            });
            // Prefix nested types with their declaring class name to avoid schema name collisions.
            options.CreateSchemaReferenceId = typeInfo =>
            {
                var type = typeInfo.Type;
                if (type.IsNested && type.DeclaringType is { } parent)
                {
                    return $"{parent.Name}{type.Name}";
                }

                if (type.IsPrimitive
                    || type.IsGenericType
                    || type == typeof(DateOnly)
                    || type == typeof(TimeOnly)
                    || type == typeof(string)
                    || type == typeof(decimal)
                    || type == typeof(DateTime)
                    || type == typeof(DateTimeOffset)
                    || type == typeof(Guid)
                    || type == typeof(TimeSpan)
                    )
                {
                    return null;
                }

                return OpenApiOptions.CreateDefaultSchemaReferenceId(typeInfo);
            };
        });
        builder.Services.AddHealthChecks();
        builder.Services.AddExceptionHandler<DomainExceptionHandler>();
        builder.Services.AddExceptionHandler<InfrastructureExceptionHandler>();
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
        int slowRequestThresholdMs = DefaultSlowRequestThresholdMs)
    {
        bool isDevelopment = app.Environment.IsDevelopment();

        if (isDevelopment)
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
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