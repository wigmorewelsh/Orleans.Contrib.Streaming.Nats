using System.Buffers;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers;
using Orleans.Runtime;

namespace Orleans.Contrib.Streaming.NATS;

public class SiloNatsStreamConfigurator<TSerializer> : SiloRecoverableStreamConfigurator, ISiloMemoryStreamConfigurator
    where TSerializer : class, INatsMessageBodySerializer
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
        this.ConfigureDelegate(services =>
        {
            services.AddTransientNamedService<INatsMessageBodySerializer, TSerializer>(name);
            services.ConfigureNamedOptionForLogging<HashRingStreamQueueMapperOptions>(name);
        });
    }
}

/// <summary>
/// Implementations of this interface are responsible for serializing MemoryMessageBody objects
/// </summary>
public interface INatsMessageBodySerializer
{
    /// <summary>
    /// Serialize <see cref="MemoryMessageBody"/> to an array segment of bytes.
    /// </summary>
    /// <param name="body">The body.</param>
    /// <returns>The serialized data.</returns>
    public void Serialize(MemoryMessageBody body, IBufferWriter<byte> bufferWriter);

    /// <summary>
    /// Deserialize an array segment into a <see cref="MemoryMessageBody"/>
    /// </summary>
    /// <param name="bodyBytes">The body bytes.</param>
    /// <returns>The deserialized message body.</returns>
    MemoryMessageBody Deserialize(ReadOnlySequence<byte> bodyBytes);
}