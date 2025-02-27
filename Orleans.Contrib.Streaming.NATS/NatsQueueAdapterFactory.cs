using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using NATS.Client.JetStream;
using Orleans.Configuration;
using Orleans.Providers;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Streams;

namespace Orleans.Contrib.Streaming.NATS;

public class NatsQueueAdapterFactory : IQueueAdapterFactory, IQueueAdapterCache
{
    private readonly HashRingStreamQueueMapperOptions _queueMapperOptions;
    private readonly INatsMessageBodySerializer _serializer;
    private readonly ILogger<NatsQueueAdapterFactory> _logger;
    private readonly INatsConnection _connection;
    private HashRingBasedStreamQueueMapper _mapper;

    public NatsQueueAdapterFactory(
        string name,
        HashRingStreamQueueMapperOptions queueMapperOptions,
        INatsMessageBodySerializer serializer,
        ILogger<NatsQueueAdapterFactory> logger, 
        INatsConnection connection)
    {
        Name = name;
        _queueMapperOptions = queueMapperOptions;
        _serializer = serializer;
        _logger = logger;
        _connection = connection;
        _mapper = new HashRingBasedStreamQueueMapper(this._queueMapperOptions, name);
    }

    public string Name { get; set; }

    public async Task<IQueueAdapter> CreateAdapter()
    {
        try
        {
            await _connection.ConnectAsync();
            var context = new NatsJSContext(_connection);
            return new NatsAdaptor(context, Name, _serializer, _mapper, _logger);
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
        return _mapper;
    }

    public async Task<IStreamFailureHandler> GetDeliveryFailureHandler(QueueId queueId)
    {
        return new NoOpStreamDeliveryFailureHandler();
    }

    private ConcurrentDictionary<QueueId, SimpleQueueCache> _cache = new();
    public IQueueCache CreateQueueCache(QueueId queueId)
    {
        return _cache.GetOrAdd(queueId, new SimpleQueueCache(1024 * 4, NullLogger.Instance));
    }
    
    public static NatsQueueAdapterFactory Create(IServiceProvider services, string name)
    {
        var queueMapperOptions = services.GetOptionsByName<HashRingStreamQueueMapperOptions>(name);
        var serializer = services.GetRequiredKeyedService<INatsMessageBodySerializer>(name);
        var natsConnection = services.GetRequiredKeyedService<INatsConnection>(name);
        var factory = ActivatorUtilities.CreateInstance<NatsQueueAdapterFactory>(services, name, queueMapperOptions, serializer, natsConnection);
        return factory;
    }
}