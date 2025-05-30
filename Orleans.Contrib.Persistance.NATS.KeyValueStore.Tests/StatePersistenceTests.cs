using System;
using System.Threading.Tasks;
using Orleans.Contrib.Persistance.NATS.KeyValueStore.Tests.Fixtures;
using Orleans.Contrib.Persistance.NATS.KeyValueStore.Tests.Grains;
using Shouldly;
using Xunit;

namespace Orleans.Contrib.Persistance.NATS.KeyValueStore.Tests;

public class StatePersistenceTests : IClassFixture<TestFixture<StatePersistenceTests.Settings>>
{
    private readonly TestFixture<Settings> _fixture;

    public StatePersistenceTests(TestFixture<Settings> fixture)
    {
        _fixture = fixture;
    }

    public class Settings : ITestSettings
    {
        public static string StreamName => "test-stream";
    }

    [Fact]
    public async Task WhenStateIsWrittenAndRead_ShouldReturnPersistedValue()
    {
        var grain = _fixture.Client.GetGrain<ITestStateGrain>(Guid.NewGuid().ToString());
        await grain.SetState("hello");
        var state = await grain.GetState();
        state.Value.ShouldBe("hello");
    }

    [Fact]
    public async Task WhenStateIsUpdated_ShouldOverwritePreviousValue()
    {
        var grain = _fixture.Client.GetGrain<ITestStateGrain>(Guid.NewGuid().ToString());
        await grain.SetState("first");
        await grain.SetState("second");
        var state = await grain.GetState();
        state.Value.ShouldBe("second");
    }

    [Fact]
    public async Task WhenStateIsDeleted_ShouldRemoveFromStorage()
    {
        var grain = _fixture.Client.GetGrain<ITestStateGrain>(Guid.NewGuid().ToString());
        await grain.SetState("to-be-deleted");
        await grain.DeleteState();
        var state = await grain.GetState();
        state.Value.ShouldBeNull();
    }

    [Fact]
    public async Task WhenStateDoesNotExist_ShouldReturnNull()
    {
        var grain = _fixture.Client.GetGrain<ITestStateGrain>(Guid.NewGuid().ToString());
        var state = await grain.GetState();
        state.Value.ShouldBeNull();
    }
}
