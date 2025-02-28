using Orleans.Streams;

namespace Orleans.Contrib.Streaming.NATS.Tests.Grains;

public class BatchingStreamConsumerGrain : Grain, IBatchingStreamConsumerGrain, IAsyncBatchObserver<string>
{
    private List<ICompleteObserver> _observers = new();
    private List<string> _messages = new();
    private int _batchs = 0;
    
    public Task Subscribe(ICompleteObserver completeObserver)
    {
        _observers.Add(completeObserver);
        return Task.CompletedTask;
    }

    public Task Consume(string streamProvider, string streamNamespace, Guid streamGuid)
    {
        var streamId = StreamId.Create(streamNamespace, streamGuid);
        var stream = this.GetStreamProvider(streamProvider).GetStream<string>(streamId);
        return stream.SubscribeAsync(this);
    }

    public Task<List<string>> Message()
    {
        return Task.FromResult(_messages);
    }

    public Task<int> Batchs()
    {
        return Task.FromResult(_batchs);
    }
    
    public Task OnNextAsync(IList<SequentialItem<string>> items)
    {
        _batchs++;
        _messages.AddRange(items.Select(x => x.Item));
        foreach (var completeObserver in _observers)
        {
            completeObserver.OnCompleted();
        }
        return Task.CompletedTask;
    }

    public Task OnErrorAsync(Exception ex)
    {
        return Task.CompletedTask;
    }
}