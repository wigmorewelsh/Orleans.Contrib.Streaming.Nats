using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NATS.Extensions.Microsoft.DependencyInjection;
using Orleans.Contrib.Persistance.NATS.ObjectStore.Hosting;
using Orleans.Hosting;
using Orleans.TestingHost;
using Xunit;

namespace Orleans.Contrib.Persistance.NATS.ObjectStore.Tests.Fixtures;

public interface ITestSettings
{
    public static abstract string StoreName { get; }
}

public class TestFixture<TTestSettings> : IAsyncLifetime where TTestSettings : ITestSettings
{
    private TestCluster _host = null!;
    private InProcessSiloHandle _silo = null!;

    public IServiceProvider Services => _silo.SiloHost.Services;
    public IClusterClient Client => _host.Client;

    internal class SiloBuilderConfigurator<T> : ISiloConfigurator where T : ITestSettings
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Information);
                logging.AddConsole(cl => cl.LogToStandardErrorThreshold = LogLevel.Error);
            });
            siloBuilder.Services.AddNatsClient();
            siloBuilder.AddNatsObjectStoreGrainStorageAsDefault();
        }
    }

    private class ClientBuilderConfigurator<T> : IClientBuilderConfigurator where T : ITestSettings
    {
        public void Configure(IConfiguration configuration, IClientBuilder clientBuilder) { }
    }

    public async Task KillClientAsync()
    {
        await _host.KillClientAsync();
        await Task.Delay(TimeSpan.FromSeconds(10));
        await _host.InitializeClientAsync();
    }

    public async Task InitializeAsync()
    {
        var builder = new TestClusterBuilder
        {
            Options = { InitialSilosCount = 1 }
        };
        builder.AddSiloBuilderConfigurator<SiloBuilderConfigurator<TTestSettings>>();
        builder.AddClientBuilderConfigurator<ClientBuilderConfigurator<TTestSettings>>();
        _host = builder.Build();
        await _host.DeployAsync();
        _silo = (InProcessSiloHandle)_host.Primary;
    }

    public async Task DisposeAsync()
    {
        await _host.DisposeAsync();
    }

    public async Task RestartSilo()
    {
        await _host.RestartSiloAsync(_silo);
    }
}
