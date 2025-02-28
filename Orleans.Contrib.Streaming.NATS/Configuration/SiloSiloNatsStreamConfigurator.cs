using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Hosting;
using NATS.Extensions.Microsoft.DependencyInjection;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Runtime;

namespace Orleans.Contrib.Streaming.NATS;

public class SiloSiloNatsStreamConfigurator<TSerializer> : SiloRecoverableStreamConfigurator, ISiloNatsStreamConfigurator
    where TSerializer : class, INatsMessageBodySerializer
{
    private readonly string _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="SiloMemoryStreamConfigurator{TSerializer}"/> class.
    /// </summary>
    /// <param name="name">The stream provider name.</param>
    /// <param name="configureServicesDelegate">The services configuration delegate.</param>
    public SiloSiloNatsStreamConfigurator(
        string name, Action<Action<IServiceCollection>> configureServicesDelegate)
        : base(name, configureServicesDelegate, NatsQueueAdapterFactory.Create)
    {
        _name = name;
        this.ConfigureDelegate(services =>
        {
            services.AddKeyedTransient<INatsMessageBodySerializer, TSerializer>(name);
            services.ConfigureNamedOptionForLogging<HashRingStreamQueueMapperOptions>(name);
        });
    }

    public void ConfigureNats(Action<NatsBuilder>? configure = null)
    {
        ConfigureDelegate(services =>
        {
            void BuildAction(NatsBuilder c)
            {
                c.WithKey(_name);
                configure?.Invoke(c);
            }

            services.AddNatsClient(BuildAction);
        });
    }
    
    public void ConfigureStream(Action<NatsStreamOptions> configure)
    {
        ConfigureDelegate(services =>
        {
            services.ConfigureNamedOptionForLogging<NatsStreamOptions>(_name);
            services.Configure(_name, configure);
        });
    }
    
    public void ConfigureConsumers(Action<NatsConsumerOptions> configure)
    {
        ConfigureDelegate(services =>
        {
            services.ConfigureNamedOptionForLogging<NatsConsumerOptions>(_name);
            services.Configure(_name, configure);
        });
    }
}

public class NatsConsumerOptions
{
    public bool CreateConsumer { get; set; } = true;
}

public class NatsStreamOptions
{
    public string? StreamName { get; set; }
    public bool CreateStream { get; set; } = true;
}