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
    
    [Fact]
    public async Task WhenEventIsRaisedAndSiloRestarts_ShouldPersistState()
    {
        var grain = testFixture.Client.GetGrain<ISampleEventSourcedGrain>(Guid.NewGuid());
        
        var sampleEvent = new SampleGrainEvent { Name = "test" };
        await grain.Raise(sampleEvent);
        
        // Restart the silo
        await testFixture.RestartSilo();
        
        var updatedState = await grain.GetState();
        updatedState.Name.ShouldBe("test");
    }
    
    [Fact]
    public async Task WhenManyEventsAreRaised_ShouldUpdateState()
    {
        var grain = testFixture.Client.GetGrain<ISampleEventSourcedGrain>(Guid.NewGuid());
        
        var sampleEvent = new SampleGrainEvent { Name = "test" };
        await grain.RaiseMany(sampleEvent);
        
        var updatedState = await grain.GetState();
        updatedState.Name.ShouldBe("test");
    }
    
    [Fact]
    public async Task WhenMultipleGrainsAreUsed_ShouldPersistStateSeparately()
    {
        var grain1 = testFixture.Client.GetGrain<ISampleEventSourcedGrain>(Guid.NewGuid());
        var grain2 = testFixture.Client.GetGrain<ISampleEventSourcedGrain>(Guid.NewGuid());
        
        var sampleEvent1 = new SampleGrainEvent { Name = "test1" };
        var sampleEvent2 = new SampleGrainEvent { Name = "test2" };
        
        await grain1.Raise(sampleEvent1);
        await grain2.Raise(sampleEvent2);
        
        // Restart the silo
        await testFixture.RestartSilo();
        
        var updatedState1 = await grain1.GetState();
        var updatedState2 = await grain2.GetState();
        
        updatedState1.Name.ShouldBe("test1");
        updatedState2.Name.ShouldBe("test2");
    }
    
    [Fact]
    public async Task WhenConditionalEventIsRaised_ShouldUpdateState()
    {
        var grain = testFixture.Client.GetGrain<ISampleEventSourcedGrain>(Guid.NewGuid());
        
        var sampleEvent = new SampleGrainEvent { Name = "test" };
        var success = await grain.RaiseConditional(sampleEvent);
        success.ShouldBeTrue();
        
        var updatedState = await grain.GetState();
        updatedState.Name.ShouldBe("test");
    }
    
    // check raising multile conditional events
    [Fact]
    public async Task WhenMultipleConditionalEventsAreRaised_ShouldUpdateState()
    {
        var grain = testFixture.Client.GetGrain<ISampleEventSourcedGrain>(Guid.NewGuid());
      
        var sampleEvent = new SampleGrainEvent { Name = "test" };
        var success = await grain.RaiseManyConditional(sampleEvent); 
        success.ShouldBeTrue();
        
        var updatedState = await grain.GetState();
        updatedState.Name.ShouldBe("test");
    }
    
}