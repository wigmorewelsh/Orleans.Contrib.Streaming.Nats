using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using Orleans.Providers;
using Orleans.Providers.Streams.Common;
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
            var consumerConfig = new ConsumerConfig(_queueId.ToString())
            {
                DurableName = _queueId.ToString(),
                FilterSubject = $"{_name}.{_queueId}.>",
                DeliverPolicy = ConsumerConfigDeliverPolicy.New,
                AckPolicy = ConsumerConfigAckPolicy.All,
            };
            _consumer = await _context.CreateOrUpdateConsumerAsync(_name, consumerConfig);
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
            var natsJsFetchOpts = new NatsJSFetchOpts()
            {
                MaxMsgs = maxCount
            };

            await foreach (var message in _consumer.FetchNoWaitAsync(natsJsFetchOpts, serializer))
            {
                _logger.LogDebug("Received message {Subject}", message.Subject);

                var messageSubject = message.Subject;

                var streamId = NatsSubjects.FromSubject(messageSubject);

                var (stream, _) = message.Metadata!.Value.Sequence;

                var natsStreamSequenceToken = new EventSequenceTokenV2((long)stream);

                var batch = new NatsBatchContainer(streamId,
                    natsStreamSequenceToken, message.Data?.Events,
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
            var mostRecentMessage = messages
                .OfType<NatsBatchContainer>()
                .MaxBy(x => x.SequenceToken.SequenceNumber);
            
            if (mostRecentMessage == null) return;

            await _context.PublishAsync(mostRecentMessage.ReplyTo!, NatsJSConstants.Ack);
        }
        catch (Exception err)
        {
            _logger.LogError(err, "Error delivering messages");
        }
    }

    public Task Shutdown(TimeSpan timeout)
    {
        return Task.CompletedTask;
    }
}