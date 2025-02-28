using Microsoft.Extensions.DependencyInjection;
using Orleans.Contrib.Streaming.NATS.Tests.Fixtures;
using Orleans.Contrib.Streaming.NATS.Tests.Grains;
using Orleans.Streams;
using Shouldly;
using Xunit.Abstractions;

namespace Orleans.Contrib.Streaming.NATS.Tests;

public class MultipleSubscriberTests : StreamTestsBase, IClassFixture<TestFixture>
{
    private const string StreamNamespace = "MyStreamNamespace";
    private const string StreamProvider = "StreamProvider";
    private readonly TestFixture _testFixture;
    private readonly ITestOutputHelper _output;

    public MultipleSubscriberTests(TestFixture testFixture, ITestOutputHelper output)
    {
        _testFixture = testFixture;
        _output = output;
    }
    
    [Fact]
    public async Task MultipleParallelSubscriptionTest()
    {
        var streamGuid = Guid.NewGuid();
        
        var (consumer1, completeObserver1) = await StartConsumeGrain(streamGuid);
        var (consumer2, completeObserver2) = await StartConsumeGrain(streamGuid); 
        
        var streamProvider = _testFixture.Services.GetRequiredKeyedService<IStreamProvider>(StreamProvider);

        await PublishMessages(streamGuid, streamProvider, 10);
       
        var tasks = new List<Task>
        {
            completeObserver1.WaitFor(10),
            completeObserver2.WaitFor(10)
        };
        
        await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(30));
        
        var messages1 = await consumer1.Message();
        var messages2 = await consumer2.Message();
        if (messages1.Count > 10)
        {
            _output.WriteLine("Messages1: " + string.Join(", ", messages1));
        }
        if( messages2.Count > 10)
        {
            _output.WriteLine("Messages2: " + string.Join(", ", messages2));
        }
        
        messages1.Count.ShouldBe(10);
        messages2.Count.ShouldBe(10);
    }
    
    private async Task<(IConsumerGrain grain, TaskCompletionSourceObserver completeObserver)> StartConsumeGrain(Guid streamGuid)
    {
        var grainFactory = _testFixture.Services.GetRequiredService<IGrainFactory>();
        var grain = grainFactory.GetGrain<IConsumerGrain>(Guid.NewGuid());
        await grain.Consume(StreamProvider, StreamNamespace, streamGuid);
       
        var completeObserver = new TaskCompletionSourceObserver();
        var reference = grainFactory.CreateObjectReference<ICompleteObserver>(completeObserver);
        await grain.Subscribe(reference);
        return (grain, completeObserver);
    }
    
    private static async Task PublishMessages(Guid streamGuid, IStreamProvider streamProvider, int count)
    {
        var streamId = StreamId.Create(StreamNamespace, streamGuid);
        var stream = streamProvider.GetStream<string>(streamId);
        
        for (int i = 0; i < count; i++)
        {
            await stream.OnNextAsync($"test {i}");
        }
    }
}