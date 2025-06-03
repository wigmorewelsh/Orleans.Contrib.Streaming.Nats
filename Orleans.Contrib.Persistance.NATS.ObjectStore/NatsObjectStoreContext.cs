using System.Threading;
using System.Threading.Tasks;
using NATS.Client.ObjectStore;

namespace Orleans.Contrib.Persistance.NATS.ObjectStore;

public interface INatsObjectStoreContext
{
    Task<INatsObjStore> CreateStoreAsync(string name, CancellationToken cancellationToken = default);
}

public class NatsObjectStoreContext : INatsObjectStoreContext
{
    private readonly INatsObjContext _client;

    public NatsObjectStoreContext(INatsObjContext client)
    {
        _client = client;
    }

    public async Task<INatsObjStore> CreateStoreAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _client.CreateObjectStoreAsync(name, cancellationToken);
    }
}

