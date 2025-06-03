using System.Threading.Tasks;
using Orleans;

namespace Orleans.Contrib.Persistance.NATS.ObjectStore.Tests.Grains;

public interface ITestStateGrain : IGrainWithStringKey
{
    Task<string> GetState();
    Task SetState(string value);
    Task DeleteState();
}

public class TestState
{
    public string Value { get; set; } = string.Empty;
}

public class TestStateGrain : Grain<TestState>, ITestStateGrain
{
    public Task<string> GetState() => Task.FromResult(State.Value);
    public Task SetState(string value)
    {
        State.Value = value;
        return WriteStateAsync();
    }

    public Task DeleteState()
    {
        return ClearStateAsync();
    }
}

