using Orleans.Contrib.EventSourcing.NATS.Tests.Fixtures;
using Orleans.Contrib.EventSourcing.NATS.Tests.Grains;
using Xunit;
using Shouldly;

namespace Orleans.Contrib.EventSourcing.NATS.Tests;

public class BasicTests(TestFixture<BasicTests.TestSettings> testFixture)
    : IClassFixture<TestFixture<BasicTests.TestSettings>>
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class TestSettings : ITestSettings
    {
        public static string StreamName => nameof(BasicTests);
    }

    [Fact]
    public async Task WhenEventIsRaised_ShouldUpdateState()
    {
        var grain = testFixture.Client.GetGrain<ISampleEventSourcedGrain>(Guid.NewGuid());
        var state = await grain.GetState();
        
        var sampleEvent = new SampleGrainEvent { Name = "test" };
        await grain.Raise(sampleEvent);
        
        var updatedState = await grain.GetState();
        updatedState.Name.ShouldBe("test");
    }
}