using NATS.Extensions.Microsoft.DependencyInjection;

namespace Orleans.Contrib.Streaming.NATS;

public interface ISiloNatsStreamConfigurator : ISiloRecoverableStreamConfigurator
{
    public void ConfigureNats(Action<NatsBuilder>? configure = null);
    void ConfigureStream(Action<NatsStreamOptions> configure);
    void ConfigureConsumers(Action<NatsConsumerOptions> configure);
}