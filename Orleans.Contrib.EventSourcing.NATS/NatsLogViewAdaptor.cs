using NATS.Client.Core;
using NATS.Client.JetStream;
using Orleans.EventSourcing;
using Orleans.Storage;

namespace Orleans.Contrib.EventSourcing.NATS;

public class NatsLogViewAdaptor<TLogView, TLogEntry> : ILogViewAdaptor<TLogView, TLogEntry> where TLogView : new()
{
    private readonly ILogViewAdaptorHost<TLogView, TLogEntry> _hostGrain;
    private readonly ILogConsistencyProtocolServices _services;
    private readonly NatsJSContext _js;

    public NatsLogViewAdaptor(ILogViewAdaptorHost<TLogView, TLogEntry> hostGrain, object initialState, IGrainStorage grainStorage, string grainTypeName, ILogConsistencyProtocolServices services)
    {
        _hostGrain = hostGrain;
        _services = services;
        var nats = new NatsConnection();
        var js = new NatsJSContext(nats);
        _js = js;
    }

    public Task<IReadOnlyList<TLogEntry>> RetrieveLogSegment(int fromVersion, int toVersion)
    {
        throw new NotImplementedException();
    }

    public TLogView TentativeView { get; }
    public TLogView ConfirmedView { get; }
    public int ConfirmedVersion { get; }
    public IEnumerable<TLogEntry> UnconfirmedSuffix { get; }

    public void Submit(TLogEntry entry)
    {
        throw new NotImplementedException();
    }

    public void SubmitRange(IEnumerable<TLogEntry> entries)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> TryAppend(TLogEntry entry)
    {
        var response = await _js.PublishAsync("test", entry);
        return response.IsSuccess();
    }

    public async Task<bool> TryAppendRange(IEnumerable<TLogEntry> entries)
    {
        foreach (var entry in entries)
        {
            var response = await _js.PublishAsync("test", entry);
            if (!response.IsSuccess())
            {
                return false;
            }
        }

        return true;
    }

    public Task ConfirmSubmittedEntries()
    {
        return Task.CompletedTask;
    }

    public async Task Synchronize()
    {
        var stream = await _js.GetStreamAsync("test");
        var consumer = await stream.CreateOrderedConsumerAsync();
        var messages = consumer.FetchNoWaitAsync<TLogEntry>(new NatsJSFetchOpts() { });
        await foreach (var message in messages)
            if(message.Data is {} entry)
                _hostGrain.UpdateView(new TLogView(), entry);
    }

    public void EnableStatsCollection()
    {
    }

    public void DisableStatsCollection()
    {
    }

    public LogConsistencyStatistics GetStats()
    {
        return new LogConsistencyStatistics();
    }

    public Task PreOnActivate()
    {
        return Task.CompletedTask;
    }

    public Task PostOnActivate()
    {
        return Task.CompletedTask;
    }

    public Task PostOnDeactivate()
    {
        return Task.CompletedTask;
    }
}