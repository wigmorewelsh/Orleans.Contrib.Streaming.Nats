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
    private readonly NatsConfigator _natsConfigator;
    private readonly HashRingStreamQueueMapperOptions _queueMapperOptions;
    private readonly INatsMessageBodySerializer _serializer;
    private readonly ILogger<NatsQueueAdapterFactory> _logger;
    private HashRingBasedStreamQueueMapper _mapper;

    public NatsQueueAdapterFactory(
        string name,
        HashRingStreamQueueMapperOptions queueMapperOptions,
        NatsConfigator natsConfigator,
        INatsMessageBodySerializer serializer,
        ILogger<NatsQueueAdapterFactory> logger)
    {
        Name = name;
        _queueMapperOptions = queueMapperOptions;
        _natsConfigator = natsConfigator;
        _serializer = serializer;
        _logger = logger;
        _mapper = new HashRingBasedStreamQueueMapper(this._queueMapperOptions, name);
    }

    public string Name { get; set; }

    public async Task<IQueueAdapter> CreateAdapter()
    {
        try
        {
            var natsOptions = NatsOpts.Default;
            natsOptions = _natsConfigator.Configure(natsOptions);
            var connection = new NatsConnection(natsOptions);
            await connection.ConnectAsync();
            var context = new NatsJSContext(connection);
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

    public IQueueCache CreateQueueCache(QueueId queueId)
    {
        return new SimpleQueueCache(1024 * 4, NullLogger.Instance);
    }
    
    public static NatsQueueAdapterFactory Create(IServiceProvider services, string name)
    {
        var queueMapperOptions = services.GetOptionsByName<HashRingStreamQueueMapperOptions>(name);
        var natsOpts = services.GetOptionsByName<NatsConfigator>(name);
        var serializer = services.GetKeyedService<INatsMessageBodySerializer>(name);
        var factory = ActivatorUtilities.CreateInstance<NatsQueueAdapterFactory>(services, name, queueMapperOptions, natsOpts, serializer);
        return factory;
    }
}