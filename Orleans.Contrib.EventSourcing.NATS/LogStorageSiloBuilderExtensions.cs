
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orleans.Contrib.EventSourcing.NATS;
using Orleans.EventSourcing;
using Orleans.Providers;

namespace Orleans.Hosting;

public static class LogStorageSiloBuilderExtensions
{
    /// <summary>
    /// Adds a log storage log consistency provider as default consistency provider"/>
    /// </summary>
    public static ISiloBuilder AddNatsLogConsistencyProviderAsDefault(this ISiloBuilder builder)
    {
        return builder.AddLogStorageBasedLogConsistencyProvider(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME);
    }

    /// <summary>
    /// Adds a log storage log consistency provider"/>
    /// </summary>
    public static ISiloBuilder AddNatsLogConsistencyProvider(this ISiloBuilder builder, string name = "LogStorage")
    {
        return builder.ConfigureServices(services => services.AddNatsLogConsistencyProvider(name));
    }

    internal static IServiceCollection AddNatsLogConsistencyProvider(this IServiceCollection services, string name)
    {
        services.AddLogConsistencyProtocolServicesFactory();
        services.TryAddSingleton<ILogViewAdaptorFactory>(sp => sp.GetRequiredKeyedService<ILogViewAdaptorFactory>(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME));
        return services.AddKeyedSingleton<ILogViewAdaptorFactory, NatsLogViewAdaptorFactory>(name);
    }
}