using System.Text;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Streams;

namespace Orleans.Contrib.Streaming.Nats;

public class NatsAdaptor : IQueueAdapter
{
    private readonly NatsJSContext _context;
    private Serializer _serializer;
    private readonly HashRingBasedStreamQueueMapper _hashRingBasedStreamQueueMapper;

    public NatsAdaptor(NatsJSContext context, string name, Serializer serializer,
        HashRingBasedStreamQueueMapper hashRingBasedStreamQueueMapper)
    {
        _context = context;
        Name = name;
        _serializer = serializer;
        _hashRingBasedStreamQueueMapper = hashRingBasedStreamQueueMapper;
    }

    public async Task QueueMessageBatchAsync<T>(StreamId streamId, IEnumerable<T> events, StreamSequenceToken? token,
        Dictionary<string, object> requestContext)
    {
        var queueId = _hashRingBasedStreamQueueMapper.GetQueueForStream(streamId);
        await CheckStreamExists();

        foreach (var @event in events)
        {
            var natsJsPubOpts = new NatsJSPubOpts() { ExpectedLastSubjectSequence = (ulong?)token?.SequenceNumber };
            using var ms = new MemoryStream();
            _serializer.GetSerializer<T>().Serialize(@event, ms);
            ms.Position = 0;
            var natsMessage = new NatsMessage()
            {
                Type = typeof(T),
                Data = ms.ToArray()
            };
            var @namespace = Encoding.UTF8.GetString(streamId.Namespace.Span); 
            var key = Encoding.UTF8.GetString(streamId.Key.Span);
            await _context.PublishAsync($"somestream.{queueId}.{@namespace}.{key}", natsMessage,
                opts: natsJsPubOpts);
        }
    }

    private async Task CheckStreamExists()
    {
        try
        {
            var stream = await _context.GetStreamAsync("somestream");
        }
        catch (Exception err)
        {
            await _context.CreateStreamAsync(new StreamConfig("somestream", new[] { "somestream.>" }));
        }
    }

    public IQueueAdapterReceiver CreateReceiver(QueueId queueId)
    {
        var natsQueueAdapterReceiver = new NatsQueueAdapterReceiver(_context, queueId, _serializer);
        return natsQueueAdapterReceiver;
    }

    public string Name { get; }
    public bool IsRewindable => true;
    public StreamProviderDirection Direction => StreamProviderDirection.ReadWrite;
}