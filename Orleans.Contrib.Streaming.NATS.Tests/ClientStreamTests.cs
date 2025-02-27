using System.Diagnostics;
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
        
        var (grain, completeObserver) = await StartConsumeGrain(streamGuid);

        var streamProvider = _testFixture.Client.GetStreamProvider(StreamProvider);
        var dateTime = await PublishMessage(streamGuid, streamProvider);

        await completeObserver.Task.WaitAsync(TimeSpan.FromSeconds(30));
        
        var messages = await grain.Message();
        messages.ShouldContain("test " + dateTime);
    }


    [Fact]
    public async Task WhenMessageIsPublished_ObserverReceivesCompletion()
    {
        var streamGuid = Guid.NewGuid();
        
        var (grain, completeObserver) = await StartConsumeGrain(streamGuid);
        
        var streamProvider = _testFixture.Services.GetRequiredKeyedService<IStreamProvider>(StreamProvider);
        
        var dateTime = await PublishMessage(streamGuid, streamProvider);

        await completeObserver.Task.WaitAsync(TimeSpan.FromSeconds(30));
        
        var messages = await grain.Message();
        messages.ShouldContain("test " + dateTime);
    }
    
    [Fact(Skip = "")]
    public async Task WhenMessageIsPublishedFromClient_ObserverReceivesCompletion_PublishTwo()
    {
        var streamGuid = Guid.NewGuid();
        
        var (grain, completeObserver) = await StartConsumeGrain(streamGuid);

        var streamProvider = _testFixture.Client.GetStreamProvider(StreamProvider);
        var dateTime = await PublishMessage(streamGuid, streamProvider);

        await completeObserver.Task.WaitAsync(TimeSpan.FromSeconds(5));
        
        var messages = await grain.Message();
        messages.ShouldContain("test " + dateTime);

        completeObserver.Reset();

        var dateTime2 = await PublishMessage(streamGuid, streamProvider);

        await completeObserver.Task.WaitAsync(TimeSpan.FromSeconds(5));
        
        var messages2 = await grain.Message();
        messages2.ShouldContain("test " + dateTime2);
    }
    
    [Fact]
    public async Task WhenMessageIsPublishedFromClient_ObserverReceivesCompletion_PublishMany()
    {
        var streamGuid = Guid.NewGuid();
        
        var (grain, completeObserver) = await StartConsumeGrain(streamGuid);

        var streamProvider = _testFixture.Client.GetStreamProvider(StreamProvider);
        await PublishMessages(streamGuid, streamProvider, 10);

        await TestingUtils.WaitUntilAsync(async lastTry => completeObserver.Count >= 10, TimeSpan.FromSeconds(60));
        
        var messages = await grain.Message();
        messages.Count.ShouldBe(10);
    }
    
    [Fact(Skip = "")]
    public async Task WhenMessageIsPublishedFromClient_ObserverReceivesCompletion_WithClientRestart()
    {
        var streamGuid = Guid.NewGuid();
        
        var (grain, completeObserver) = await StartConsumeGrain(streamGuid);

        var streamProvider = _testFixture.Client.GetStreamProvider(StreamProvider);
        var dateTime = await PublishMessage(streamGuid, streamProvider);

        await completeObserver.Task.WaitAsync(TimeSpan.FromSeconds(30));
        
        var messages = await grain.Message();
        messages.ShouldContain("test " + dateTime);

        await _testFixture.KillClientAsync();
        completeObserver.Reset();

        _testFixture.Client.ShouldNotBeNull();
        
        var streamProvider2 = _testFixture.Client.GetStreamProvider(StreamProvider);
        var dateTime2 = await PublishMessage(streamGuid, streamProvider2);

        await completeObserver.Task.WaitAsync(TimeSpan.FromSeconds(30));
        
        var messages2 = await grain.Message();
        messages2.ShouldContain("test " + dateTime2);
    }

    [Fact]
    public async Task WhenMessageIsPublishedFromServer_ObserverReceivesCompletion()
    {
        var streamGuid = Guid.NewGuid();

        var observer = new StreamObserver();
        
        var client = _testFixture.Client;
        var streamProvider = client.GetStreamProvider(StreamProvider);
        var streamId = StreamId.Create(StreamNamespace, streamGuid);
        var stream = streamProvider.GetStream<string>(streamId);
        await stream.SubscribeAsync(observer);
        
        var grainFactory = _testFixture.Services.GetRequiredService<IGrainFactory>();
        var publisherGrain = grainFactory.GetGrain<IPublisherGrain>(Guid.NewGuid());
        await publisherGrain.Publish(StreamProvider, StreamNamespace, streamGuid, "test");
        
        var firstMessage = await observer.FirstAsync().WaitAsync(TimeSpan.FromSeconds(5));
        
        firstMessage.ShouldBe("test");
    }

    public class StreamObserver : IAsyncObserver<string>
    {
        private readonly List<TaskCompletionSource<string>> _taskCompletionSources = new();
        public List<string> Messages = new();
        
        public Task OnNextAsync(string item, StreamSequenceToken? token = null)
        {
            Messages.Add(item);
            foreach (var x in _taskCompletionSources) 
                x.TrySetResult(item);
            _taskCompletionSources.Clear();
            return Task.CompletedTask;
        }
        
        public async Task<string> FirstAsync()
        {
            if (Messages.Count != 0) return Messages.First();
            var taskCompletionSource = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            _taskCompletionSources.Add(taskCompletionSource);
            await taskCompletionSource.Task;
            return Messages.First();
        }

        public Task OnErrorAsync(Exception ex)
        {
            return Task.CompletedTask;
        }
    }
    
    public class TaskCompletionSourceObserver : ICompleteObserver
    {
        public int Count = 0;
        
        private TaskCompletionSource _taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        
        public Task OnCompleted()
        {
            Count++;
            _taskCompletionSource.TrySetResult();
            return Task.CompletedTask;
        }
        
        public Task Task => _taskCompletionSource.Task;

        public void Reset()
        {
            _taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }
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

    private static async Task<DateTime> PublishMessage(Guid streamGuid, IStreamProvider streamProvider)
    {
        var streamId = StreamId.Create(StreamNamespace, streamGuid);
        var stream = streamProvider.GetStream<string>(streamId);
        
        var dateTime = DateTime.UtcNow;
        await stream.OnNextAsync($"test {dateTime}");
        return dateTime;
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