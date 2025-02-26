using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Orleans.TestingHost;

namespace Orleans.Contrib.Streaming.NATS.Tests.Fixtures;

// ReSharper disable once ClassNeverInstantiated.Global
public class TestFixture : IAsyncLifetime
{
    private TestCluster _host = null!;
    private InProcessSiloHandle _silo = null!;

    public IServiceProvider Services => _silo.SiloHost.Services;
    public IClusterClient Client => _host.Client;

    public class StartupToken
    {
        public TaskCompletionSource TaskCompletionSource { get; } = new TaskCompletionSource();
    }
    
    internal class SiloBuilderConfigurator : ISiloConfigurator
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
    
    private class ClientBuilderConfigurator : IClientBuilderConfigurator
    {
        public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
        {
            clientBuilder
                .UseLocalhostClustering()
                .AddNatsStreams("StreamProvider", c =>
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
        }
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        var builder = new TestClusterBuilder();
        
        builder.AddSiloBuilderConfigurator<SiloBuilderConfigurator>();
        builder.AddClientBuilderConfigurator<ClientBuilderConfigurator>();

        _host = builder.Build();
        _silo = (InProcessSiloHandle)await _host.StartSiloAsync(1, new TestClusterOptions() { });
        await _host.InitializeClientAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _host.DisposeAsync();
    }
}

