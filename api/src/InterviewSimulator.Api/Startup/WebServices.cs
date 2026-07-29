using InterviewSimulator.Api.Features.Auth;
using InterviewSimulator.Api.Features.Identity;
using InterviewSimulator.Api.Features.Interviews;

namespace InterviewSimulator.Api.Startup;

public static class WebServices
{
    public static WebApplication AddWebServices(this WebApplication app)
    {
        app.MapAuthenticationEndpoints();
        app.MapIdentityEndpoints();
        app.MapInterviewEndpoints();

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