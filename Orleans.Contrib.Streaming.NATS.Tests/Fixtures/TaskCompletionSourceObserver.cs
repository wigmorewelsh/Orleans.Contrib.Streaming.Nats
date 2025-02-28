using Orleans.Contrib.Streaming.NATS.Tests.Grains;

namespace Orleans.Contrib.Streaming.NATS.Tests;

public class TaskCompletionSourceObserver : ICompleteObserver
{
    public int Count = 0;
        
    private TaskCompletionSource _taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    private record WaitTask(int Count, TaskCompletionSource TaskCompletionSource);
    private readonly List<WaitTask> _waitTasks = new();
        
    public Task OnCompleted()
    {
        Count++;
        _taskCompletionSource.TrySetResult();
        foreach (var waitTask in _waitTasks.ToArray())
        {
            if (waitTask.Count <= Count)
            {
                waitTask.TaskCompletionSource.TrySetResult();
                _waitTasks.Remove(waitTask);
            }
        }
        return Task.CompletedTask;
    }

    public Task WaitFor(int count)
    {
        if(count <= Count)
        {
            return Task.CompletedTask;
        }
        var taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _waitTasks.Add(new WaitTask(count, taskCompletionSource));
        return taskCompletionSource.Task;
    } 
        
    public Task Task => _taskCompletionSource.Task;

    public void Reset()
    {
        _taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}