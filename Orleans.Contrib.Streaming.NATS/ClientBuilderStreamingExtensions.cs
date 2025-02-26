using Microsoft.Extensions.DependencyInjection;
using NATS.Extensions.Microsoft.DependencyInjection;
using Orleans.Providers;

namespace Orleans.Contrib.Streaming.NATS;

public static class ClientBuilderStreamingExtensions
{
    public static IClientBuilder AddNatsStreams(
        this IClientBuilder builder,
        string name,
        Action<IClusterClientNatsStreamConfigurator>? configure = null)
    {
        return AddNatsStreams<DefaultNatsMessageBodySerializer>(builder, name, configure);
    }
    
    public static IClientBuilder AddNatsStreams<TSerializer>(
        this IClientBuilder builder,
        string name,
        Action<IClusterClientNatsStreamConfigurator>? configure = null)
        where TSerializer : class, INatsMessageBodySerializer
    {
        //the constructor wire up DI with all default components of the streams , so need to be called regardless of configureStream null or not
        var natsStreamConfigurator = new ClusterClientNatsStreamConfigurator<TSerializer>(name, builder);
        configure?.Invoke(natsStreamConfigurator);
        return builder;
    } 
}

public interface IClusterClientNatsStreamConfigurator : INatsStreamConfigurator, IClusterClientPersistentStreamConfigurator { }

public class ClusterClientNatsStreamConfigurator<TSerializer> : ClusterClientPersistentStreamConfigurator, IClusterClientNatsStreamConfigurator
    where TSerializer : class, INatsMessageBodySerializer
{
    private readonly string _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClusterClientNatsStreamConfigurator{TSerializer}"/> class.
    /// </summary>
    /// <param name="name">The stream provider name.</param>
    /// <param name="builder">The builder.</param>
    public ClusterClientNatsStreamConfigurator(string name, IClientBuilder builder)
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
}
