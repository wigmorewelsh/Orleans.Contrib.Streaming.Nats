using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.EventSourcing;
using Orleans.Serialization;
using Orleans.Storage;

namespace Orleans.Contrib.EventSourcing.NATS;

public class NatsLogViewAdaptorFactory(Serializer serializer, string name) : ILogViewAdaptorFactory
{
    public ILogViewAdaptor<TLogView, TLogEntry> MakeLogViewAdaptor<TLogView, TLogEntry>(
        ILogViewAdaptorHost<TLogView, TLogEntry> hostGrain, 
        TLogView initialState,
        string grainTypeName, 
        IGrainStorage grainStorage, 
        ILogConsistencyProtocolServices services)
        where TLogView : class, new() where TLogEntry : class
    {
        return new NatsLogViewAdaptor<TLogView, TLogEntry>(hostGrain, initialState,
            services, serializer, name);
    }

    public bool UsesStorageProvider => false;

    public static NatsLogViewAdaptorFactory Create(IServiceProvider serviceProvider, object? name)
    {
        Debug.Assert(name != null, nameof(name) + " != null");
        return ActivatorUtilities.CreateInstance<NatsLogViewAdaptorFactory>(serviceProvider, name);
    }
}