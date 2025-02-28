namespace Orleans.Contrib.Streaming.NATS.Tests.Grains;

public interface IImplicitSubscriberGrain : IGrainWithGuidKey
{
    Task Subscribe(ICompleteObserver reference);
    Task<List<string>> Message();
}