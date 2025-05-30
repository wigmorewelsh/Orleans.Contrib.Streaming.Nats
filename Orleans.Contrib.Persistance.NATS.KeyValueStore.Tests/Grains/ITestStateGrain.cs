using Orleans;
using System.Threading.Tasks;

namespace Orleans.Contrib.Persistance.NATS.KeyValueStore.Tests.Grains;

public interface ITestStateGrain : IGrainWithStringKey
{
    Task<TestState> GetState();
    Task SetState(string value);
    Task DeleteState();
}