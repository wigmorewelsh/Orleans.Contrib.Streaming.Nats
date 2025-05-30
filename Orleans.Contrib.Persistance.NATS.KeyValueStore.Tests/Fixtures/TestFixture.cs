using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NATS.Extensions.Microsoft.DependencyInjection;
using Orleans.Hosting;
using Orleans.TestingHost;
using Xunit;

namespace Orleans.Contrib.Persistance.NATS.KeyValueStore.Tests.Fixtures;

public interface ITestSettings
{
    public static abstract string StreamName { get; }
}

// ReSharper disable once ClassNeverInstantiated.Global
public class TestFixture<TTestSettings> : IAsyncLifetime where TTestSettings : ITestSettings
{
    private TestCluster _host = null!;
    private InProcessSiloHandle _silo = null!;

    public IServiceProvider Services => _silo.SiloHost.Services;
    public IClusterClient Client => _host.Client;

    public class StartupToken
    {
        public TaskCompletionSource TaskCompletionSource { get; } = new TaskCompletionSource();
    }

    internal class SiloBuilderConfigurator<TTestSettings> : ISiloConfigurator where TTestSettings : ITestSettings
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Information);
                logging.AddConsole(cl => cl.LogToStandardErrorThreshold = LogLevel.Error);
            });
            siloBuilder.Services.AddNatsClient();
            siloBuilder.AddNatsGrainStorageAsDefault();
        }
    }

    private class ClientBuilderConfigurator<TTestSettings> : IClientBuilderConfigurator where TTestSettings : ITestSettings
    {
        public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
        {
        }
    }

    public async Task KillClientAsync()
    {
        await _host.KillClientAsync();
        // make sure dead client has had time to drop
        await Task.Delay(TimeSpan.FromMinutes(1) + TimeSpan.FromSeconds(5));
        await _host.InitializeClientAsync();
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        // if (Environment.GetEnvironmentVariable("NATS_SERVER") is { } natserver)
        // {
        //     var nats = new NatsConnection(new NatsOpts(){ Url = natserver });
        //     await nats.ConnectAsync();
        //     var context = new NatsJSContext(nats);
        //     await context.PurgeStreamAsync("StreamProvider", new StreamPurgeRequest());
        // }
        // else
        // {
        //     var nats = new NatsConnection();
        //     await nats.ConnectAsync();
        //     var context = new NatsJSContext(nats);
        //     await context.PurgeStreamAsync("StreamProvider", new StreamPurgeRequest());
        // } 

        var builder = new TestClusterBuilder
        {
            Options =
            {
                InitialSilosCount = 1
            }
        };

        builder.AddSiloBuilderConfigurator<SiloBuilderConfigurator<TTestSettings>>();
        builder.AddClientBuilderConfigurator<ClientBuilderConfigurator<TTestSettings>>();

        _host = builder.Build();
        await _host.DeployAsync();
        _silo = (InProcessSiloHandle)_host.Primary;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _host.DisposeAsync();
    }

    public async Task RestartSilo()
    {
        await _host.RestartSiloAsync(_silo);
    }
}