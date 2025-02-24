using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
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
        return builder.ConfigureServices((Action<IServiceCollection>)(services =>
        {
            Action<OptionsBuilder<NatsGrainStorageOptions>>? action = configureOptions;
            if (action != null)
                action(services.AddOptions<NatsGrainStorageOptions>(name));
            services
                .AddTransient<IPostConfigureOptions<NatsGrainStorageOptions>,
                    DefaultStorageProviderSerializerOptionsConfigurator<NatsGrainStorageOptions>>();
            services.ConfigureNamedOptionForLogging<NatsGrainStorageOptions>(name);
            if (string.Equals(name, "Default", StringComparison.Ordinal))
                services.TryAddSingleton<IGrainStorage>(
                    (Func<IServiceProvider, IGrainStorage>)(sp => sp.GetRequiredKeyedService<IGrainStorage>("Default")));
            services.AddKeyedSingleton<IGrainStorage>(name, NatsGrainStorageFactory.Create);
        }));
    }
}