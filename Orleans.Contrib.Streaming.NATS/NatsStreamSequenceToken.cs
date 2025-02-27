using NATS.Client.JetStream;
using Orleans.Providers.Streams.Common;
using Orleans.Streams;

namespace Orleans.Contrib.Streaming.NATS;

[GenerateSerializer]
public class NatsStreamSequenceToken : StreamSequenceToken, IEquatable<NatsStreamSequenceToken>
{
    public bool Equals(NatsStreamSequenceToken? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return SequenceNumber == other.SequenceNumber && EventIndex == other.EventIndex;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((NatsStreamSequenceToken)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(SequenceNumber, EventIndex);
    }

    public static bool operator ==(NatsStreamSequenceToken? left, NatsStreamSequenceToken? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(NatsStreamSequenceToken? left, NatsStreamSequenceToken? right)
    {
        return !Equals(left, right);
    }

    public NatsStreamSequenceToken(NatsJSSequencePair valueSequence)
    {
        SequenceNumber = (long)valueSequence.Stream;
    }
    
    public override bool Equals(StreamSequenceToken? other)
    {
        if (other is null) return false;
        return SequenceNumber == other.SequenceNumber && EventIndex == other.EventIndex;
    }

    public override int CompareTo(StreamSequenceToken other)
    {
        return SequenceNumber.CompareTo(other.SequenceNumber);
    }

    [Id(0)] public override long SequenceNumber { get; protected set; }
    [Id(1)] public override int EventIndex { get; protected set; }
    
    public EventSequenceToken CreateSequenceTokenForEvent(int eventInd)
    {
        return new EventSequenceToken(SequenceNumber, eventInd);
    }
}