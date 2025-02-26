namespace Orleans.Contrib.Streaming.NATS.Tests.Grains;

public interface IConsumerGrain : IGrainWithGuidKey
{
    Task<List<string>> Message();
    Task Subscribe(ICompleteObserver observer);
    Task Consume(string streamProvider, string streamNamespace, Guid streamGuid);
}