using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime;
using Orleans.Streams;
using Shouldly;

namespace Orleans.Conttib.Streaming.Nats.Tests;

public class WalkthoughTests : IClassFixture<TestFixture>  
{
    private readonly TestFixture _testFixture;

    public WalkthoughTests(TestFixture testFixture)
    {
        _testFixture = testFixture;
    }
    
    [Fact]
    public async Task Test1()
    {
        var grainFactory = _testFixture.Services.GetRequiredService<IGrainFactory>();
        var grain = grainFactory.GetGrain<ITestGrain>(Guid.NewGuid());
        var result = await grain.Test();
        result.ShouldBe("Hello, World!");
    }
    
    [Fact]
    public async Task Test2()
    {
        var grainFactory = _testFixture.Services.GetRequiredService<IGrainFactory>();
        var grain = grainFactory.GetGrain<ITestGrain>(Guid.NewGuid());
        await grain.Test2();
       
        var completeObserver = new TaskCompletionSourceObserver();
        var reference = grainFactory.CreateObjectReference<ICompleteObserver>(completeObserver);
        await grain.Subscribe(reference);
        
        var streamProvider = _testFixture.Services.GetRequiredKeyedService<IStreamProvider>("StreamProvider");
        var streamId = StreamId.Create("MyStreamNamespace", Guid.Empty);
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

public interface ITestGrain : IGrainWithGuidKey
{
    Task<string> Test();
    Task Test2();
    Task<List<string>> Message();
    Task Subscribe(ICompleteObserver observer);
}

public interface ICompleteObserver : IGrainObserver
{
    Task OnCompleted();
}

public class TestGrain : Grain, ITestGrain, IAsyncObserver<string>
{
    private List<string> _messages = new();
    private StreamSubscriptionHandle<string> _sub;
    private List<ICompleteObserver> _observers = new();

    public Task<string> Test()
    {
        return Task.FromResult("Hello, World!");
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        if(_sub is {} sub)
            await sub?.UnsubscribeAsync();
    }

    public async Task Test2()
    {
        var streamProvider = this.GetStreamProvider("StreamProvider");
        var streamId = StreamId.Create("MyStreamNamespace", Guid.Empty);
        var stream = streamProvider.GetStream<string>(streamId);
        _sub = await stream.SubscribeAsync(this);
    }
    
    public Task Subscribe(ICompleteObserver observer)
    {
        _observers.Add(observer);
        return Task.CompletedTask;
    }

    public Task<List<string>> Message()
    {
        return Task.FromResult(_messages);
    }
    
    public async Task OnNextAsync(string item, StreamSequenceToken? token = null)
    {
        _messages.Add(item);
        foreach (var completeObserver in _observers)
        {
            await completeObserver.OnCompleted();
        }
    }

    public Task OnCompletedAsync()
    {
        return Task.CompletedTask;
    }

    public Task OnErrorAsync(Exception ex)
    {
        return Task.CompletedTask;
    }
}