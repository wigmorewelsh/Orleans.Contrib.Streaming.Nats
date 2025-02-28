namespace Orleans.Contrib.Streaming.NATS.Tests.Grains;

public interface IPublisherGrain : IGrainWithGuidKey
{
    Task Publish(string streamProvider, string streamNamespace, Guid streamGuid, string test);
}