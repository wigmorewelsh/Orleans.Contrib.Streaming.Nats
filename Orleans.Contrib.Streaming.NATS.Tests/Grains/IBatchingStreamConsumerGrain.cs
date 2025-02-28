namespace Orleans.Contrib.Streaming.NATS.Tests.Grains;

internal interface IBatchingStreamConsumerGrain : IGrainWithGuidKey
{
    Task Subscribe(ICompleteObserver completeObserver);
    Task Consume(string streamProvider, string streamNamespace, Guid streamGuid);
    Task<List<string>> Message();
    Task<int> Batchs();
}