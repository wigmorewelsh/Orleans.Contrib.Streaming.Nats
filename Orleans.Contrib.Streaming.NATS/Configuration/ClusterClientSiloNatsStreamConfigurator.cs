using Microsoft.Extensions.DependencyInjection;
using NATS.Extensions.Microsoft.DependencyInjection;

namespace Orleans.Contrib.Streaming.NATS;

public class ClusterClientSiloNatsStreamConfigurator<TSerializer> : ClusterClientPersistentStreamConfigurator, IClusterClientSiloNatsStreamConfigurator
    where TSerializer : class, INatsMessageBodySerializer
{
    private readonly string _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClusterClientSiloNatsStreamConfigurator{TSerializer}"/> class.
    /// </summary>
    /// <param name="name">The stream provider name.</param>
    /// <param name="builder">The builder.</param>
    public ClusterClientSiloNatsStreamConfigurator(string name, IClientBuilder builder)
        : base(name, builder, NatsQueueAdapterFactory.Create)
    {
        _name = name;
        ConfigureDelegate(services =>
        {
            services.AddKeyedTransient<INatsMessageBodySerializer, TSerializer>(name);
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
            services.Configure(_name, configure);
        });
    }

    public void ConfigureConsumers(Action<NatsConsumerOptions> configure)
    {
        ConfigureDelegate(services =>
        {
            services.Configure(_name, configure);
        });
    }
}