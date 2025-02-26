namespace Orleans.Contrib.Streaming.NATS.Tests.Grains;

public interface ICompleteObserver : IGrainObserver
{
    Task OnCompleted();
}