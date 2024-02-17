using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    private readonly NatsConfigator _natsConfigator;
    private readonly HashRingStreamQueueMapperOptions _queueMapperOptions;
    private readonly Serializer _serialize;
    private readonly ILogger<NatsQueueAdapterFactory> _logger;
    private HashRingBasedStreamQueueMapper _hashRingBasedStreamQueueMapper;

    public NatsQueueAdapterFactory(
        string name,
        HashRingStreamQueueMapperOptions queueMapperOptions,
        NatsConfigator natsConfigator,
        Serializer serialize,
        ILogger<NatsQueueAdapterFactory> logger)
    {
        Name = name;
        _queueMapperOptions = queueMapperOptions;
        _natsConfigator = natsConfigator;
        _serialize = serialize;
        _logger = logger;
        _hashRingBasedStreamQueueMapper = new HashRingBasedStreamQueueMapper(this._queueMapperOptions, name);
    }

    public string Name { get; set; }

    public async Task<IQueueAdapter> CreateAdapter()
    {
        try
        {
            var natsOptions = NatsOpts.Default;
            natsOptions = _natsConfigator.Configure(natsOptions);
            natsOptions = natsOptions with
            {
                SerializerRegistry = new NatsOrleansSerilizerRegistry(_serialize)
            };
            var connection = new NatsConnection(natsOptions);
            await connection.ConnectAsync();
            var context = new NatsJSContext(connection);
            return new NatsAdaptor(context, Name, _serialize, _hashRingBasedStreamQueueMapper, _logger);
        } 
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create adapter {Exception}", ex);
            throw;
        }
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
        var natsOpts = services.GetOptionsByName<NatsConfigator>(name);
        var factory = ActivatorUtilities.CreateInstance<NatsQueueAdapterFactory>(services, name, queueMapperOptions, natsOpts);
        return factory;
    }
}