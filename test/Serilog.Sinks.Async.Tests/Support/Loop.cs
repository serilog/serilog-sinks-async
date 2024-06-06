using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Serilog.Sinks.Async.Tests.Support;

public static class Loop
{
    /// <summary>
    ///     Repeats the specified <see cref="action" />, until the specified <see cref="until" /> returns true,
    ///     With a delay between retries of <see cref="retryInterval" />, or until the <see cref="timeout " /> is exceeded.
    /// </summary>
    /// <typeparam name="TResult"> The type of the result of the <see cref="action" /> </typeparam>
    /// <param name="action"> The action to perform </param>
    /// <param name="until"> The predicate which determines whether the action has completed </param>
    /// <param name="retryInterval"> The delay between retries </param>
    /// <param name="timeout"> The time after which the retries are cancelled </param>
    /// <returns> </returns>
    public static RetryResult<TResult> Retry<TResult>(this Func<TResult> action, Predicate<TResult> until,
        TimeSpan retryInterval, TimeSpan timeout)
    {
        var retries = 0;
        TResult result;
        var stopwatch = Stopwatch.StartNew();
        do
        {
            retries++;
            result = action();

            if (until(result))
            {
                return new RetryResult<TResult>(result, retries, stopwatch.Elapsed);
            }

            Task.Delay((int)retryInterval.TotalMilliseconds).Wait();
        } while (stopwatch.Elapsed < timeout);

        return new RetryResult<TResult>(result, retries, stopwatch.Elapsed);
    }

    /// <summary>
    ///     Repeats the specified <see cref="action" />, until the specified <see cref="timeout" />,
    ///     With a delay between retries of <see cref="retryInterval" />
    /// </summary>
    /// <typeparam name="TResult"> The type of the result of the <see cref="action" /> </typeparam>
    /// <param name="action"> The action to perform </param>
    /// <param name="retryInterval"> The delay between retries </param>
    /// <param name="timeout"> The time after which the retries are cancelled </param>
    /// <returns> </returns>
    public static RetryResult<TResult> RetryUntilTimesout<TResult>(this Func<TResult> action,
        TimeSpan retryInterval, TimeSpan timeout)
    {
        var retries = 0;
        TResult result;
        var stopwatch = Stopwatch.StartNew();
        do
        {
            retries++;
            result = action();

            Task.Delay((int)retryInterval.TotalMilliseconds).Wait();
        } while (stopwatch.Elapsed < timeout);

        return new RetryResult<TResult>(result, retries, stopwatch.Elapsed);
    }

    /// <summary>
    ///     Repeats the specified <see cref="action" /> the specified number of times
    /// </summary>
    public static void For(Action action, int times)
    {
        if (times > 0)
        {
            for (var counter = 0; counter < times; counter++)
            {
                action();
            }
        }
    }

    /// <summary>
    ///     Repeats the specified <see cref="Action{Int32}" /> the specified number of times, from 0 to <see cref="to" />
    /// </summary>
    public static void For(Action<int> action, int to)
    {
        if (to > 0)
        {
            for (var counter = 0; counter < to; counter++)
            {
                action(counter);
            }
        }
    }

    /// <summary>
    ///     Repeats the specified <see cref="Action{Int32}" /> the specified number of times, from <see cref="from" /> to
    ///     <see cref="to" />
    /// </summary>
    public static void For(Action<int> action, int from, int to)
    {
        if (to > 0
            && from >= 0
            && from <= to)
        {
            var maxCount = (from > 0) ? (to + 1) : to;
            for (var counter = from; counter < maxCount; counter++)
            {
                action(counter);
            }
        }
    }
}

public class RetryResult<TResult>
{
    public RetryResult(TResult result, int retries, TimeSpan elapsed)
    {
        Result = result;
        Retries = retries;
        Elapsed = elapsed;
    }

    public int Retries { get; private set; }

    public TimeSpan Elapsed { get; private set; }

    public TResult Result { get; private set; }
}