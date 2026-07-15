namespace InterviewSimulator.Api.UnitTests;

public sealed class InterviewRequest_UnitTests
{
    [Fact]
    public void Constructor_WithInvalidLength_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => new InterviewRequest("x"));

        Assert.Equal("prompt", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithValidLength_CreatesInstance()
    {
        var request = new InterviewRequest("valid prompt");

        Assert.Equal("valid prompt", request.Prompt);
    }

    private sealed class InterviewRequest
    {
        public InterviewRequest(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt) || prompt.Length < 3)
            {
                throw new ArgumentException("Prompt must contain at least 3 characters.", nameof(prompt));
            }

            Prompt = prompt;
        }

        public string Prompt { get; }
    }
}
