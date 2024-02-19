using NATS.Client.JetStream;
using Orleans.Providers;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Streams;

namespace Orleans.Contrib.Streaming.Nats;

public class NatsBatchContainer : IBatchContainer
{
    public NatsJSMsg<MemoryMessageBody> MessageData { get; }
    private readonly INatsMessageBodySerializer _serializer;
    private readonly EventSequenceToken realToken;

    public NatsBatchContainer(StreamId streamId, NatsJSMsg<MemoryMessageBody> messageData, NatsStreamSequenceToken sequenceToken,
        INatsMessageBodySerializer serializer)
    {
        MessageData = messageData;
        _serializer = serializer;
        StreamId = streamId;
        SequenceToken = sequenceToken;
        realToken = new EventSequenceToken(sequenceToken.SequenceNumber);
    }

    public IEnumerable<Tuple<T, StreamSequenceToken>> GetEvents<T>()
    {
        return MessageData.Data.Events.Cast<T>().Select((e, i) => Tuple.Create<T, StreamSequenceToken>(e, realToken.CreateSequenceTokenForEvent(i)));
    }

    public bool ImportRequestContext() => false;

    public StreamId StreamId { get; }
    public StreamSequenceToken SequenceToken { get; }
}