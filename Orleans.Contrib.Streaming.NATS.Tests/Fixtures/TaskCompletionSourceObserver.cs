using Orleans.Contrib.Streaming.NATS.Tests.Grains;

namespace Orleans.Contrib.Streaming.NATS.Tests;

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