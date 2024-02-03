using NATS.Client.JetStream;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Streams;

namespace Orleans.Contrib.Streaming.Nats;

public class NatsBatchContainer : IBatchContainer
{
    private readonly Serializer _serializer;
    private List<NatsJSMsg<NatsMessage>> _messages = new List<NatsJSMsg<NatsMessage>>();
    public IReadOnlyList<NatsJSMsg<NatsMessage>> Messages => _messages;
    public NatsBatchContainer(StreamId streamId, NatsStreamSequenceToken sequenceToken, Serializer serializer)
    {
        _serializer = serializer;
        StreamId = streamId;
        SequenceToken = sequenceToken;
    }

    public IEnumerable<Tuple<T, StreamSequenceToken>> GetEvents<T>()
    {
        foreach (var message in _messages)
        {
            var @event = _serializer.Deserialize<T>(message.Data.Data);
            yield return new Tuple<T, StreamSequenceToken>(@event, new NatsStreamSequenceToken(message.Metadata.Value.Sequence));
        } 
    }

    public bool ImportRequestContext() => false;

    public StreamId StreamId { get; }
    public StreamSequenceToken SequenceToken { get; }

    public void AddMessage(NatsJSMsg<NatsMessage> message)
    {
        _messages.Add(message);
    }
}