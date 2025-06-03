using Orleans.Contrib.Persistance.NATS.ObjectStore.Tests.Fixtures;
using Orleans.Contrib.Persistance.NATS.ObjectStore.Tests.Grains;
using Shouldly;

namespace Orleans.Contrib.Persistance.NATS.ObjectStore.Tests;

public class StatePersistenceTests : IClassFixture<TestFixture<StatePersistenceTests.Settings>>
{
    private readonly TestFixture<Settings> _fixture;

    public StatePersistenceTests(TestFixture<Settings> fixture)
    {
        _fixture = fixture;
    }

    public class Settings : ITestSettings
    {
        public static string StoreName => "test-stream";
    }

    [Fact]
    public async Task WhenStateIsWrittenAndRead_ShouldReturnPersistedValue()
    {
        var grain = _fixture.Client.GetGrain<ITestStateGrain>(Guid.NewGuid().ToString());
        await grain.SetState("hello");
        var state = await grain.GetState();
        state.ShouldBe("hello");
    }

    [Fact]
    public async Task WhenStateIsUpdated_ShouldOverwritePreviousValue()
    {
        var grain = _fixture.Client.GetGrain<ITestStateGrain>(Guid.NewGuid().ToString());
        await grain.SetState("first");
        await grain.SetState("second");
        var state = await grain.GetState();
        state.ShouldBe("second");
    }

    [Fact]
    public async Task WhenStateIsDeleted_ShouldRemoveFromStorage()
    {
        var grain = _fixture.Client.GetGrain<ITestStateGrain>(Guid.NewGuid().ToString());
        await grain.SetState("to-be-deleted");
        await grain.DeleteState();
        var state = await grain.GetState();
        state.ShouldBeEmpty();
    }

    [Fact]
    public async Task WhenStateDoesNotExist_ShouldReturnNull()
    {
        var grain = _fixture.Client.GetGrain<ITestStateGrain>(Guid.NewGuid().ToString());
        var state = await grain.GetState();
        state.ShouldBeEmpty();
    }
}
