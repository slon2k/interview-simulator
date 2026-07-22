using InterviewSimulator.Api.Features.Auth;

namespace InterviewSimulator.Api.Startup;

public static class EndpointsMapping
{
    public static WebApplication MapApplicationEndpoints(this WebApplication app)
    {
        app.MapAuthenticationEndpoints();

        if (!app.Environment.IsDevelopment())
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

        return app;
    }
}