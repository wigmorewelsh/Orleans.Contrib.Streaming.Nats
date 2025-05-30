using Orleans;
using System.Threading.Tasks;

namespace Orleans.Contrib.Persistance.NatsKv.Tests.Grains;

[GenerateSerializer]
public class TestState
{
    [Id(0)]
    public string Value { get; set; }
}

public class TestStateGrain : Grain<TestState>, ITestStateGrain
{
    public Task<TestState> GetState() => Task.FromResult(State);

    public async Task SetState(string value)
    {
        State.Value = value;
        await WriteStateAsync();
    }

    public async Task DeleteState()
    {
        await ClearStateAsync();
    }
}