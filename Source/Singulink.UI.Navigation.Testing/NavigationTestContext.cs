using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;

namespace Singulink.UI.Navigation.Testing;

/// <summary>
/// Runs test bodies on a single-threaded synchronization context that mimics a UI thread, which <see cref="TestNavigator"/> and
/// <see cref="Tasks.TaskRunner"/> require. All continuations, posted callbacks and fire-and-forget work run on the calling thread in the order they are
/// queued, and any exception thrown by queued work fails the test.
/// </summary>
public static class NavigationTestContext
{
    /// <summary>
    /// Runs the specified test body inside a fresh single-threaded synchronization context and blocks until the body and all work it queued has completed.
    /// </summary>
    public static void Run(Func<Task> testBody)
    {
        Run(async () => {
            await testBody();
            return true;
        });
    }

    /// <summary>
    /// Runs the specified test body inside a fresh single-threaded synchronization context, blocks until the body and all work it queued has completed,
    /// and returns the body's result.
    /// </summary>
    public static TResult Run<TResult>(Func<Task<TResult>> testBody)
    {
        var previousContext = SynchronizationContext.Current;
        using var context = new PumpSynchronizationContext();

        SynchronizationContext.SetSynchronizationContext(context);

        try
        {
            var task = testBody();
            context.RunUntilIdle(task);
            return task.GetAwaiter().GetResult();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }
    }

    /// <summary>
    /// Queue-based synchronization context that executes all posted work on the thread that created it.
    /// </summary>
    private sealed class PumpSynchronizationContext : SynchronizationContext, IDisposable
    {
        private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = [];
        private readonly int _threadId = Environment.CurrentManagedThreadId;
        private int _pendingCount;

        public override void Post(SendOrPostCallback d, object? state)
        {
            Interlocked.Increment(ref _pendingCount);
            _queue.Add((d, state));
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            if (Environment.CurrentManagedThreadId == _threadId)
            {
                d(state);
                return;
            }

            using var completed = new ManualResetEventSlim();
            ExceptionDispatchInfo? exception = null;

            Post(_ => {
                try
                {
                    d(state);
                }
                catch (Exception ex)
                {
                    exception = ExceptionDispatchInfo.Capture(ex);
                }
                finally
                {
                    completed.Set();
                }
            }, null);

            completed.Wait();
            exception?.Throw();
        }

        public override SynchronizationContext CreateCopy() => this;

        /// <summary>
        /// Pumps queued work until the specified task has completed and no queued work remains.
        /// </summary>
        public void RunUntilIdle(Task task)
        {
            while (!task.IsCompleted || Volatile.Read(ref _pendingCount) > 0)
            {
                var (callback, state) = _queue.Take();

                try
                {
                    callback(state);
                }
                finally
                {
                    Interlocked.Decrement(ref _pendingCount);
                }
            }
        }

        public void Dispose() => _queue.Dispose();
    }
}
