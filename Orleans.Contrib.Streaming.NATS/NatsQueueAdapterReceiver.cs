using System.Text;
using Microsoft.Extensions.Logging;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Streams;

namespace Orleans.Contrib.Streaming.NATS;

public class NatsQueueAdapterReceiver : IQueueAdapterReceiver
{
    private readonly string _name;
    private readonly NatsJSContext _context;
    private readonly QueueId _queueId;
    private INatsJSConsumer? _consumer;
    private readonly INatsMessageBodySerializer _serializer;
    private readonly ILogger _logger;

    public NatsQueueAdapterReceiver(
        string name, 
        NatsJSContext context, 
        QueueId queueId, 
        INatsMessageBodySerializer serializer,
        ILogger logger)
    {
        _name = name;
        _context = context;
        _queueId = queueId;
        _serializer = serializer;
        _logger = logger;
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
            _consumer = await _context.GetConsumerAsync(_name, _queueId.ToString());
        }
        catch (Exception err)
        {
            _logger.LogInformation("Creating consumer {ConsumerName}", _name);
            _consumer = await _context.CreateOrUpdateConsumerAsync(_name,
                new ConsumerConfig(_queueId.ToString())
                {
                    DurableName = _queueId.ToString(),
                    FilterSubject = $"{_name}.{_queueId}.>",
                    DeliverPolicy = ConsumerConfigDeliverPolicy.New,
                    AckPolicy = ConsumerConfigAckPolicy.Explicit
                }); 
        }
    }

    private async Task CheckStreamExists()
    {
        try
        {
            var stream = await _context.GetStreamAsync(_name);
        }
        catch (Exception err)
        {
            _logger.LogInformation("Creating stream {StreamName}", _name);
            await _context.CreateStreamAsync(new StreamConfig(_name, new[] { $"{_name}.>" }));
        }
    }

    public async Task<IList<IBatchContainer>> GetQueueMessagesAsync(int maxCount)
    {
        if (_consumer == null) return new List<IBatchContainer>();

        try
        {
            var messages = new List<IBatchContainer>();
            var serializer = new NatsMemoryMessageBodySerializer(_serializer);
            await foreach (var message in _consumer.FetchAsync<MemoryMessageBody>(new NatsJSFetchOpts()
                               { MaxMsgs = maxCount, Expires = TimeSpan.FromSeconds(1) }, serializer: serializer))
            {
                _logger.LogDebug("Received message {Subject}", message.Subject);
                var rawStreamId = message.Subject.Split('.');
                var rawNamespace = Encoding.UTF8.GetBytes(rawStreamId[2]);
                var rawKey = Encoding.UTF8.GetBytes(rawStreamId[3]);
                var batch = new NatsBatchContainer(StreamId.Create(rawNamespace, rawKey),
                    new NatsStreamSequenceToken(message.Metadata.Value.Sequence), message.Data?.Events,
                    message.ReplyTo);
                messages.Add(batch);
            }

            return messages;
        } 
        catch (Exception err)
        {
            _logger.LogError(err, "Error receiving messages");
            return new List<IBatchContainer>();
        }
    }

    public async Task MessagesDeliveredAsync(IList<IBatchContainer> messages)
    {
        try
        {
            foreach (var batch in messages)
            {
                if (batch is not NatsBatchContainer natsBatchContainer) continue;

                if (natsBatchContainer.ReplyTo is { } replyTo)
                {
                    // this is faster than waiting for the ack to be confirmed
                    // and orleans tracks the sequence number for each consumer
                    _context.PublishConcurrentAsync(replyTo, NatsJSConstants.Ack).AsTask().Ignore();
                }

            }
        } catch (Exception err)
        {
            _logger.LogError(err, "Error delivering messages");
        }
    }

    public Task Shutdown(TimeSpan timeout)
    {
        return Task.CompletedTask;
    }
}