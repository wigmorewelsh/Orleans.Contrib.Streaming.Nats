using System.Text;
using Microsoft.Extensions.Logging;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Streams;

namespace Orleans.Contrib.Streaming.NATS;

public class NatsAdaptor : IQueueAdapter
{
    private readonly NatsJSContext _context;
    private readonly INatsMessageBodySerializer _serializer;
    private readonly HashRingBasedStreamQueueMapper _mapper;
    private readonly ILogger _logger;

    public NatsAdaptor(
        NatsJSContext context, 
        string name, 
        INatsMessageBodySerializer serializer,
        HashRingBasedStreamQueueMapper mapper,
        ILogger logger)
    {
        _context = context;
        Name = name;
        _serializer = serializer;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task QueueMessageBatchAsync<T>(StreamId streamId, IEnumerable<T> events, StreamSequenceToken? token,
        Dictionary<string, object> requestContext)
    {
        var queueId = _mapper.GetQueueForStream(streamId);
        await CheckStreamExists();

        var message = new MemoryMessageBody(events.Cast<object>(), requestContext);
        var natsJsPubOpts = new NatsJSPubOpts() { ExpectedLastSubjectSequence = (ulong?)token?.SequenceNumber };
        var @namespace = Encoding.UTF8.GetString(streamId.Namespace.Span);
        var key = Encoding.UTF8.GetString(streamId.Key.Span);
        _logger.LogInformation("Publishing message to {Stream} {QueueId} {@namespace} {Key}", Name, queueId, @namespace, key);
        var serilizer = new NatsMemoryMessageBodySerializer(_serializer);
        await _context.PublishAsync($"{Name}.{queueId}.{@namespace}.{key}", message, opts: natsJsPubOpts, serializer: serilizer);
    }

    private async Task CheckStreamExists()
    {
        var streamConfig = new StreamConfig(Name, new[] { $"{Name}.>" })
        {
            Discard = StreamConfigDiscard.New,
            DiscardNewPerSubject = true,
            MaxMsgsPerSubject = 1000,
            MaxBytes = 10 * 1024 * 1024,
            MaxAge = TimeSpan.FromDays(2),
            Retention = StreamConfigRetention.Interest
        };
        try
        {
            var stream = await _context.GetStreamAsync(Name);
            await stream.UpdateAsync(streamConfig);
            // await stream.PurgeAsync(new StreamPurgeRequest());
        }
        catch (Exception err)
        {
            _logger.LogInformation("Creating stream {Stream}", Name);
       
            await _context.CreateStreamAsync(streamConfig);
        }
    }

    public IQueueAdapterReceiver CreateReceiver(QueueId queueId)
    {
        return new NatsQueueAdapterReceiver(Name, _context, queueId, _serializer, _logger);
    }

    public string Name { get; }
    public bool IsRewindable => true;
    public StreamProviderDirection Direction => StreamProviderDirection.ReadWrite;
}