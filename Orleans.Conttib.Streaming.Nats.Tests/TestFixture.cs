using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using Orleans.Configuration;
using Orleans.Contrib.Streaming.NATS;
using Orleans.Runtime;
using Orleans.TestingHost;

namespace Orleans.Conttib.Streaming.Nats.Tests;

public class TestFixture : IAsyncLifetime
{
    private TestCluster _host;
    private InProcessSiloHandle _silo;

    public IServiceProvider Services => _silo.SiloHost.Services;

    public class StartupToken
    {
        public TaskCompletionSource TaskCompletionSource { get; } = new TaskCompletionSource();
    }
    
    internal class SiloConfig : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Information);
                logging.AddConsole(cl => cl.LogToStandardErrorThreshold = LogLevel.Error);
            });
            siloBuilder.UseLocalhostClustering();
            siloBuilder.AddNatsStreams("StreamProvider", c =>
            {
                if (Environment.GetEnvironmentVariable("NATS_SERVER") is { } natserver)
                {
                    c.ConfigureNats(n =>
                    {
                        n.ConfigureOptions(o => o with { Url = natserver });
                    });
                }
                else
                {
                    c.ConfigureNats();
                }
            });
            siloBuilder.AddMemoryGrainStorage("PubSubStore");
        }
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        var builder = new TestClusterBuilder();

        builder.AddSiloBuilderConfigurator<SiloConfig>();

        _host = builder.Build();
        _silo = (InProcessSiloHandle)await _host.StartSiloAsync(1, new TestClusterOptions() { });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _host.DisposeAsync();
    }
}

