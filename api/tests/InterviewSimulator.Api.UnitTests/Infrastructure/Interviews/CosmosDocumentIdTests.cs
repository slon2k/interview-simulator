using InterviewSimulator.Api.Infrastructure.Interviews;

namespace InterviewSimulator.Api.UnitTests.Infrastructure.Interviews;

public sealed class CosmosSessionDocument_ToCosmosId
{
    [Fact]
    public void ToCosmosId_WithValidGuid_ReturnsSessionPrefixedId()
    {
        var id = new Guid("12345678-1234-1234-1234-123456789abc");

        var result = CosmosSessionDocument.ToCosmosId(id);

        Assert.Equal("session|12345678-1234-1234-1234-123456789abc", result);
    }

    [Fact]
    public void ToCosmosId_WithEmptyGuid_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            CosmosSessionDocument.ToCosmosId(Guid.Empty));

        Assert.Equal("sessionId", ex.ParamName);
    }
}

public sealed class CosmosSessionDocument_Create
{
    [Fact]
    public void Create_WithValidArguments_SetsAllFields()
    {
        var sessionId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        var doc = CosmosSessionDocument.Create(
            sessionId: sessionId,
            userId: "github|100",
            role: "backend-engineer",
            seniority: "mid",
            topic: "dotnet",
            interviewType: "technical",
            createdAt: createdAt,
            questionCount: 5,
            status: "active",
            answeredCount: 0);

        Assert.Equal(CosmosSessionDocument.ToCosmosId(sessionId), doc.Id);
        Assert.Equal("github|100", doc.UserId);
        Assert.Equal("session", doc.Type);
        Assert.Equal(1, doc.SchemaVersion);
        Assert.Equal("backend-engineer", doc.Role);
        Assert.Equal("mid", doc.Seniority);
        Assert.Equal("dotnet", doc.Topic);
        Assert.Equal("technical", doc.InterviewType);
        Assert.Equal("active", doc.Status);
        Assert.Equal(5, doc.QuestionCount);
        Assert.Equal(0, doc.AnsweredCount);
        Assert.Equal(createdAt, doc.CreatedAt);
        Assert.Equal(createdAt, doc.UpdatedAt);
        Assert.Null(doc.Summary);
        Assert.Null(doc.StartedAt);
        Assert.Null(doc.CompletedAt);
    }

    [Fact]
    public void Create_WithEmptySessionId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            CosmosSessionDocument.Create(
                sessionId: Guid.Empty,
                userId: "github|100",
                role: "r", seniority: "s", topic: "t", interviewType: "i",
                createdAt: DateTimeOffset.UtcNow,
                questionCount: 5, status: "active", answeredCount: 0));

        Assert.Equal("sessionId", ex.ParamName);
    }

    [Fact]
    public void Create_WithWhitespaceUserId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            CosmosSessionDocument.Create(
                sessionId: Guid.NewGuid(),
                userId: "   ",
                role: "r", seniority: "s", topic: "t", interviewType: "i",
                createdAt: DateTimeOffset.UtcNow,
                questionCount: 5, status: "active", answeredCount: 0));

        Assert.Equal("userId", ex.ParamName);
    }
}

public sealed class CosmosTurnDocument_ToCosmosId
{
    [Theory]
    [InlineData(1,   "turn|12345678-1234-1234-1234-123456789abc|001")]
    [InlineData(9,   "turn|12345678-1234-1234-1234-123456789abc|009")]
    [InlineData(10,  "turn|12345678-1234-1234-1234-123456789abc|010")]
    [InlineData(99,  "turn|12345678-1234-1234-1234-123456789abc|099")]
    [InlineData(100, "turn|12345678-1234-1234-1234-123456789abc|100")]
    [InlineData(999, "turn|12345678-1234-1234-1234-123456789abc|999")]
    public void ToCosmosId_ProducesCorrectlyPaddedId(int turnNumber, string expected)
    {
        var sessionId = new Guid("12345678-1234-1234-1234-123456789abc");

        var result = CosmosTurnDocument.ToCosmosId(sessionId, turnNumber);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToCosmosId_AlphabeticOrdering_IsPreserved()
    {
        var sessionId = Guid.NewGuid();

        var ids = Enumerable.Range(1, 100)
            .Select(n => CosmosTurnDocument.ToCosmosId(sessionId, n))
            .ToList();

        var sorted = ids.OrderBy(x => x, StringComparer.Ordinal).ToList();

        Assert.Equal(ids, sorted);
    }

    [Fact]
    public void ToCosmosId_WithEmptyGuid_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            CosmosTurnDocument.ToCosmosId(Guid.Empty, 1));

        Assert.Equal("sessionId", ex.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void ToCosmosId_WithNonPositiveTurnNumber_ThrowsArgumentOutOfRangeException(int turnNumber)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            CosmosTurnDocument.ToCosmosId(Guid.NewGuid(), turnNumber));

        Assert.Equal("turnNumber", ex.ParamName);
    }
}

public sealed class CosmosTurnDocument_Create
{
    [Fact]
    public void Create_WithValidArguments_SetsAllFields()
    {
        var sessionId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var question = new CosmosQuestionDocument { Text = "Tell me about yourself." };

        var doc = CosmosTurnDocument.Create(
            sessionId: sessionId,
            userId: "github|100",
            turnNumber: 1,
            question: question,
            createdAt: createdAt);

        Assert.Equal(CosmosTurnDocument.ToCosmosId(sessionId, 1), doc.Id);
        Assert.Equal("github|100", doc.UserId);
        Assert.Equal("turn", doc.Type);
        Assert.Equal(1, doc.SchemaVersion);
        Assert.Equal(1, doc.TurnNumber);
        Assert.Equal("Tell me about yourself.", doc.Question.Text);
        Assert.Equal(createdAt, doc.CreatedAt);
        Assert.Equal(createdAt, doc.UpdatedAt);
        Assert.Null(doc.Answer);
        Assert.Null(doc.Evaluation);
        Assert.Null(doc.AiMetadata);
        Assert.Null(doc.AnsweredAt);
        Assert.Null(doc.EvaluatedAt);
    }

    [Fact]
    public void Create_WithEmptySessionId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            CosmosTurnDocument.Create(
                sessionId: Guid.Empty,
                userId: "github|100",
                turnNumber: 1,
                question: new CosmosQuestionDocument { Text = "Q" },
                createdAt: DateTimeOffset.UtcNow));

        Assert.Equal("sessionId", ex.ParamName);
    }

    [Fact]
    public void Create_WithWhitespaceUserId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            CosmosTurnDocument.Create(
                sessionId: Guid.NewGuid(),
                userId: "  ",
                turnNumber: 1,
                question: new CosmosQuestionDocument { Text = "Q" },
                createdAt: DateTimeOffset.UtcNow));

        Assert.Equal("userId", ex.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveTurnNumber_ThrowsArgumentOutOfRangeException(int turnNumber)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            CosmosTurnDocument.Create(
                sessionId: Guid.NewGuid(),
                userId: "github|100",
                turnNumber: turnNumber,
                question: new CosmosQuestionDocument { Text = "Q" },
                createdAt: DateTimeOffset.UtcNow));

        Assert.Equal("turnNumber", ex.ParamName);
    }
}
