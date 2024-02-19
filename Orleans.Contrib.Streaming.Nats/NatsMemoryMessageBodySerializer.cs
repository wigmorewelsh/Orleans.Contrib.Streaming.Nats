using System.Buffers;
using NATS.Client.Core;
using Orleans.Providers;
using Orleans.Serialization;

namespace Orleans.Contrib.Streaming.Nats;

public class NatsMemoryMessageBodySerializer : INatsSerializer<MemoryMessageBody> 
{
    private readonly INatsMessageBodySerializer _serializer;

    public NatsMemoryMessageBodySerializer(INatsMessageBodySerializer serializer)
    {
        _serializer = serializer;
    }

    public void Serialize(IBufferWriter<byte> bufferWriter, MemoryMessageBody value)
    {
        _serializer.Serialize(value, bufferWriter); 
    }

    public MemoryMessageBody? Deserialize(in ReadOnlySequence<byte> buffer)
    {
        return _serializer.Deserialize(buffer);
    }
}