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

public class NatsQueueAdapterFactory : IQueueAdapterFactory 
{
    private readonly SimpleQueueAdapterCache _adapterCache;
    private readonly NatsStreamOptions _streamOptions;
    private readonly HashRingStreamQueueMapperOptions _queueMapperOptions;
    private readonly INatsMessageBodySerializer _serializer;
    private readonly ILogger<NatsQueueAdapterFactory> _logger;
    private readonly INatsConnection _connection;
    private HashRingBasedStreamQueueMapper _mapper;

    public NatsQueueAdapterFactory(
        string name,
        NatsStreamOptions streamOptions,
        NatsConsumerOptions natsConsumerOptions,
        HashRingStreamQueueMapperOptions queueMapperOptions,
        SimpleQueueCacheOptions cacheOptions,
        INatsMessageBodySerializer serializer,
        ILogger<NatsQueueAdapterFactory> logger, 
        INatsConnection connection, 
        ILoggerFactory loggerFactory)
    {
        Name = name;
        _streamOptions = streamOptions;
        _queueMapperOptions = queueMapperOptions;
        _serializer = serializer;
        _logger = logger;
        _connection = connection;
        _mapper = new HashRingBasedStreamQueueMapper(this._queueMapperOptions, name);
        _adapterCache = new SimpleQueueAdapterCache(cacheOptions, name, loggerFactory);
    }

    public string Name { get; set; }

    public async Task<IQueueAdapter> CreateAdapter()
    {
        try
        {
            await _connection.ConnectAsync();
            var context = new NatsJSContext(_connection);
            return new NatsAdaptor(context, Name, _streamOptions, _serializer, _mapper, _logger);
        } 
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create adapter {Exception}", ex);
            throw;
        }
    }

    public IQueueAdapterCache GetQueueAdapterCache()
    {
        return _adapterCache;
    }

    public IStreamQueueMapper GetStreamQueueMapper()
    {
        return _mapper;
    }

    public async Task<IStreamFailureHandler> GetDeliveryFailureHandler(QueueId queueId)
    {
        return new NoOpStreamDeliveryFailureHandler();
    }

    
    public static NatsQueueAdapterFactory Create(IServiceProvider services, string name)
    {
        var streamOptions = services.GetOptionsByName<NatsStreamOptions>(name);
        var consumerOptions = services.GetOptionsByName<NatsConsumerOptions>(name);
        var queueMapperOptions = services.GetOptionsByName<HashRingStreamQueueMapperOptions>(name);
        var simpleQueueCacheOptions = services.GetOptionsByName<SimpleQueueCacheOptions>(name);
        var serializer = services.GetRequiredKeyedService<INatsMessageBodySerializer>(name);
        var natsConnection = services.GetRequiredKeyedService<INatsConnection>(name);
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        var factory = ActivatorUtilities.CreateInstance<NatsQueueAdapterFactory>(services, name, streamOptions, consumerOptions, queueMapperOptions, simpleQueueCacheOptions, serializer, natsConnection, loggerFactory);
        return factory;
    }
}