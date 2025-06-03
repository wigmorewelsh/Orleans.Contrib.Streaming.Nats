using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.ObjectStore;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Storage;
using Orleans.Runtime.Hosting;
using Orleans.Contrib.Persistance.NATS.ObjectStore.Configuration;

namespace Orleans.Contrib.Persistance.NATS.ObjectStore.Hosting;

public static class NatsObjectStoreStorageConfigExtensions
{
    public static ISiloBuilder AddNatsObjectStoreGrainStorageAsDefault(this ISiloBuilder builder, Action<OptionsBuilder<NatsObjectStoreGrainStorageOptions>>? configureOptions = null)
    {
        return builder.AddNatsObjectStoreGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
    }

    public static ISiloBuilder AddNatsObjectStoreGrainStorage(
        this ISiloBuilder builder,
        string name,
        Action<OptionsBuilder<NatsObjectStoreGrainStorageOptions>>? configureOptions = null)
    {
        return builder.ConfigureServices(services =>
        {
            services.TryAddSingleton<INatsObjectStoreContext>(c =>
            {
                var client = c.GetRequiredService<INatsConnection>();
                var js = new NatsJSContext(client);
                var context = new NatsObjContext(js);
                return new NatsObjectStoreContext(context);
            });

            configureOptions?.Invoke(services.AddOptions<NatsObjectStoreGrainStorageOptions>(name));
            services.AddTransient<IPostConfigureOptions<NatsObjectStoreGrainStorageOptions>, DefaultStorageProviderSerializerOptionsConfigurator<NatsObjectStoreGrainStorageOptions>>();
            services.ConfigureNamedOptionForLogging<NatsObjectStoreGrainStorageOptions>(name);
            services.AddGrainStorage(name, NatsObjectStoreGrainStorageFactory.Create);

            if (string.Equals(name, "Default", StringComparison.Ordinal))
                services.TryAddSingleton<IGrainStorage>(
                    sp => sp.GetRequiredKeyedService<IGrainStorage>("Default"));
        });
    }
}
