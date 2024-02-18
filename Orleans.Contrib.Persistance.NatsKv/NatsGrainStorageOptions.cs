using Orleans.Storage;

namespace Orleans.Contrib.Persistance.NatsKv;

public class NatsGrainStorageOptions : IStorageProviderSerializerOptions
{
    public IGrainStorageSerializer GrainStorageSerializer { get; set; }
}