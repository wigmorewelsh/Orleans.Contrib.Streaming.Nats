using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using Orleans.Contrib.EventSourcing.NATS.Serialization;
using Orleans.EventSourcing;
using Orleans.Runtime;
using Orleans.Serialization;

namespace Orleans.Contrib.EventSourcing.NATS;

public class NatsLogViewAdaptor<TLogView, TLogEntry> : ILogViewAdaptor<TLogView, TLogEntry> where TLogView : class, new()
{
    private readonly ILogViewAdaptorHost<TLogView, TLogEntry> _hostGrain;
    private readonly ILogConsistencyProtocolServices _services;
    private readonly NatsJSContext _js;

    public NatsLogViewAdaptor(ILogViewAdaptorHost<TLogView, TLogEntry> hostGrain, object initialState, ILogConsistencyProtocolServices services,
        Serializer serializer)
    {
        _hostGrain = hostGrain;
        _services = services;
        var nats = new NatsConnection(NatsOpts.Default with { SerializerRegistry = new NatsOrleansSerializerRegistry(serializer) });
        var js = new NatsJSContext(nats);
        _js = js;
        
        ConfirmedView = initialState as TLogView ?? new TLogView();
    }
    
    private async Task CheckStreamExists()
    {
        var streamConfig = new StreamConfig(StreamName(), [$"{StreamName()}.>"])
        {
            Retention = StreamConfigRetention.Limits
        };
        try
        {
            var stream = await _js.GetStreamAsync(StreamName());
            await stream.UpdateAsync(streamConfig);
        }
        catch (Exception err)
        {
            // _logger.LogInformation("Creating stream {Stream}", StreamName());
       
            await _js.CreateStreamAsync(streamConfig);
        }
    }

    public async Task<IReadOnlyList<TLogEntry>> RetrieveLogSegment(int fromVersion, int toVersion)
    {
        var stream = await _js.GetStreamAsync(StreamName());
        
        throw new Exception();
    }
    
    private readonly Queue<TLogEntry> _queue = new Queue<TLogEntry>();

    public TLogView TentativeView { get; } = new();
    public TLogView ConfirmedView { get; }
    public int ConfirmedVersion { get; }
    public IEnumerable<TLogEntry> UnconfirmedSuffix => _queue;

    public void Submit(TLogEntry entry)
    {
        _queue.Enqueue(entry);
    }

    public void SubmitRange(IEnumerable<TLogEntry> entries)
    {
        foreach (var entry in entries)
        {
            _queue.Enqueue(entry);
        }
    }

    public async Task<bool> TryAppend(TLogEntry entry)
    {
        var subject = NatsSubject();

        var response = await _js.PublishAsync(subject, entry);
        if (!response.IsSuccess()) return false;
        
        _hostGrain.UpdateView(ConfirmedView, entry);
        return true;

    }

    private string StreamName()
    {
        return "test".ToUpperInvariant();
    }
    
    private string NatsSubject()
    {
        var key = _services.GrainId.ToString();
        var streamName = StreamName();
        var subject = $"{streamName}.{key}";
        return subject;
    }

    public async Task<bool> TryAppendRange(IEnumerable<TLogEntry> entries)
    {
        var subject = NatsSubject();
        foreach (var entry in entries)
        {
            var response = await _js.PublishAsync(subject, entry);
            if (!response.IsSuccess())
            {
                return false;
            }
            _hostGrain.UpdateView(ConfirmedView, entry);
        }

        return true;
    }

    public async Task ConfirmSubmittedEntries()
    {
        var subject = NatsSubject();

        while (_queue.TryDequeue(out var entry))
        {
            var response = await _js.PublishAsync(subject, entry);
            if (!response.IsSuccess())
            {
                // TODO: Handle failure to confirm the entry
                break;
            }
            _hostGrain.UpdateView(ConfirmedView, entry);
        }
    }

    public async Task Synchronize()
    {
        await CheckStreamExists();
        
        var subject = NatsSubject();
        var stream = await _js.GetStreamAsync(StreamName());
        var natsJsOrderedConsumerOpts = new NatsJSOrderedConsumerOpts()
        {
            FilterSubjects = [subject],
        };
        var consumer = await stream.CreateOrderedConsumerAsync(natsJsOrderedConsumerOpts);
        var natsJsFetchOpts = new NatsJSFetchOpts()
        {
            MaxMsgs = 10_000,
        };
        var messages = consumer.FetchNoWaitAsync<TLogEntry>(natsJsFetchOpts);
        await foreach (var message in messages)
            if (message.Data is { } entry)
                _hostGrain.UpdateView(ConfirmedView, entry);
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