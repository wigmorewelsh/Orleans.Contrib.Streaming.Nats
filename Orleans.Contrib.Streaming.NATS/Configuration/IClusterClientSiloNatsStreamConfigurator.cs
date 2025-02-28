using NATS.Extensions.Microsoft.DependencyInjection;

namespace Orleans.Contrib.Streaming.NATS;

public interface IClusterClientSiloNatsStreamConfigurator : ISiloNatsStreamConfigurator,
    IClusterClientPersistentStreamConfigurator
{
}