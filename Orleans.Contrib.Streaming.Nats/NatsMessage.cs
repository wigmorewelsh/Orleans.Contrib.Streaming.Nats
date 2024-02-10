namespace Orleans.Contrib.Streaming.Nats;

[GenerateSerializer]
public class NatsMessage
{
    [Id(0)]
    public Type Type { get; set; }
    [Id(1)]
    public byte[] Data { get; set; }
}