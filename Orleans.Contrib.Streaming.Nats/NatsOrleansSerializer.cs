using System.Buffers;
using NATS.Client.Core;
using Orleans.Providers;
using Orleans.Serialization;

namespace Orleans.Contrib.Streaming.Nats;

public class NatsOrleansSerializer<T> : INatsSerialize<T>, INatsDeserialize<T>
{
    private readonly Serializer<T> _serializer;

    public NatsOrleansSerializer(Serializer<T> serializer)
    {
        _serializer = serializer;
    }

    public void Serialize(IBufferWriter<byte> bufferWriter, T value)
    {
        _serializer.Serialize(value, bufferWriter);
    }

    public T? Deserialize(in ReadOnlySequence<byte> buffer)
    {
        return _serializer.Deserialize(buffer);
    }
}

