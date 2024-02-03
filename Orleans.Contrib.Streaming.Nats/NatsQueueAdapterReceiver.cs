using System.Text;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Streams;

namespace Orleans.Contrib.Streaming.Nats;

public class NatsQueueAdapterReceiver : IQueueAdapterReceiver
{
    private readonly NatsJSContext _context;
    private readonly QueueId _queueId;
    private readonly NatsJSContext _natsJsContext;
    private INatsJSConsumer _consumer;
    private Serializer _serializer;

    public NatsQueueAdapterReceiver(NatsJSContext context, QueueId queueId, Serializer serializer)
    {
        _context = context;
        _queueId = queueId;
        _serializer = serializer;
    }

    public async Task Initialize(TimeSpan timeout)
    {
        _consumer = await _context.CreateOrUpdateConsumerAsync(_queueId.ToString(), new ConsumerConfig(_queueId.ToString()) { });
    }

    public async Task<IList<IBatchContainer>> GetQueueMessagesAsync(int maxCount)
    {
        var messages = new Dictionary<string, NatsBatchContainer>();
        // a batch corrosponds to an orleans stream
        // since I'm mapping a stream to a nats subject they need to be grouped into a batch
        await foreach (var message in _consumer.FetchAsync<NatsMessage>(new NatsJSFetchOpts() { MaxMsgs = maxCount }))
        {
            if(!messages.TryGetValue(message.Subject, out var batch))
            {
                var bytes = Encoding.UTF8.GetBytes(message.Subject);
                batch = new NatsBatchContainer(StreamId.Parse(bytes), new NatsStreamSequenceToken(message.Metadata.Value.Sequence), _serializer);
                messages.Add(message.Subject, batch);
            }
            
            batch.AddMessage(message);
        }

        return messages.Values.Cast<IBatchContainer>().ToList();
    }

    public async Task MessagesDeliveredAsync(IList<IBatchContainer> messages)
    {
        foreach (var batch in messages)
        {
            if (batch is not NatsBatchContainer natsBatchContainer) continue;
            
            foreach (var message in natsBatchContainer.Messages) 
                await message.AckAsync();
        }
    }

    public Task Shutdown(TimeSpan timeout)
    {
        return Task.CompletedTask;
    }
}

public class NatsStreamSequenceToken : StreamSequenceToken
{
    public NatsStreamSequenceToken(NatsJSSequencePair valueSequence)
    {
    }

    public override bool Equals(StreamSequenceToken other)
    {
        throw new NotImplementedException();
    }

    public override int CompareTo(StreamSequenceToken other)
    {
        throw new NotImplementedException();
    }

    public override long SequenceNumber { get; protected set; }
    public override int EventIndex { get; protected set; }
}