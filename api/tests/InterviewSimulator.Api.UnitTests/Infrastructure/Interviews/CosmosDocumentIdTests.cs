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
