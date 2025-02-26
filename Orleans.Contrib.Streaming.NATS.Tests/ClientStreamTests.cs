using Microsoft.Extensions.DependencyInjection;
using Orleans.Contrib.Streaming.NATS.Tests.Fixtures;
using Orleans.Contrib.Streaming.NATS.Tests.Grains;
using Orleans.Streams;
using Shouldly;

namespace Orleans.Contrib.Streaming.NATS.Tests;

public class ClientStreamTests : IClassFixture<TestFixture>  
{
    private const string StreamNamespace = "MyStreamNamespace";
    private const string StreamProvider = "StreamProvider";
    private readonly TestFixture _testFixture;

    public ClientStreamTests(TestFixture testFixture)
    {
        _testFixture = testFixture;
    }
    
    [Fact]
    public async Task WhenMessageIsPublishedFromClient_ObserverReceivesCompletion()
    {
        var streamGuid = Guid.NewGuid();
        
        var grainFactory = _testFixture.Services.GetRequiredService<IGrainFactory>();
        var grain = grainFactory.GetGrain<IConsumerGrain>(Guid.NewGuid());
        await grain.Consume(StreamProvider, StreamNamespace, streamGuid);
       
        var completeObserver = new TaskCompletionSourceObserver();
        var reference = grainFactory.CreateObjectReference<ICompleteObserver>(completeObserver);
        await grain.Subscribe(reference);
        
        var streamProvider = _testFixture.Client.GetStreamProvider(StreamProvider);
        var streamId = StreamId.Create(StreamNamespace, streamGuid);
        var stream = streamProvider.GetStream<string>(streamId);
        
        var dateTime = DateTime.UtcNow;
        await stream.OnNextAsync($"test {dateTime}");

        await completeObserver.Task.WaitAsync(TimeSpan.FromSeconds(30));
        
        await Task.Delay(500);
        
        var messages = await grain.Message();
        messages.ShouldContain("test " + dateTime);
    }

    
    [Fact]
    public async Task WhenMessageIsPublished_ObserverReceivesCompletion()
    {
        var streamGuid = Guid.NewGuid();
        
        var grainFactory = _testFixture.Services.GetRequiredService<IGrainFactory>();
        var grain = grainFactory.GetGrain<IConsumerGrain>(Guid.NewGuid());
        await grain.Consume(StreamProvider, StreamNamespace, streamGuid);
       
        var completeObserver = new TaskCompletionSourceObserver();
        var reference = grainFactory.CreateObjectReference<ICompleteObserver>(completeObserver);
        await grain.Subscribe(reference);
        
        var streamProvider = _testFixture.Services.GetRequiredKeyedService<IStreamProvider>(StreamProvider);
        var streamId = StreamId.Create(StreamNamespace, streamGuid);
        var stream = streamProvider.GetStream<string>(streamId);
        
        var dateTime = DateTime.UtcNow;
        await stream.OnNextAsync($"test {dateTime}");

        await completeObserver.Task.WaitAsync(TimeSpan.FromSeconds(30));
        
        await Task.Delay(500);
        
        var messages = await grain.Message();
        messages.ShouldContain("test " + dateTime);
    }

    public class TaskCompletionSourceObserver : ICompleteObserver
    {
        private readonly TaskCompletionSource _taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        
        public Task OnCompleted()
        {
            _taskCompletionSource.TrySetResult();
            return Task.CompletedTask;
        }
        
        public Task Task => _taskCompletionSource.Task;
    }
}