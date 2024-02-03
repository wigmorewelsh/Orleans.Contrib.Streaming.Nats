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

    public NatsAdaptor(NatsJSContext context, string name, Serializer serializer)
    {
        _context = context;
        Name = name;
        _serializer = serializer;
    }

    public async Task QueueMessageBatchAsync<T>(StreamId streamId, IEnumerable<T> events, StreamSequenceToken token,
        Dictionary<string, object> requestContext)
    {
        foreach (var @event in events)
        {
           await _context.PublishAsync(streamId.GetKeyAsString(), @event,
                opts: new NatsJSPubOpts() { ExpectedLastSubjectSequence = (ulong)token.SequenceNumber });
        }
    }

    public IQueueAdapterReceiver CreateReceiver(QueueId queueId)
    {
        return new NatsQueueAdapterReceiver(_context, queueId, _serializer);
    }

    public string Name { get; }
    public bool IsRewindable => true;
    public StreamProviderDirection Direction => StreamProviderDirection.ReadWrite;
}