using Orleans.Storage;

namespace Orleans.Contrib.Persistance.NATS.KeyValueStore;

public class NatsGrainStorageOptions : IStorageProviderSerializerOptions
{
    public IGrainStorageSerializer GrainStorageSerializer { get; set; }
}