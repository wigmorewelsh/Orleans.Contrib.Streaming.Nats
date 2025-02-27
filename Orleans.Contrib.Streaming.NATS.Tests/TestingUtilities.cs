using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Logging;
using Orleans.TestingHost.Logging;

namespace Orleans.Contrib.Streaming.NATS.Tests;

/// <summary> Collection of test utilities </summary>
public static class TestingUtils
{
    /// <summary> Run the predicate until it succeed or times out </summary>
    /// <param name="predicate">The predicate to run</param>
    /// <param name="timeout">The timeout value</param>
    /// <param name="delayOnFail">The time to delay next call upon failure</param>
    /// <returns>True if the predicate succeed, false otherwise</returns>
    public static async Task WaitUntilAsync(Func<bool, Task<bool>> predicate, TimeSpan timeout,
        TimeSpan? delayOnFail = null)
    {
        delayOnFail = delayOnFail ?? TimeSpan.FromSeconds(1);
        var keepGoing = new[] { true };

        async Task loop()
        {
            bool passed;
            do
            {
                // need to wait a bit to before re-checking the condition.
                await Task.Delay(delayOnFail.Value);
                passed = await predicate(false);
            } while (!passed && keepGoing[0]);

            if (!passed)
                await predicate(true);
        }

        var task = loop();
        try
        {
            await Task.WhenAny(task, Task.Delay(timeout));
        }
        finally
        {
            keepGoing[0] = false;
        }

        await task;
    }
}