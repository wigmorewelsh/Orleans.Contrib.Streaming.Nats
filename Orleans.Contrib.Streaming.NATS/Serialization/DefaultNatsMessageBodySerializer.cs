using System.Buffers;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Providers;
using Orleans.Serialization;

namespace Orleans.Contrib.Streaming.NATS;

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