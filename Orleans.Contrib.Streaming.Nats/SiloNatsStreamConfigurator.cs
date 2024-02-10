using Microsoft.Extensions.DependencyInjection;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers;

namespace Orleans.Contrib.Streaming.Nats;

public class SiloNatsStreamConfigurator<TSerializer> : SiloRecoverableStreamConfigurator, ISiloMemoryStreamConfigurator
    where TSerializer : class, IMemoryMessageBodySerializer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SiloMemoryStreamConfigurator{TSerializer}"/> class.
    /// </summary>
    /// <param name="name">The stream provider name.</param>
    /// <param name="configureServicesDelegate">The services configuration delegate.</param>
    public SiloNatsStreamConfigurator(
        string name, Action<Action<IServiceCollection>> configureServicesDelegate)
        : base(name, configureServicesDelegate, NatsQueueAdapterFactory.Create)
    {
        this.ConfigureDelegate(services => services.ConfigureNamedOptionForLogging<HashRingStreamQueueMapperOptions>(name));
    }
}