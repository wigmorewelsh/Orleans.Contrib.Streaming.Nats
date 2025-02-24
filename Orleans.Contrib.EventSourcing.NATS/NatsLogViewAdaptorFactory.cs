using Orleans.Configuration;
using Orleans.EventSourcing;
using Orleans.Storage;

namespace Orleans.Contrib.EventSourcing.NATS;

public class NatsLogViewAdaptorFactory : ILogViewAdaptorFactory
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
            services);
    }

    public bool UsesStorageProvider => false;
}