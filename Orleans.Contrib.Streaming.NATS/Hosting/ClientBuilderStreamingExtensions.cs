using Orleans.Providers;

namespace Orleans.Contrib.Streaming.NATS;

public static class ClientBuilderStreamingExtensions
{
    public static IClientBuilder AddNatsStreams(
        this IClientBuilder builder,
        string name,
        Action<IClusterClientSiloNatsStreamConfigurator>? configure = null)
    {
        return AddNatsStreams<DefaultNatsMessageBodySerializer>(builder, name, configure);
    }
    
    public static IClientBuilder AddNatsStreams<TSerializer>(
        this IClientBuilder builder,
        string name,
        Action<IClusterClientSiloNatsStreamConfigurator>? configure = null)
        where TSerializer : class, INatsMessageBodySerializer
    {
        //the constructor wire up DI with all default components of the streams , so need to be called regardless of configureStream null or not
        var natsStreamConfigurator = new ClusterClientSiloNatsStreamConfigurator<TSerializer>(name, builder);
        configure?.Invoke(natsStreamConfigurator);
        return builder;
    } 
}