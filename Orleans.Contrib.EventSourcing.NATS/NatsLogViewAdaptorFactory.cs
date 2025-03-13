using Orleans.Configuration;
using Orleans.EventSourcing;
using Orleans.Serialization;
using Orleans.Storage;

namespace Orleans.Contrib.EventSourcing.NATS;

public class NatsLogViewAdaptorFactory(Serializer serializer) : ILogViewAdaptorFactory
{
    public ILogViewAdaptor<TLogView, TLogEntry> MakeLogViewAdaptor<TLogView, TLogEntry>(
        ILogViewAdaptorHost<TLogView, TLogEntry> hostGrain, 
        TLogView initialState,
        string grainTypeName, 
        IGrainStorage grainStorage, 
        ILogConsistencyProtocolServices services)
        where TLogView : class, new() where TLogEntry : class
    {
        return new NatsLogViewAdaptor<TLogView, TLogEntry>(hostGrain, initialState, grainStorage, grainTypeName,
            services, serializer);
    }

    public bool UsesStorageProvider => false;
}