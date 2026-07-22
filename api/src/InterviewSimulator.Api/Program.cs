using InterviewSimulator.Api;
using InterviewSimulator.Api.Startup;

using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<AzureSpeechOptions>()
    .Bind(builder.Configuration.GetSection(AzureSpeechOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddSingleton<IValidateOptions<AzureSpeechOptions>, AzureSpeechOptionsValidator>();

builder.Services.AddOptions<AzureOpenAIOptions>()
    .Bind(builder.Configuration.GetSection(AzureOpenAIOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddSingleton<IValidateOptions<AzureOpenAIOptions>, AzureOpenAIOptionsValidator>();

builder.AddApplicationDiagnostics();

builder.AddApplicationAuthentication();

var app = builder.Build();
const int slowRequestThresholdMs = 500;

app.UseApplicationDiagnostics(slowRequestThresholdMs);
app.UseAuthentication();
app.UseAuthorization();
app.MapApplicationEndpoints();

app.Run();

public partial class Program;
