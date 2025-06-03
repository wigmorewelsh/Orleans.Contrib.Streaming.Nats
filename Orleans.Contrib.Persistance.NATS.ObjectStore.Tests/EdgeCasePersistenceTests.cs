using Orleans.Contrib.Persistance.NATS.ObjectStore.Tests.Fixtures;
using Orleans.Contrib.Persistance.NATS.ObjectStore.Tests.Grains;
using Shouldly;

namespace Orleans.Contrib.Persistance.NATS.ObjectStore.Tests;

public class EdgeCasePersistenceTests : IClassFixture<TestFixture<EdgeCasePersistenceTests.TestSettings>>
{
    public class TestSettings : ITestSettings
    {
        public static string StoreName => nameof(EdgeCasePersistenceTests);
    }

    private readonly TestFixture<TestSettings> _fixture;

    public EdgeCasePersistenceTests(TestFixture<TestSettings> fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task WhenEmptyStateObjectIsWrittenAndRead_ShouldSucceed()
    {
        var grain = _fixture.Client.GetGrain<ITestStateGrain>(Guid.NewGuid().ToString());
        await grain.SetState(string.Empty);
        var state = await grain.GetState();
        state.ShouldBe(string.Empty);
    }

    [Fact(Skip = "NATS does not support special characters in keys; this test is for documentation only.")]
    public async Task WhenKeyHasSpecialCharacters_ShouldStoreAndRetrieveSuccessfully()
    {
        var specialKey = "key!@#$%^&*()_+-=|\\/?<>,.~`";
        var grain = _fixture.Client.GetGrain<ITestStateGrain>(specialKey);
        await grain.SetState("special");
        var state = await grain.GetState();
        state.ShouldBe("special");
    }
}


