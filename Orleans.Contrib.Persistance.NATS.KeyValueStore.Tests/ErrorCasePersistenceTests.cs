using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute.ExceptionExtensions;
using Xunit.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;
using NATS.Client.KeyValueStore;
using NSubstitute;
using Orleans.Contrib.Persistance.NATS.KeyValueStore.Tests.Fixtures;
using Orleans.Runtime;
using Shouldly;
using Xunit;


namespace Orleans.Contrib.Persistance.NATS.KeyValueStore.Tests;

public class ErrorCasePersistenceTests : IClassFixture<TestFixture<ErrorCasePersistenceTests.TestSettings>>
{
    public class TestSettings : ITestSettings
    {
        public static string StreamName => nameof(ErrorCasePersistenceTests);
    }

    private readonly TestFixture<TestSettings> _testFixture;
    private readonly ITestOutputHelper _output;
    private readonly INatsKVContext _context;
    private readonly IOptionsMonitor<NatsGrainStorageOptions> _options;
    private readonly ILogger<NatsGrainStorage> _logger;
    private readonly NatsGrainStorage _storage;
    private readonly IGrainState<string> _grainState;
    private INatsKVStore _store;

    public ErrorCasePersistenceTests(TestFixture<TestSettings> testFixture, ITestOutputHelper output)
    {
        _testFixture = testFixture;
        _output = output;
        _context = Substitute.For<INatsKVContext>();
        _options = Substitute.For<IOptionsMonitor<NatsGrainStorageOptions>>();
        _logger = Substitute.For<ILogger<NatsGrainStorage>>();
        _storage = new NatsGrainStorage(_context, _options, "test", _logger);
        _grainState = Substitute.For<IGrainState<string>>();
        _store = Substitute.For<INatsKVStore>();
        _storage.Init(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
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
        _store.PutAsync(Arg.Any<string>(), Arg.Any<object>()).Throws(new Exception("Write failed"));
        _context.CreateStoreAsync(Arg.Any<string>()).Returns(ValueTask.FromResult(_store));
        await Should.ThrowAsync<OrleansException>(async () =>
            await _storage.WriteStateAsync("state", GrainId.Create("test", "1"), _grainState));
    }

    [Fact]
    public async Task WhenReadFails_ShouldThrowOrleansException()
    {
        _store.GetEntryAsync<string>(Arg.Any<string>()).Throws(new Exception("Read failed"));
        _context.CreateStoreAsync(Arg.Any<string>()).Returns(ValueTask.FromResult(_store));
        await Should.ThrowAsync<OrleansException>(async () =>
            await _storage.ReadStateAsync("state", GrainId.Create("test", "1"), _grainState));
    }

    [Fact]
    public async Task WhenDeleteFails_ShouldThrowOrleansException()
    {
        _store.DeleteAsync(Arg.Any<string>()).Throws(new Exception("Delete failed"));
        _context.CreateStoreAsync(Arg.Any<string>()).Returns(ValueTask.FromResult(_store));
        await Should.ThrowAsync<OrleansException>(async () =>
            await _storage.ClearStateAsync("state", GrainId.Create("test", "1"), _grainState));
    }

    [Fact(Skip = "This test is for documentation purposes only.")]
    public async Task WhenOperationExceedsTimeout_ShouldThrowTaskCanceledException()
    {
        _store.GetEntryAsync<string>(Arg.Any<string>()).Returns(_ =>
        {
            Task.Delay(2000).Wait();
            return new NatsKVEntry<string>("", "");
        });
        _context.CreateStoreAsync(Arg.Any<string>()).Returns(ValueTask.FromResult(_store));
        var cts = new System.Threading.CancellationTokenSource(100); // 100ms timeout
        await Should.ThrowAsync<TaskCanceledException>(async () =>
            await _storage.ReadStateAsync("state", GrainId.Create("test", "1"), _grainState).WaitAsync(cts.Token));
    }
}
