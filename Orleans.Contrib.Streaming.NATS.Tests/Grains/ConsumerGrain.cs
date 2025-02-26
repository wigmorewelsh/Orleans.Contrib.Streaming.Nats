using Orleans.Streams;

namespace Orleans.Contrib.Streaming.NATS.Tests.Grains;

public class ConsumerGrain : Grain, IConsumerGrain, IAsyncObserver<string>
{
    private List<string> _messages = new();
    private StreamSubscriptionHandle<string> _sub;
    private List<ICompleteObserver> _observers = new();

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        if(_sub is {} sub)
            await sub.UnsubscribeAsync();
    }
    
    public Task Subscribe(ICompleteObserver observer)
    {
        _observers.Add(observer);
        return Task.CompletedTask;
    }

    public async Task Consume(string streamProvider, string streamNamespace, Guid streamGuid)
    {
        var streamId = StreamId.Create(streamNamespace, streamGuid);
        var stream = this.GetStreamProvider(streamProvider).GetStream<string>(streamId);
        _sub = await stream.SubscribeAsync(this);
    }

    public Task<List<string>> Message()
    {
        return Task.FromResult(_messages);
    }
    
    public async Task OnNextAsync(string item, StreamSequenceToken? token = null)
    {
        _messages.Add(item);
        foreach (var completeObserver in _observers)
        {
            await completeObserver.OnCompleted();
        }
    }

    public Task OnCompletedAsync()
    {
        return Task.CompletedTask;
    }

    public Task OnErrorAsync(Exception ex)
    {
        return Task.CompletedTask;
    }
}