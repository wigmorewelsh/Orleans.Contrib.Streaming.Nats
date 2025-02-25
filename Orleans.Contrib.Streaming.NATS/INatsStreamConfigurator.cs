using NATS.Extensions.Microsoft.DependencyInjection;

namespace Orleans.Contrib.Streaming.NATS;

public interface INatsStreamConfigurator : ISiloRecoverableStreamConfigurator
{
    public void ConfigureNats(Action<NatsBuilder>? configure = null);
}