using Microsoft.Extensions.DependencyInjection;
using Orleans.Contrib.Streaming.NATS.Tests.Fixtures;
using Orleans.Contrib.Streaming.NATS.Tests.Grains;
using Orleans.Streams;
using Shouldly;

namespace Orleans.Contrib.Streaming.NATS.Tests;

public class ImplicitSubscriberTests : IClassFixture<TestFixture>
{
    private const string StreamProvider = "StreamProvider";
    
    private TestFixture _testFixture;
    
    public ImplicitSubscriberTests(TestFixture testFixture)
    {
        _testFixture = testFixture;
    }
    
    [Fact]
    public async Task WhenMessageIsPublished_ObserverReceivesCompletion()
    {
        var streamGuid = Guid.NewGuid();
        
        var streamProvider = _testFixture.Services.GetRequiredKeyedService<IStreamProvider>(StreamProvider);
        
        await PublishMessages(streamGuid, streamProvider, 10);
        
        var grain = _testFixture.Client.GetGrain<IImplicitSubscriberGrain>(streamGuid);
        var observer = new TaskCompletionSourceObserver();
        var reference = _testFixture.Client.CreateObjectReference<ICompleteObserver>(observer);
        await grain.Subscribe(reference);
        
        await observer.WaitFor(10).WaitAsync(TimeSpan.FromSeconds(30));
        
        var messages = await grain.Message();
        messages.Count.ShouldBe(10);
        for (int i = 0; i < 10; i++)
        {
            messages.ShouldContain("test " + i);
        }
    }
    
    private static async Task PublishMessages(Guid streamGuid, IStreamProvider streamProvider, int count)
    {
        var streamId = StreamId.Create(Constants.StreamNamespace, streamGuid);
        var stream = streamProvider.GetStream<string>(streamId);
        
        for (int i = 0; i < count; i++)
        {
            await stream.OnNextAsync($"test {i}");
        }
    }
   
}