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
    private INatsKVStore? _store = null;

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
        try
        {
            var state = await store.GetEntryAsync<T>(name);
            if (state.Value is { } value)
                grainState.State = value;
        } 
        catch (NATS.Client.KeyValueStore.NatsKVKeyNotFoundException ex)
        {
            // If the key is not found, we simply leave the state as default
            grainState.State = Activator.CreateInstance<T>();
        }
        catch (Exception ex)
        {
            throw new OrleansException($"Failed to read state '{name}' for grain '{grainId}'", ex);
        }
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