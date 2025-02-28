using Microsoft.Extensions.DependencyInjection;
using Orleans.Contrib.Streaming.NATS.Tests.Fixtures;
using Orleans.Contrib.Streaming.NATS.Tests.Grains;
using Orleans.Streams;
using Shouldly;

namespace Orleans.Contrib.Streaming.NATS.Tests;

public class StreamBatchingTests : StreamTestsBase, IClassFixture<TestFixture>
{
    private const string StreamNamespace = "MyStreamNamespace";
    private const string StreamProvider = "StreamProvider";
    
    private readonly TestFixture _testFixture;

    public StreamBatchingTests(TestFixture testFixture)
    {
        _testFixture = testFixture;
    }
    
    [Fact]
    public async Task WhenMessagesArePublished_CheckConsumeInBatches()
    {
        var streamGuid = Guid.NewGuid();
        
        var (grain, completeObserver) = await StartBatchConsumeGrain(streamGuid);
        
        var streamProvider = _testFixture.Services.GetRequiredKeyedService<IStreamProvider>(StreamProvider);
        
        await BatchPublishMessages(streamGuid, streamProvider, 10);

        await completeObserver.Task.WaitAsync(TimeSpan.FromSeconds(30));
        
        var messages = await grain.Message();
        messages.Count.ShouldBe(10);
        for (int i = 0; i < 10; i++)
        {
            messages.ShouldContain("test " + i);
        }
        
        var batchs = await grain.Batchs();
        batchs.ShouldBe(1);
    }
    
    [Fact]
    public async Task WhenBatchMessagesArePublished_CheckConsumeInBatches()
    {
        var streamGuid = Guid.NewGuid();
        
        var (grain, completeObserver) = await StartConsumeGrain(streamGuid);
        
        var streamProvider = _testFixture.Services.GetRequiredKeyedService<IStreamProvider>(StreamProvider);
        
        await BatchPublishMessages(streamGuid, streamProvider, 10);

        await completeObserver.Task.WaitAsync(TimeSpan.FromSeconds(30));
        
        var messages = await grain.Message();
        messages.Count.ShouldBe(10);
        for (int i = 0; i < 10; i++)
        {
            messages.ShouldContain("test " + i);
        }
    }

    private async Task<(IConsumerGrain grain, TaskCompletionSourceObserver completeObserver)> StartConsumeGrain(Guid streamGuid)
    {
        var grain = _testFixture.Client.GetGrain<IConsumerGrain>(streamGuid);
        var completeObserver = new TaskCompletionSourceObserver();
        
        var reference = _testFixture.Client.CreateObjectReference<ICompleteObserver>(completeObserver);
        await grain.Subscribe(reference);
        await grain.Consume(StreamProvider, StreamNamespace, streamGuid);
        return (grain, completeObserver);
    }
    
    private async Task<(IBatchingStreamConsumerGrain grain, TaskCompletionSourceObserver completeObserver)> StartBatchConsumeGrain(Guid streamGuid)
    {
        var grain = _testFixture.Client.GetGrain<IBatchingStreamConsumerGrain>(streamGuid);
        var completeObserver = new TaskCompletionSourceObserver();
        
        var reference = _testFixture.Client.CreateObjectReference<ICompleteObserver>(completeObserver);
        await grain.Subscribe(reference);
        await grain.Consume(StreamProvider, StreamNamespace, streamGuid);
        return (grain, completeObserver);
    }
    
    private async Task BatchPublishMessages(Guid streamGuid, IStreamProvider streamProvider, int count)
    {
        var streamId = StreamId.Create(StreamNamespace, streamGuid);
        var stream = streamProvider.GetStream<string>(streamId);
        var messages = new List<string>();
        for (var i = 0; i < count; i++)
        {
            messages.Add($"test {i}");
        }
        await stream.OnNextBatchAsync(messages);
    }
}