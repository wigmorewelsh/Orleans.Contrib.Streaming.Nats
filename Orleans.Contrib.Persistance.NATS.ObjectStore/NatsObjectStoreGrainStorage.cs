using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using NATS.Client.ObjectStore;

using Orleans.Contrib.Persistance.NATS.ObjectStore.Configuration;
using Orleans.Runtime;
using Orleans.Storage;

namespace Orleans.Contrib.Persistance.NATS.ObjectStore;

public class NatsObjectStoreGrainStorage : IGrainStorage, ILifecycleParticipant<ISiloLifecycle>
{
    private readonly INatsObjectStoreContext _context;
    private readonly IOptionsMonitor<NatsObjectStoreGrainStorageOptions> _options;
    private readonly string _name;
    private readonly ILogger<NatsObjectStoreGrainStorage> _logger;
    private INatsObjStore? _store = null;

    public NatsObjectStoreGrainStorage(INatsObjectStoreContext context, IOptionsMonitor<NatsObjectStoreGrainStorageOptions> options, string name, ILogger<NatsObjectStoreGrainStorage> logger)
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
            _logger.LogError(ex, "[NatsObjectStoreGrainStorage] Failed to initialize object store for name '{StoreName}'", _name);
            throw new OrleansException($"Failed to initialize object store for name '{_name}'");
        }
    }

    public void Participate(ISiloLifecycle lifecycle)
    {
        lifecycle.Subscribe(
            OptionFormattingUtilities.Name<NatsObjectStoreGrainStorage>(_name),
            ServiceLifecycleStage.AfterRuntimeGrainServices,
            Init);
    }

    private Task<INatsObjStore> Store()
    {
        if (_store == null)
            throw new OrleansException($"NATS Object Store for '{_name}' is not initialized. Did you forget to call Participate/Init?");
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
                using var stream = new MemoryStream();
                var obj = await store.GetAsync(name, stream);
                if (obj != null)
                {
                    stream.Position = 0;
                    grainState.State = System.Text.Json.JsonSerializer.Deserialize<T>(stream) ?? Activator.CreateInstance<T>();
                }
                else
                {
                    grainState.State = Activator.CreateInstance<T>();
                }
            }
            catch (NatsObjNotFoundException)
            {
                grainState.State = Activator.CreateInstance<T>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NatsObjectStoreGrainStorage] Failed to read state '{StateName}' for grain '{GrainId}'", name, grainId);
            throw new OrleansException($"Failed to read state '{name}' for grain '{grainId}'");
        }
    }

    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var name = $"{grainId}.{stateName}";
        try
        {
            var store = await Store();
            using var stream = new MemoryStream(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(grainState.State));
            await store.PutAsync(name, stream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NatsObjectStoreGrainStorage] Failed to write state '{StateName}' for grain '{GrainId}'", name, grainId);
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
            _logger.LogError(ex, "[NatsObjectStoreGrainStorage] Failed to delete state '{StateName}' for grain '{GrainId}'", name, grainId);
            throw new OrleansException($"Failed to delete state '{name}' for grain '{grainId}'");
        }
    }
}

