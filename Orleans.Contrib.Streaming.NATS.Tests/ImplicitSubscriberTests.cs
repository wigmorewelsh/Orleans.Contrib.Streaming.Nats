using Microsoft.Extensions.DependencyInjection;
using Orleans.Contrib.Streaming.NATS.Tests.Fixtures;
using Orleans.Contrib.Streaming.NATS.Tests.Grains;
using Orleans.Streams;
using Shouldly;

namespace Orleans.Contrib.Streaming.NATS.Tests;

public class ImplicitSubscriberTests : IClassFixture<TestFixture<ImplicitSubscriberTests.TestSettings>>
{
    public class TestSettings : ITestSettings
    {
        public static string StreamName => nameof(ImplicitSubscriberTests);
    }
    
    private const string StreamProvider = "StreamProvider";
    
    private TestFixture<TestSettings> _testFixture;
    
    public ImplicitSubscriberTests(TestFixture<TestSettings> testFixture)
    {
        _testFixture = testFixture;
    }
    
    [Fact(Skip = "Duplicate test")]
    public async Task WhenMessageIsPublished_ObserverReceivesCompletion()
    {
        var streamGuid = Guid.NewGuid();
        
        var streamProvider = _testFixture.Services.GetRequiredKeyedService<IStreamProvider>(StreamProvider);
        
        await PublishMessages(streamGuid, streamProvider, 10);
        
        var (grain, observer) = await ObserveImplicitGrain(streamGuid);

        await observer.WaitFor(10).WaitAsync(TimeSpan.FromSeconds(30));
        
        var messages = await grain.Message();
        messages.Count.ShouldBe(10);
        for (int i = 0; i < 10; i++)
        {
            messages.ShouldContain("test " + i);
        }
    }
    
    // test 100 streams to implicit grains, sent in parallel
    [Fact]
    public async Task Check100Streams()
    {
        var streamProvider = _testFixture.Services.GetRequiredKeyedService<IStreamProvider>(StreamProvider);
        
        var streamIds = Enumerable.Range(0, 100).Select(x => Guid.NewGuid()).ToList();
        
        // start observing
        var observers = new List<TaskCompletionSourceObserver>();
        foreach (var streamId in streamIds)
        {
            var (_, observer) = await ObserveImplicitGrain(streamId);
            observers.Add(observer);
        }
        
        // publish messages
        var tasks = new List<Task>();
        var expectedMessages = 100;
        
        foreach (var streamId in streamIds)
        {
            tasks.Add(PublishMessages(streamId, streamProvider, expectedMessages));
        }
        await Task.WhenAll(tasks);
        
        // wait for completion
        var waitTasks = observers.Select(x => x.WaitFor(expectedMessages).WaitAsync(TimeSpan.FromMinutes(3)));
        await Task.WhenAll(waitTasks);
        
        // check messages
        foreach (var streamId in streamIds)
        {
            var (grain, _) = await ObserveImplicitGrain(streamId);
            var messages = await grain.Message();
            messages.Count.ShouldBe(expectedMessages);
            for (int i = 0; i < expectedMessages; i++)
            {
                messages.ShouldContain("test " + i);
            }
        }
    }
    

    private async Task<(IImplicitSubscriberGrain grain, TaskCompletionSourceObserver observer)> ObserveImplicitGrain(Guid streamGuid)
    {
        var grain = _testFixture.Client.GetGrain<IImplicitSubscriberGrain>(streamGuid);
        var observer = new TaskCompletionSourceObserver();
        var reference = _testFixture.Client.CreateObjectReference<ICompleteObserver>(observer);
        await grain.Subscribe(reference);
        return (grain, observer);
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