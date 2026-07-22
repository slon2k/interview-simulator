using InterviewSimulator.Api.Startup;

var builder = WebApplication.CreateBuilder(args);

builder.AddApplicationOptions();
builder.AddApplicationDiagnostics();
builder.AddApplicationServices();
builder.AddApplicationAuthentication();

var app = builder.Build();

app.UseApplicationDiagnostics();
app.UseAuthentication();
app.UseAuthorization();
app.MapApplicationEndpoints();

app.Run();

public partial class Program;
