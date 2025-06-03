using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.KeyValueStore;
using Orleans.Runtime;
using Orleans.Storage;

namespace Orleans.Contrib.Persistance.NATS.KeyValueStore;

public class NatsGrainStorage : IGrainStorage, ILifecycleParticipant<ISiloLifecycle>
{
    private readonly INatsKVContext _context;
    private readonly IOptionsMonitor<NatsGrainStorageOptions> _options;
    private readonly string _name;
    private readonly ILogger<NatsGrainStorage> _logger;
    private INatsKVStore? _store = null;

    public NatsGrainStorage(INatsKVContext context, IOptionsMonitor<NatsGrainStorageOptions> options, string name, ILogger<NatsGrainStorage> logger)
    {
        _context = context;
        _options = options;
        _name = name;
        _logger = logger;
    }

    internal async Task Init(CancellationToken cancellationToken)
    {
        try
        {
            _store = await _context.CreateStoreAsync(_name, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NatsGrainStorage] Failed to initialize store for name '{StoreName}'", _name);
            throw new OrleansException($"Failed to initialize store for name '{_name}'");
        }
    }

    public void Participate(ISiloLifecycle lifecycle)
    {
        lifecycle.Subscribe(
            OptionFormattingUtilities.Name<NatsGrainStorage>(_name),
            ServiceLifecycleStage.AfterRuntimeGrainServices,
            Init);
    }

    private Task<INatsKVStore> Store()
    {
        if (_store == null)
            throw new OrleansException($"NATS KV Store for '{_name}' is not initialized. Did you forget to call Participate/Init?");
        return Task.FromResult(_store);
    }

    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var name = $"{grainId}.{stateName}";
        try
        {
            var store = await Store();
            try
            {
                var state = await store.GetEntryAsync<T>(name);
                if (state.Value is { } value)
                    grainState.State = value;
            }
            catch (NatsKVKeyNotFoundException)
            {
                grainState.State = Activator.CreateInstance<T>();
            }
        }
        catch (Exception ex)
        {
            // Log the exception
            _logger.LogError(ex, "[NatsGrainStorage] Failed to read state '{StateName}' for grain '{GrainId}'", name, grainId);
            throw new OrleansException($"Failed to read state '{name}' for grain '{grainId}'");
        }
    }

    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var name = $"{grainId}.{stateName}";
        try
        {
            var store = await Store();
            await store.PutAsync(name, grainState.State);
        }
        catch (NatsPayloadTooLargeException ex)
        {
            // Log the exception
            _logger.LogError(ex, "[NatsGrainStorage] Payload size exceeds NATS KV limit");
            throw new OrleansException($"Payload size exceeds NATS KV limit: {ex.Message}");
        }
        catch (Exception ex)
        {
            // Log the exception
            _logger.LogError(ex, "[NatsGrainStorage] Failed to write state '{StateName}' for grain '{GrainId}'", name, grainId);
            throw new OrleansException($"Failed to write state '{name}' for grain '{grainId}'");
        }
    }

    public async Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var name = $"{grainId}.{stateName}";
        try
        {
            var store = await Store();
            await store.DeleteAsync(name);
        }
        catch (Exception ex)
        {
            // Log the exception
            _logger.LogError(ex, "[NatsGrainStorage] Failed to delete state '{StateName}' for grain '{GrainId}'", name, grainId);
            throw new OrleansException($"Failed to delete state '{name}' for grain '{grainId}'");
        }
    }
}
