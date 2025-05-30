using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute.ExceptionExtensions;
using Xunit.Abstractions;
using System;
using System.Threading.Tasks;
using NSubstitute;
using Orleans.Contrib.Persistance.NatsKv.Tests.Fixtures;
using Orleans.Runtime;
using Shouldly;
using Xunit;


namespace Orleans.Contrib.Persistance.NatsKv.Tests;

public class ErrorCasePersistenceTests : IClassFixture<TestFixture<ErrorCasePersistenceTests.TestSettings>>
{
    public class TestSettings : ITestSettings
    {
        public static string StreamName => nameof(ErrorCasePersistenceTests);
    }

    private readonly TestFixture<TestSettings> _testFixture;
    private readonly ITestOutputHelper _output;
    private readonly NATS.Client.KeyValueStore.INatsKVContext _context;
    private readonly IOptionsMonitor<NatsGrainStorageOptions> _options;
    private readonly ILogger<NatsGrainStorage> _logger;
    private readonly NatsGrainStorage _storage;
    private readonly IGrainState<string> _grainState;

    public ErrorCasePersistenceTests(TestFixture<TestSettings> testFixture, ITestOutputHelper output)
    {
        _testFixture = testFixture;
        _output = output;
        _context = Substitute.For<NATS.Client.KeyValueStore.INatsKVContext>();
        _options = Substitute.For<IOptionsMonitor<NatsGrainStorageOptions>>();
        _logger = Substitute.For<ILogger<NatsGrainStorage>>();
        _storage = new NatsGrainStorage(_context, _options, "test", _logger);
        _grainState = Substitute.For<IGrainState<string>>();
    }

    [Fact]
    public async Task WhenNatsConnectionFails_ShouldThrowOrleansException()
    {
        _context.CreateStoreAsync(Arg.Any<string>()).Throws(new Exception("NATS connection failed"));
        await Should.ThrowAsync<OrleansException>(async () =>
            await _storage.ReadStateAsync("state", GrainId.Create("test", "1"), _grainState));
    }

    [Fact]
    public async Task WhenWriteFails_ShouldThrowOrleansException()
    {
        var store = Substitute.For<NATS.Client.KeyValueStore.INatsKVStore>();
        store.PutAsync(Arg.Any<string>(), Arg.Any<object>()).Throws(new Exception("Write failed"));
        _context.CreateStoreAsync(Arg.Any<string>()).Returns(new ValueTask<NATS.Client.KeyValueStore.INatsKVStore>(store));
        await Should.ThrowAsync<OrleansException>(async () =>
            await _storage.WriteStateAsync("state", GrainId.Create("test", "1"), _grainState));
    }

    [Fact]
    public async Task WhenReadFails_ShouldThrowOrleansException()
    {
        var store = Substitute.For<NATS.Client.KeyValueStore.INatsKVStore>();
        store.GetEntryAsync<string>(Arg.Any<string>()).Throws(new Exception("Read failed"));
        _context.CreateStoreAsync(Arg.Any<string>()).Returns(new ValueTask<NATS.Client.KeyValueStore.INatsKVStore>(store));
        await Should.ThrowAsync<OrleansException>(async () =>
            await _storage.ReadStateAsync("state", GrainId.Create("test", "1"), _grainState));
    }

    [Fact]
    public async Task WhenDeleteFails_ShouldThrowOrleansException()
    {
        var store = Substitute.For<NATS.Client.KeyValueStore.INatsKVStore>();
        store.DeleteAsync(Arg.Any<string>()).Throws(new Exception("Delete failed"));
        _context.CreateStoreAsync(Arg.Any<string>()).Returns(new ValueTask<NATS.Client.KeyValueStore.INatsKVStore>(store));
        await Should.ThrowAsync<OrleansException>(async () =>
            await _storage.ClearStateAsync("state", GrainId.Create("test", "1"), _grainState));
    }

    [Fact]
    public async Task WhenOperationExceedsTimeout_ShouldThrowTaskCanceledException()
    {
        var store = Substitute.For<NATS.Client.KeyValueStore.INatsKVStore>();
        store.GetEntryAsync<string>(Arg.Any<string>()).Returns(_ =>
        {
            Task.Delay(2000).Wait(); 
            return new NATS.Client.KeyValueStore.NatsKVEntry<string>("", "");
        });
        _context.CreateStoreAsync(Arg.Any<string>()).Returns(new ValueTask<NATS.Client.KeyValueStore.INatsKVStore>(store));
        var cts = new System.Threading.CancellationTokenSource(100); // 100ms timeout
        await Should.ThrowAsync<TaskCanceledException>(async () =>
            await _storage.ReadStateAsync("state", GrainId.Create("test", "1"), _grainState).WaitAsync(cts.Token));
    }
}
