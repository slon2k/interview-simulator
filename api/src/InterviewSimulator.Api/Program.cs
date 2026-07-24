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
await app.InitializeCosmosPersistenceAsync();

app.Run();

public partial class Program;
