using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Orleans.Contrib.Streaming.Nats;
using Orleans.Runtime;

namespace Orleans.Conttib.Streaming.Nats.Tests;

public class TestFixture : IAsyncLifetime
{
    private IHost _host;
    private Task _task;

    public IServiceProvider Services => _host.Services;

    public class StartupToken
    {
        public TaskCompletionSource TaskCompletionSource { get; } = new TaskCompletionSource();
    }
    
    public class StartupTask : IStartupTask
    {
        private readonly StartupToken _token;

        public StartupTask(StartupToken token)
        {
            _token = token;
        }
        
        public Task Execute(CancellationToken cancellationToken)
        {
            _token.TaskCompletionSource.TrySetResult();
            return Task.CompletedTask;
        }
    }
    
    async Task IAsyncLifetime.InitializeAsync()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<StartupToken>();
        builder.UseOrleans(o =>
        {
            o.AddStartupTask<StartupTask>();
            o.UseLocalhostClustering();
            o.AddNatsStreams("StreamProvider");
            o.AddMemoryGrainStorage("PubSubStore");
        });

        _host = builder.Build();
        _task = Task.Run(async () =>
        {
            await _host.RunAsync();
        });
        await Task.Yield();
        var token = _host.Services.GetRequiredService<StartupToken>();
        await token.TaskCompletionSource.Task;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _host.StopAsync();
    }
}