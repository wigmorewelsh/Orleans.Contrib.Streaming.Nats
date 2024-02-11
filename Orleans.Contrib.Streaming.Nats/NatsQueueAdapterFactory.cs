using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using NATS.Client.JetStream;
using Orleans.Configuration;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Streams;

namespace Orleans.Contrib.Streaming.Nats;

public class NatsQueueAdapterFactory : IQueueAdapterFactory, IQueueAdapterCache
{
    private readonly NatsOpts natsOptions;
    private readonly HashRingStreamQueueMapperOptions queueMapperOptions;
    private readonly Serializer serialize;
    private HashRingBasedStreamQueueMapper _hashRingBasedStreamQueueMapper;

    public NatsQueueAdapterFactory(
        string name,
        HashRingStreamQueueMapperOptions queueMapperOptions,
        NatsOpts natsOptions,
        Serializer serialize)
    {
        Name = name;
        this.queueMapperOptions = queueMapperOptions;
        this.natsOptions = natsOptions;
        this.serialize = serialize;
        _hashRingBasedStreamQueueMapper = new HashRingBasedStreamQueueMapper(this.queueMapperOptions, name);
    }

    public string Name { get; set; }

    public async Task<IQueueAdapter> CreateAdapter()
    {
        var natsOptions = new NatsOpts()
        {
            SerializerRegistry = new NatsOrleansSerilizerRegistry(serialize)
        };
        var connection = new NatsConnection(natsOptions);
        await connection.ConnectAsync();
        var context = new NatsJSContext(connection);
        return new NatsAdaptor(context, Name, serialize, _hashRingBasedStreamQueueMapper);
    }

    public IQueueAdapterCache GetQueueAdapterCache()
    {
        return this;
    }

    public IStreamQueueMapper GetStreamQueueMapper()
    {
        return _hashRingBasedStreamQueueMapper;
    }

    public async Task<IStreamFailureHandler> GetDeliveryFailureHandler(QueueId queueId)
    {
        return new NoOpStreamDeliveryFailureHandler();
    }

    public IQueueCache CreateQueueCache(QueueId queueId)
    {
        return new SimpleQueueCache(1024 * 4, NullLogger.Instance);
    }
    
    public static NatsQueueAdapterFactory Create(IServiceProvider services, string name)
    {
        var queueMapperOptions = services.GetOptionsByName<HashRingStreamQueueMapperOptions>(name);
        var natsOpts = services.GetOptionsByName<NatsOpts>(name);
        var factory = ActivatorUtilities.CreateInstance<NatsQueueAdapterFactory>(services, name, queueMapperOptions, natsOpts);
        return factory;
    }
}