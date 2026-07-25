using InterviewSimulator.Api.Infrastructure.Identity;
using InterviewSimulator.Api.Startup;

var builder = WebApplication.CreateBuilder(args);

builder.AddApplicationOptions();
builder.AddApplicationDiagnostics();
builder.AddCosmosPersistence();
builder.AddIdentityServices();
builder.AddApplicationAuthentication();

var app = builder.Build();

app.UseApplicationDiagnostics();
app.UseAuthentication();
app.UseUserProfilePersistence();
app.UseAuthorization();
app.MapApplicationEndpoints();
await app.InitializeCosmosPersistenceAsync();

app.Run();

public partial class Program;
