using Orleans.Storage;

namespace Orleans.Contrib.Persistance.NATS.ObjectStore.Configuration;

public class NatsObjectStoreGrainStorageOptions : IStorageProviderSerializerOptions
{
    public IGrainStorageSerializer GrainStorageSerializer { get; set; }
}

