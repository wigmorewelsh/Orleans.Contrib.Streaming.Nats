using NATS.Client.Core;
using Orleans.Serialization;

namespace Orleans.Contrib.EventSourcing.NATS.Serialization;

public class NatsOrleansSerializerRegistry(Serializer serializer) : INatsSerializerRegistry
{
    public INatsSerialize<T> GetSerializer<T>()
    {
        return new NatsOrleansSerialize<T>(serializer.GetSerializer<T>());
    }

    public INatsDeserialize<T> GetDeserializer<T>()
    {
        return new NatsOrleansSerialize<T>(serializer.GetSerializer<T>());
    }
}