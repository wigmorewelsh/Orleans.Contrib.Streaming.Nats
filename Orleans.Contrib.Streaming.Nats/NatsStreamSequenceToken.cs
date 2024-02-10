using NATS.Client.JetStream;
using Orleans.Streams;

namespace Orleans.Contrib.Streaming.Nats;

[GenerateSerializer]
public class NatsStreamSequenceToken : StreamSequenceToken
{
    public NatsStreamSequenceToken(NatsJSSequencePair valueSequence)
    {
        SequenceNumber = (long)valueSequence.Stream;
    }

    public override bool Equals(StreamSequenceToken other)
    {
        return SequenceNumber == other.SequenceNumber;
    }

    public override int CompareTo(StreamSequenceToken other)
    {
        return SequenceNumber.CompareTo(other.SequenceNumber);
    }

    [Id(0)] public override long SequenceNumber { get; protected set; }
    [Id(1)] public override int EventIndex { get; protected set; }
}