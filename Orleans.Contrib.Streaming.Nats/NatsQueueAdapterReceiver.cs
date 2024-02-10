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
    private INatsJSConsumer? _consumer;
    private Serializer _serializer;

    public NatsQueueAdapterReceiver(NatsJSContext context, QueueId queueId, Serializer serializer)
    {
        _context = context;
        _queueId = queueId;
        _serializer = serializer;
    }

    public async Task Initialize(TimeSpan timeout)
    {
        await CheckStreamExists();
        await EnsureConsumerExists();
    }

    private async Task EnsureConsumerExists()
    {
        try
        {
            _consumer = await _context.GetConsumerAsync("somestream", _queueId.ToString());
        }
        catch (Exception err)
        {
            _consumer = await _context.CreateOrUpdateConsumerAsync("somestream",
                new ConsumerConfig(_queueId.ToString())
                {
                    DurableName = _queueId.ToString(),
                    FilterSubject = $"somestream.{_queueId}.>",
                    DeliverPolicy = ConsumerConfigDeliverPolicy.New,
                    AckPolicy = ConsumerConfigAckPolicy.Explicit
                }); 
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

    public async Task<IList<IBatchContainer>> GetQueueMessagesAsync(int maxCount)
    {
        if (_consumer == null) return new List<IBatchContainer>();

        var messages = new Dictionary<string, NatsBatchContainer>();
        await foreach (var message in _consumer.FetchAsync<NatsMessage>(new NatsJSFetchOpts()
                           { MaxMsgs = maxCount, Expires = TimeSpan.FromSeconds(1) }))
        {
            if (!messages.TryGetValue(message.Subject, out var batch))
            {
                var rawStreamId = message.Subject.Split('.');
                var rawNamespace = Encoding.UTF8.GetBytes(rawStreamId[2]);
                var rawKey = Encoding.UTF8.GetBytes(rawStreamId[3]);
                batch = new NatsBatchContainer(StreamId.Create(rawNamespace, rawKey),
                    new NatsStreamSequenceToken(message.Metadata.Value.Sequence), _serializer);
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