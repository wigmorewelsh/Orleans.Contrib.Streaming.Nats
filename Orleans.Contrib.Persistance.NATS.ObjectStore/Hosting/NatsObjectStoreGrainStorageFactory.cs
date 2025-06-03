using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Orleans.Contrib.Persistance.NATS.ObjectStore;

public static class NatsObjectStoreGrainStorageFactory
{
    public static NatsObjectStoreGrainStorage Create(IServiceProvider services, object? name)
    {
        Debug.Assert(name != null, nameof(name) + " != null");
        return ActivatorUtilities.CreateInstance<NatsObjectStoreGrainStorage>(services,
            services.GetRequiredService<IOptionsMonitor<Configuration.NatsObjectStoreGrainStorageOptions>>(),
            name,
            services.GetRequiredService<ILogger<NatsObjectStoreGrainStorage>>()
        );
    }
}

