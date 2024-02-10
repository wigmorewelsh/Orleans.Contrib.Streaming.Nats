using NATS.Client.Core;
using Orleans.Serialization;

namespace Orleans.Contrib.Streaming.Nats;

public class NatsOrleansSerilizerRegistry : INatsSerializerRegistry
{
    private readonly Serializer _serialize;

    public NatsOrleansSerilizerRegistry(Serializer serialize)
    {
        _serialize = serialize;
    }

    public INatsSerialize<T> GetSerializer<T>()
    {
        return new NatsOrleansSerializer<T>(_serialize.GetSerializer<T>());
    }

    public INatsDeserialize<T> GetDeserializer<T>()
    {
        return new NatsOrleansSerializer<T>(_serialize.GetSerializer<T>()); 
    }
}