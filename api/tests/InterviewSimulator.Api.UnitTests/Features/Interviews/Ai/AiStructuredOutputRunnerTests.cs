using FluentValidation;
using FluentValidation.Results;
using InterviewSimulator.Api.Features.Interviews.Ai;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace InterviewSimulator.Api.UnitTests.Features.Interviews.Ai;

public sealed class AiStructuredOutputRunner_RunAsync
{
    [Fact]
    public async Task RunAsync_WithValidJsonAndValidatorSuccess_ReturnsResponse()
    {
        var runner = CreateRunner(new AiOptions(), new SuccessValidator<TestOutput>());
        var context = CreateContext();
        var metadata = CreateMetadata();

        var result = await runner.RunAsync(
            context,
            _ => Task.FromResult(new AiRawResponse("{\"value\":\"ok\"}", metadata)),
            CancellationToken.None);

        Assert.Equal("ok", result.Value.Value);
        Assert.Equal(metadata, result.Metadata);
    }

    [Fact]
    public async Task RunAsync_WithMalformedJsonThenValidJson_RetriesAndSucceeds()
    {
        var runner = CreateRunner(
            new AiOptions { InvalidOutputRetryCount = 1, TransientRetryCount = 0 },
            new SuccessValidator<TestOutput>());
        var context = CreateContext();
        var metadata = CreateMetadata();
        var attempts = 0;

        var result = await runner.RunAsync(
            context,
            _ =>
            {
                attempts++;

                return attempts == 1
                    ? Task.FromResult(new AiRawResponse("{not-json", metadata))
                    : Task.FromResult(new AiRawResponse("{\"value\":\"recovered\"}", metadata));
            },
            CancellationToken.None);

        Assert.Equal(2, attempts);
        Assert.Equal("recovered", result.Value.Value);
    }

    [Fact]
    public async Task RunAsync_WithMalformedJsonTwice_ThrowsAiInvalidResponseException()
    {
        var runner = CreateRunner(
            new AiOptions { InvalidOutputRetryCount = 1, TransientRetryCount = 0 },
            new SuccessValidator<TestOutput>());
        var context = CreateContext();
        var metadata = CreateMetadata();
        var attempts = 0;

        var exception = await Assert.ThrowsAsync<AiInvalidResponseException>(() =>
            runner.RunAsync(
                context,
                _ =>
                {
                    attempts++;
                    return Task.FromResult(new AiRawResponse("{bad-json", metadata));
                },
                CancellationToken.None));

        Assert.Equal(2, attempts);
        Assert.Equal("AI response was not valid JSON.", exception.Reason);
    }

    [Fact]
    public async Task RunAsync_WithValidatorErrorsTwice_ThrowsAiInvalidResponseException()
    {
        var runner = CreateRunner(
            new AiOptions { InvalidOutputRetryCount = 1, TransientRetryCount = 0 },
            new ErrorValidator<TestOutput>("invalid output"));
        var context = CreateContext();
        var metadata = CreateMetadata();
        var attempts = 0;

        var exception = await Assert.ThrowsAsync<AiInvalidResponseException>(() =>
            runner.RunAsync(
                context,
                _ =>
                {
                    attempts++;
                    return Task.FromResult(new AiRawResponse("{\"value\":\"bad\"}", metadata));
                },
                CancellationToken.None));

        Assert.Equal(2, attempts);
        Assert.Contains("invalid output", exception.ValidationErrors);
    }

    [Fact]
    public async Task RunAsync_WithTransientFailureThenSuccess_RetriesAndSucceeds()
    {
        var runner = CreateRunner(
            new AiOptions { InvalidOutputRetryCount = 0, TransientRetryCount = 1 },
            new SuccessValidator<TestOutput>());
        var context = CreateContext();
        var metadata = CreateMetadata();
        var attempts = 0;

        var result = await runner.RunAsync(
            context,
            _ =>
            {
                attempts++;

                if (attempts == 1)
                {
                    throw new AiProviderTransientException(context, "temporary failure");
                }

                return Task.FromResult(new AiRawResponse("{\"value\":\"ok\"}", metadata));
            },
            CancellationToken.None);

        Assert.Equal(2, attempts);
        Assert.Equal("ok", result.Value.Value);
    }

    [Fact]
    public async Task RunAsync_WithTransientFailureTwice_ThrowsAiProviderUnavailableException()
    {
        var runner = CreateRunner(
            new AiOptions { InvalidOutputRetryCount = 0, TransientRetryCount = 1 },
            new SuccessValidator<TestOutput>());
        var context = CreateContext();
        var attempts = 0;

        var exception = await Assert.ThrowsAsync<AiProviderUnavailableException>(() =>
            runner.RunAsync(
                context,
                _ =>
                {
                    attempts++;
                    throw new AiProviderTransientException(context, "temporary failure");
                },
                CancellationToken.None));

        Assert.Equal(2, attempts);
        Assert.Equal("AI provider was unavailable after retry attempts.", exception.Reason);
    }

    [Fact]
    public async Task RunAsync_WithOperationCanceledException_RethrowsWithoutRetry()
    {
        var runner = CreateRunner(
            new AiOptions { InvalidOutputRetryCount = 1, TransientRetryCount = 1 },
            new SuccessValidator<TestOutput>());
        var context = CreateContext();
        var attempts = 0;

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            runner.RunAsync(
                context,
                _ =>
                {
                    attempts++;
                    throw new OperationCanceledException();
                },
                CancellationToken.None));

        Assert.Equal(1, attempts);
    }

    private static AiStructuredOutputRunner<TestOutput> CreateRunner(
        AiOptions options,
        IValidator<TestOutput> validator)
    {
        IOptions<AiOptions> optionsWrapper = new OptionsWrapper<AiOptions>(options);

        return new AiStructuredOutputRunner<TestOutput>(
            optionsWrapper,
            validator,
            new NullLogger<AiStructuredOutputRunner<TestOutput>>());
    }

    private static AiOperationContext CreateContext() => new(
        OperationName: "test-operation",
        PromptVersion: PromptVersions.QuestionGeneration,
        Provider: "test-provider",
        Model: "test-model");

    private static AiCallMetadata CreateMetadata() => new(
        PromptVersion: PromptVersions.QuestionGeneration,
        Provider: "test-provider",
        Model: "test-model",
        PromptTokens: 10,
        CompletionTokens: 5);

    private sealed record TestOutput(string Value);

    private sealed class SuccessValidator<T> : AbstractValidator<T>
    {
    }

    private sealed class ErrorValidator<T>(string errorMessage) : AbstractValidator<T>
    {
        public override Task<ValidationResult> ValidateAsync(
            ValidationContext<T> context,
            CancellationToken cancellation = default)
        {
            var failure = new ValidationFailure(string.Empty, errorMessage);
            return Task.FromResult(new ValidationResult([failure]));
        }
    }
}
