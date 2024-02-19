using System.Buffers;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using Orleans.Providers;
using Orleans.Serialization;

namespace Orleans.Contrib.Streaming.Nats;

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
        Action<ISiloMemoryStreamConfigurator> configure = null)
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
        Action<ISiloMemoryStreamConfigurator> configure = null)
        where TSerializer : class, INatsMessageBodySerializer
    {
        //the constructor wire up DI with all default components of the streams , so need to be called regardless of configureStream null or not
        var natsStreamConfigurator = new SiloNatsStreamConfigurator<TSerializer>(name,
            configureDelegate => builder.ConfigureServices(configureDelegate)
        );
        configure?.Invoke(natsStreamConfigurator);
        return builder;
    }
}

/// <summary>
/// Default <see cref="IMemoryMessageBodySerializer"/> implementation.
/// </summary>
[Serializable, GenerateSerializer, Immutable]
[SerializationCallbacks(typeof(Runtime.OnDeserializedCallbacks))]
public sealed class DefaultNatsMessageBodySerializer : INatsMessageBodySerializer, IOnDeserialized
{
    [NonSerialized]
    private Serializer<MemoryMessageBody> serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultMemoryMessageBodySerializer" /> class.
    /// </summary>
    /// <param name="serializer">The serializer.</param>
    public DefaultNatsMessageBodySerializer(Serializer<MemoryMessageBody> serializer)
    {
        this.serializer = serializer;
    }

    /// <inheritdoc />
    public void Serialize(MemoryMessageBody body, IBufferWriter<byte> bufferWriter)
    {
        serializer.Serialize(body, bufferWriter);
    }

    /// <inheritdoc />
    public MemoryMessageBody Deserialize(ReadOnlySequence<byte> bodyBytes)
    {
        return serializer.Deserialize(bodyBytes);
    }

    /// <inheritdoc />
    void IOnDeserialized.OnDeserialized(DeserializationContext context)
    {
        this.serializer = context.ServiceProvider.GetRequiredService<Serializer<MemoryMessageBody>>();
    }
}