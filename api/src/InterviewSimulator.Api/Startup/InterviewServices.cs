using InterviewSimulator.Api.Features.Interviews;

namespace InterviewSimulator.Api.Startup;

public static class InterviewServices
{
    public static WebApplicationBuilder AddInterviewServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IQuestionGenerator, HardcodedQuestionGenerator>();

        return builder;
    }
}
