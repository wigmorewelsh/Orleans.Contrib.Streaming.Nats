using System.Buffers;
using NATS.Client.Core;
using Orleans.Serialization;

namespace Orleans.Contrib.EventSourcing.NATS.Serialization;

public class NatsOrleansSerialize<T>(Serializer<T> getSerializer) : INatsSerialize<T>, INatsDeserialize<T>
{
    public void Serialize(IBufferWriter<byte> bufferWriter, T value)
    {
        getSerializer.Serialize(value, bufferWriter);
    }

    public T? Deserialize(in ReadOnlySequence<byte> buffer)
    {
        return getSerializer.Deserialize(buffer);
    }
}