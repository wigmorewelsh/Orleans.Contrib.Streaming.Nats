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
    private readonly NatsStreamOptions _streamOptions;
    private readonly INatsMessageBodySerializer _serializer;
    private readonly HashRingBasedStreamQueueMapper _mapper;
    private readonly ILogger _logger;

    public NatsAdaptor(NatsJSContext context,
        string name,
        NatsStreamOptions streamOptions,
        INatsMessageBodySerializer serializer,
        HashRingBasedStreamQueueMapper mapper,
        ILogger logger)
    {
        _context = context;
        _streamOptions = streamOptions;
        Name = name;
        _serializer = serializer;
        _mapper = mapper;
        _logger = logger;
    }

    private string StreamName()
    {
        return _streamOptions.StreamName ?? Name;
    }
    
    public async Task QueueMessageBatchAsync<T>(StreamId streamId, IEnumerable<T> events, StreamSequenceToken? token,
        Dictionary<string, object> requestContext)
    {
        var queueId = _mapper.GetQueueForStream(streamId);
        await CheckStreamExists();

        var message = new MemoryMessageBody(events.Cast<object>(), requestContext);
        var natsJsPubOpts = new NatsJSPubOpts()
        {
            ExpectedLastSubjectSequence = (ulong?)token?.SequenceNumber
        };
       
        var subject = NatsSubjects.ToSubject(StreamName(), queueId, streamId);

        _logger.LogInformation("Publishing message to {Subject}", subject);
        var serilizer = new NatsMemoryMessageBodySerializer(_serializer);
        await _context.PublishAsync(subject, message, opts: natsJsPubOpts, serializer: serilizer);
    }

    private async Task CheckStreamExists()
    {
        var streamConfig = new StreamConfig(StreamName(), [$"{StreamName()}.>"])
        {
            MaxAge = TimeSpan.FromMinutes(10),
            Retention = StreamConfigRetention.Interest
        };
        try
        {
            var stream = await _context.GetStreamAsync(StreamName());
            await stream.UpdateAsync(streamConfig);
        }
        catch (Exception err)
        {
            _logger.LogInformation("Creating stream {Stream}", StreamName());
       
            await _context.CreateStreamAsync(streamConfig);
        }
    }

    public IQueueAdapterReceiver CreateReceiver(QueueId queueId)
    {
        return new NatsQueueAdapterReceiver(StreamName(), _context, queueId, _serializer, _logger);
    }

    public string Name { get; }
    public bool IsRewindable => true;
    public StreamProviderDirection Direction => StreamProviderDirection.ReadWrite;
}