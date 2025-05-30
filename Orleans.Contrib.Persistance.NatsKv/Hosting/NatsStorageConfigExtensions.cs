using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Storage;

namespace Orleans.Contrib.Persistance.NatsKv;

public static class NatsStorageConfigExtensions
{
    public static ISiloBuilder AddNatsGrainStorageAsDefault(this ISiloBuilder builder, Action<OptionsBuilder<NatsGrainStorageOptions>> configureOptions = null)
    {
        return builder.AddNatsGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
    }
    
    public static ISiloBuilder AddNatsGrainStorage(
        this ISiloBuilder builder,
        string name,
        Action<OptionsBuilder<NatsGrainStorageOptions>>? configureOptions = null)
    {
        return builder.ConfigureServices(services =>
        {
            services.TryAddSingleton<INatsKVContext>(c =>
            {
                var connection = c.GetRequiredService<INatsConnection>();
                var jsContext = new NatsJSContext(connection);
                return new NatsKVContext(jsContext);
            });
            
            configureOptions?.Invoke(services.AddOptions<NatsGrainStorageOptions>(name));
            services.AddTransient<IPostConfigureOptions<NatsGrainStorageOptions>, DefaultStorageProviderSerializerOptionsConfigurator<NatsGrainStorageOptions>>();
            services.ConfigureNamedOptionForLogging<NatsGrainStorageOptions>(name);
            services.AddKeyedSingleton<IGrainStorage>(name, NatsGrainStorageFactory.Create);
            if (string.Equals(name, "Default", StringComparison.Ordinal))
                services.TryAddSingleton<IGrainStorage>(
                    sp => sp.GetRequiredKeyedService<IGrainStorage>("Default"));
        });
    }
}