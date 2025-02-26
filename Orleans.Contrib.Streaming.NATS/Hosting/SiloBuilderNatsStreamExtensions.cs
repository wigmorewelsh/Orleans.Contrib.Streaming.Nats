using Orleans.Hosting;

namespace Orleans.Contrib.Streaming.NATS;

/// <summary>
/// <see cref="ISiloBuilder"/> extension methods for configuring nats streams. 
/// </summary>
public static class SiloBuilderNatsStreamExtensions
{
    /// <summary>
    /// Configure silo to use nats streams, using the default message serializer
    /// (<see cref="DefaultNatsMessageBodySerializer"/>).
    /// </summary>
    /// using the default built-in serializer
    /// <param name="builder">The builder.</param>
    /// <param name="name">The stream provider name.</param>
    /// <param name="configure">The configuration delegate.</param>
    /// <returns>The silo builder.</returns>
    public static ISiloBuilder AddNatsStreams(this ISiloBuilder builder, string name,
        Action<ISiloNatsStreamConfigurator>? configure = null)
    {
        return AddNatsStreams<DefaultNatsMessageBodySerializer>(builder, name, configure);
    }

    /// <summary>
    /// Configure silo to use nats streams.
    /// </summary>
    /// <typeparam name="TSerializer">The message serializer type, which must implement <see cref="INatsMessageBodySerializer"/>.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="name">The stream provider name.</param>
    /// <param name="configure">The configuration delegate.</param>
    /// <returns>The silo builder.</returns>
    public static ISiloBuilder AddNatsStreams<TSerializer>(this ISiloBuilder builder, string name,
        Action<ISiloNatsStreamConfigurator>? configure = null)
        where TSerializer : class, INatsMessageBodySerializer
    {
        //the constructor wire up DI with all default components of the streams , so need to be called regardless of configureStream null or not
        var natsStreamConfigurator = new SiloSiloNatsStreamConfigurator<TSerializer>(name,
            configureDelegate => builder.ConfigureServices(configureDelegate)
        );
        configure?.Invoke(natsStreamConfigurator);
        return builder;
    }
}