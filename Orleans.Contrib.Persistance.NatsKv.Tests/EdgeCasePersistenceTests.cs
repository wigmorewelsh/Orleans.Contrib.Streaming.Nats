using System;
using System.Text;
using System.Threading.Tasks;
using Orleans.Contrib.Persistance.NatsKv.Tests.Fixtures;
using Orleans.Contrib.Persistance.NatsKv.Tests.Grains;
using Shouldly;
using Xunit;

namespace Orleans.Contrib.Persistance.NatsKv.Tests;

public class EdgeCasePersistenceTests : IClassFixture<TestFixture<EdgeCasePersistenceTests.TestSettings>>
{
    public class TestSettings : ITestSettings
    {
        public static string StreamName => nameof(EdgeCasePersistenceTests);
    }

    private readonly TestFixture<TestSettings> _fixture;

    public EdgeCasePersistenceTests(TestFixture<TestSettings> fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task WhenLargeStateObjectIsWrittenAndRead_ShouldFailWithOrleansException()
    {
        var grain = _fixture.Client.GetGrain<ITestStateGrain>(Guid.NewGuid().ToString());
        var largeValue = new string('x', 1024 * 1024); // 1MB string
        var ex = await Should.ThrowAsync<Orleans.Runtime.OrleansException>(async () => await grain.SetState(largeValue));
        ex.Message.ShouldContain("Payload size exceeds NATS KV limit");
    }

    [Fact]
    public async Task WhenEmptyStateObjectIsWrittenAndRead_ShouldSucceed()
    {
        var grain = _fixture.Client.GetGrain<ITestStateGrain>(Guid.NewGuid().ToString());
        await grain.SetState(string.Empty);
        var state = await grain.GetState();
        state.Value.ShouldBe(string.Empty);
    }

    [Fact(Skip = "NATS does not support special characters in keys; this test is for documentation only.")]
    public async Task WhenKeyHasSpecialCharacters_ShouldStoreAndRetrieveSuccessfully()
    {
        var specialKey = "key!@#$%^&*()_+-=|\\/?<>,.~`";
        var grain = _fixture.Client.GetGrain<ITestStateGrain>(specialKey);
        await grain.SetState("special");
        var state = await grain.GetState();
        state.Value.ShouldBe("special");
    }
}


