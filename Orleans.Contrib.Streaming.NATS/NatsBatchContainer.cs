using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Streams;

namespace Orleans.Contrib.Streaming.NATS;

[GenerateSerializer]
[Alias("Orleans.Contrib.Streaming.NATS.NatsBatchContainer")]
public class NatsBatchContainer : IBatchContainer
{
    public NatsBatchContainer(StreamId streamId,
        EventSequenceTokenV2 sequenceToken, List<object>? dataEvents, string? replyTo)
    {
        Events = dataEvents;
        ReplyTo = replyTo;
        StreamId = streamId;
        SequenceToken = sequenceToken;
    }

    public IEnumerable<Tuple<T, StreamSequenceToken>> GetEvents<T>()
    {
        if (Events == null) yield break;
        
        var sequenceToken = (SequenceToken as EventSequenceTokenV2)!;

        var i = 0;
        foreach (var evt in Events.Cast<T>())
        {
            yield return Tuple.Create<T, StreamSequenceToken>(evt, sequenceToken.CreateSequenceTokenForEvent(i));
            i++;
        }
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
}