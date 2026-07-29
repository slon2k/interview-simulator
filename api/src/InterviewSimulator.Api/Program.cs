using InterviewSimulator.Api.Infrastructure.Identity;
using InterviewSimulator.Api.Startup;

var builder = WebApplication.CreateBuilder(args);

builder.AddSpeechServices();
builder.AddDiagnosticsServices();
builder.AddInterviewServices();
builder.AddOpenAIServices();
builder.AddPersistenceServices();
builder.AddIdentityServices();
builder.AddAuthenticationServices();

var app = builder.Build();

app.UseApplicationDiagnostics();
app.UseAuthentication();
app.UseUserProfilePersistence();
app.UseAuthorization();
app.AddWebServices();
await app.InitializeCosmosPersistenceAsync();

app.Run();

public partial class Program;
