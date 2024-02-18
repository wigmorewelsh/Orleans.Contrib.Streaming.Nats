using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Storage;

namespace Orleans.Contrib.Persistance.NatsKv;

/// <summary>Factory for creating MemoryGrainStorage</summary>
public static class NatsGrainStorageFactory
{
    /// <summary>
    /// Creates a new <see cref="T:Orleans.Storage.MemoryGrainStorage" /> instance.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="name">The name.</param>
    /// <returns>The storage.</returns>
    public static IGrainStorage Create(IServiceProvider services, string name) =>
        (IGrainStorage)ActivatorUtilities.CreateInstance<NatsGrainStorage>(services,
            (object)services.GetRequiredService<IOptionsMonitor<NatsGrainStorageOptions>>().Get(name), (object)name);
}