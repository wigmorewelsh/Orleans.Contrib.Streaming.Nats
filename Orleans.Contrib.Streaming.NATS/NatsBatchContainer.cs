using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Streams;

namespace Orleans.Contrib.Streaming.NATS;

[GenerateSerializer]
[Alias("Orleans.Contrib.Streaming.NATS.NatsBatchContainer")]
public class NatsBatchContainer : IBatchContainer
{
    // public NatsJSMsg<MemoryMessageBody> MessageData { get; }

    public NatsBatchContainer(StreamId streamId,
        NatsStreamSequenceToken sequenceToken, List<object>? dataEvents, string? replyTo)
    {
        Events = dataEvents;
        ReplyTo = replyTo;
        StreamId = streamId;
        SequenceToken = sequenceToken;
        realToken = new EventSequenceToken(sequenceToken.SequenceNumber);
    }

    public IEnumerable<Tuple<T, StreamSequenceToken>> GetEvents<T>()
    {
        if (Events == null) return [];
        
        return Events.Cast<T>().Select((e, i) => Tuple.Create<T, StreamSequenceToken>(e, realToken.CreateSequenceTokenForEvent(i)));
    }

    public bool ImportRequestContext() => false;

    [Id(0)]
    public StreamId StreamId { get; }
   
    [Id(1)]
    public StreamSequenceToken SequenceToken { get; }
    
    [Id(2)]
    public List<object>? Events { get; set; }
    
    [Id(3)]
    public string? ReplyTo { get; }
    [Id(4)]
    private readonly EventSequenceToken realToken;
}