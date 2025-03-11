using System.Text;
using Orleans.Streams;

namespace Orleans.Contrib.Streaming.NATS;

public class NatsSubjects
{
    public static string ToSubject(string natsStreamName, QueueId queueId, StreamId orleansStreamId)
    {
        var @namespace = Encoding.UTF8.GetString(orleansStreamId.Namespace.Span);
        var key = Encoding.UTF8.GetString(orleansStreamId.Key.Span);
        var subject = $"{natsStreamName}.{queueId}.{@namespace}.{key}";
        return subject;
    }

    public static StreamId FromSubject(string subject)
    {
        var rawStreamId = subject.Split('.');
        var ns = Encoding.UTF8.GetBytes(rawStreamId[2]);
        var key = Encoding.UTF8.GetBytes(rawStreamId[3]);
        var streamId = StreamId.Create(ns, key);
        return streamId;
    }
}