using InterviewSimulator.Api.Features.Interviews;

namespace InterviewSimulator.Api.UnitTests.Features.Interviews;

// Guards against drift between the domain enums and the presentation contract
// enums. The total switches in InterviewContractMapping give a compile-time break
// (TreatWarningsAsErrors) when a member is added and left unmapped; these tests are
// the runtime backstop and also assert round-trip stability and name equivalence.
public sealed class InterviewContractMapping_Tests
{
    public static TheoryData<InterviewType> AllInterviewTypes() =>
        [.. Enum.GetValues<InterviewType>()];

    public static TheoryData<InterviewTypeContract> AllInterviewTypeContracts() =>
        [.. Enum.GetValues<InterviewTypeContract>()];

    public static TheoryData<SeniorityLevel> AllSeniorityLevels() =>
        [.. Enum.GetValues<SeniorityLevel>()];

    public static TheoryData<SeniorityLevelContract> AllSeniorityLevelContracts() =>
        [.. Enum.GetValues<SeniorityLevelContract>()];

    [Theory]
    [MemberData(nameof(AllInterviewTypes))]
    public void InterviewType_RoundTripsThroughContract(InterviewType domain)
    {
        Assert.Equal(domain, domain.ToContract().ToDomain());
    }

    [Theory]
    [MemberData(nameof(AllInterviewTypeContracts))]
    public void InterviewTypeContract_RoundTripsThroughDomain(InterviewTypeContract contract)
    {
        Assert.Equal(contract, contract.ToDomain().ToContract());
    }

    [Theory]
    [MemberData(nameof(AllSeniorityLevels))]
    public void SeniorityLevel_RoundTripsThroughContract(SeniorityLevel domain)
    {
        Assert.Equal(domain, domain.ToContract().ToDomain());
    }

    [Theory]
    [MemberData(nameof(AllSeniorityLevelContracts))]
    public void SeniorityLevelContract_RoundTripsThroughDomain(SeniorityLevelContract contract)
    {
        Assert.Equal(contract, contract.ToDomain().ToContract());
    }

    [Fact]
    public void InterviewType_ContractAndDomain_HaveSameMemberNames()
    {
        Assert.Equal(
            Enum.GetNames<InterviewType>().OrderBy(n => n),
            Enum.GetNames<InterviewTypeContract>().OrderBy(n => n));
    }

    [Fact]
    public void SeniorityLevel_ContractAndDomain_HaveSameMemberNames()
    {
        Assert.Equal(
            Enum.GetNames<SeniorityLevel>().OrderBy(n => n),
            Enum.GetNames<SeniorityLevelContract>().OrderBy(n => n));
    }

    [Theory]
    [MemberData(nameof(AllInterviewTypes))]
    public void InterviewType_MapsToContractOfSameName(InterviewType domain)
    {
        Assert.Equal(domain.ToString(), domain.ToContract().ToString());
    }

    [Theory]
    [MemberData(nameof(AllSeniorityLevels))]
    public void SeniorityLevel_MapsToContractOfSameName(SeniorityLevel domain)
    {
        Assert.Equal(domain.ToString(), domain.ToContract().ToString());
    }
}
