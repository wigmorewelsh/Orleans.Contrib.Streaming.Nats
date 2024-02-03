namespace Orleans.Contrib.Streaming.Nats;

public class NatsMessage
{
    public Type Type { get; set; }
    public byte[] Data { get; set; }
}