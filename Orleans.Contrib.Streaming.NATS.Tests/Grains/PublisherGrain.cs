using Orleans.Streams;

namespace Orleans.Contrib.Streaming.NATS.Tests.Grains;

public class PublisherGrain : Grain, IPublisherGrain
{
    private IAsyncStream<string> _stream;

    public async Task Publish(string streamProvider, string streamNamespace, Guid streamGuid, string test)
    {
        var provider = this.GetStreamProvider(streamProvider);
        var streamId = StreamId.Create(streamNamespace, streamGuid);
        _stream = provider.GetStream<string>(streamId);
        
        await _stream.OnNextAsync(test);
    }
}