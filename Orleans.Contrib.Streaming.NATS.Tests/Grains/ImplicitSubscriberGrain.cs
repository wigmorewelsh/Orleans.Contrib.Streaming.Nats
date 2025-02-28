using Orleans.Streams;
using Orleans.Streams.Core;

namespace Orleans.Contrib.Streaming.NATS.Tests.Grains;

[ImplicitStreamSubscription(Constants.StreamNamespace)]
public class ImplicitSubscriberGrain : Grain, IStreamSubscriptionObserver, IAsyncObserver<string>, IImplicitSubscriberGrain
{
    private readonly List<string> _messages = new();
    private readonly List<ICompleteObserver> _observers = new();
    
    public Task OnSubscribed(IStreamSubscriptionHandleFactory handleFactory)
    {
        var handle = handleFactory.Create<string>();
        return handle.ResumeAsync(this);
    }

    public Task OnNextAsync(string item, StreamSequenceToken? token = null)
    {
        _messages.Add(item);
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

    public Task Subscribe(ICompleteObserver reference)
    {
        _observers.Add(reference);
        return Task.CompletedTask;
    }

    public Task<List<string>> Message()
    {
        return Task.FromResult(_messages);
    }
}