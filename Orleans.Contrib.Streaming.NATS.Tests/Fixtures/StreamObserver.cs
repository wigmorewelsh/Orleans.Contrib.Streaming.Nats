using Orleans.Streams;

namespace Orleans.Contrib.Streaming.NATS.Tests.Fixtures;

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