using InterviewSimulator.Api.Infrastructure.Data;
using InterviewSimulator.Api.Startup;

var builder = WebApplication.CreateBuilder(args);

builder.AddApplicationOptions();
builder.AddApplicationDiagnostics();
builder.AddApplicationServices();
builder.AddApplicationAuthentication();
builder.AddCosmosPersistence();

var app = builder.Build();

app.UseApplicationDiagnostics();
app.UseAuthentication();
app.UseAuthorization();
app.MapApplicationEndpoints();

// Initialize Cosmos DB
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<ICosmosDbInitializer>();
    await initializer.InitializeAsync(app.Lifetime.ApplicationStopping);
}

app.Run();

public partial class Program;
