using System.Buffers;
using Orleans.Providers;

namespace Orleans.Contrib.Streaming.NATS;

/// <summary>
/// Implementations of this interface are responsible for serializing MemoryMessageBody objects
/// </summary>
public interface INatsMessageBodySerializer
{
    /// <summary>
    /// Serialize <see cref="MemoryMessageBody"/> to an array segment of bytes.
    /// </summary>
    /// <param name="body">The body.</param>
    /// <param name="bufferWriter">The buffer writer.</param>
    /// <returns>The serialized data.</returns>
    public void Serialize(MemoryMessageBody body, IBufferWriter<byte> bufferWriter);

    /// <summary>
    /// Deserialize an array segment into a <see cref="MemoryMessageBody"/>
    /// </summary>
    /// <param name="bodyBytes">The body bytes.</param>
    /// <returns>The deserialized message body.</returns>
    MemoryMessageBody Deserialize(ReadOnlySequence<byte> bodyBytes);
}