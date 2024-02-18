using Microsoft.Extensions.Options;
using NATS.Client.KeyValueStore;
using Orleans.Runtime;
using Orleans.Storage;

namespace Orleans.Contrib.Persistance.NatsKv;

public class NatsGrainStorage : IGrainStorage
{
    private readonly INatsKVContext _context;
    private readonly IOptionsMonitor<NatsGrainStorageOptions> _options;
    private readonly string _name;
    private INatsKVStore _store;

    public NatsGrainStorage(INatsKVContext context, IOptionsMonitor<NatsGrainStorageOptions> options, string name)
    {
        _context = context;
        _options = options;
        _name = name;
    }

    private async Task<INatsKVStore> Store()
    {
        return _store ??= await _context.CreateStoreAsync(_name);
    }

    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var name = $"{grainId}.{stateName}";
        var store = await Store();
        var state = await store.GetEntryAsync<T>(name);
        grainState.State = state.Value;
    }

    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var name = $"{grainId}.{stateName}";
        var store = await Store();
        await store.PutAsync(name, grainState.State);
    }

    public async Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var name = $"{grainId}.{stateName}";
        var store = await Store();
        await store.DeleteAsync(name);
    }
}