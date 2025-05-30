using Orleans.EventSourcing;

namespace Orleans.Contrib.EventSourcing.NATS.Tests.Grains;

[GenerateSerializer]
[Alias("Orleans.Contrib.EventSourcing.NATS.Tests.Grains.SampleGrainState")]
public class SampleGrainState 
{
    [Id(0)]
    public string Name { get; set; }
    
    public void Apply(SampleGrainEvent @event)
    {
        Name = @event.Name;
    }
}

public interface ISampleGrainEventBase
{
    
}

[GenerateSerializer]
[Alias("Orleans.Contrib.EventSourcing.NATS.Tests.Grains.SampleGrainEvent")]
public class SampleGrainEvent : ISampleGrainEventBase
{
    [Id(0)]
    public string Name { get; set; } = string.Empty;
}

[Alias("Orleans.Contrib.EventSourcing.NATS.Tests.Grains.ISampleEventSourcedGrain")]
public interface ISampleEventSourcedGrain : IGrainWithGuidKey
{
    Task Raise(SampleGrainEvent @event);
    Task<SampleGrainState> GetState();
    Task RaiseMany(SampleGrainEvent @event);
    Task<bool> RaiseConditional(SampleGrainEvent sampleEvent);
    Task<bool> RaiseManyConditional(SampleGrainEvent sampleEvent);
}

public class SampleEventSourcedGrain : JournaledGrain<SampleGrainState, ISampleGrainEventBase>, ISampleEventSourcedGrain
{

    public async Task Raise(SampleGrainEvent @event)
    {
        RaiseEvent(@event);
        await ConfirmEvents();
    }
    
    public async Task RaiseMany(SampleGrainEvent @event)
    {
        RaiseEvents([@event]);
        await ConfirmEvents();
    }

    public async Task<bool> RaiseConditional(SampleGrainEvent sampleEvent)
    {
        return await base.RaiseConditionalEvent(sampleEvent);
    }

    public Task<bool> RaiseManyConditional(SampleGrainEvent sampleEvent)
    {
        return base.RaiseConditionalEvents([sampleEvent]);
    }

    public async Task<SampleGrainState> GetState()
    {
        return this.State;
    }
}