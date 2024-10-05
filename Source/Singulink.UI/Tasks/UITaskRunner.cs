using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace Singulink.UI.Tasks;

/// <summary>
/// Represents a task runner that executes tasks while managing busy state and ensuring that exceptions propagate to the UI thread.
/// </summary>
public class UITaskRunner : ITaskRunner
{
    private readonly SynchronizationContext _syncContext;

    private int _busyCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="UITaskRunner"/> class using the current synchronization context.
    /// </summary>
    /// <exception cref="InvalidOperationException">A current synchronization context was not found.</exception>
    public UITaskRunner()
    {
        _syncContext = SynchronizationContext.Current ?? throw new InvalidOperationException("Current synchronization context not found.");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UITaskRunner"/> class using the specified synchronization context.
    /// </summary>
    public UITaskRunner(SynchronizationContext syncContext)
    {
        _syncContext = syncContext;
    }

    /// <inheritdoc cref="ITaskRunner.IsBusyChanged"/>
    public event Action<bool>? IsBusyChanged;

    /// <inheritdoc cref="ITaskRunner.IsBusy"/>
    public bool IsBusy => _busyCount > 0;

    /// <inheritdoc cref="ITaskRunner.Run(bool, Func{Task})"/>
    public Task Run(bool setBusyWhileRunning, Func<Task> taskFunc)
    {
        var task = taskFunc();
        Run(setBusyWhileRunning, task);
        return task;
    }

    /// <inheritdoc cref="ITaskRunner.Run{T}(bool, Func{Task{T}})"/>
    public Task<T> Run<T>(bool setBusyWhileRunning, Func<Task<T>> taskFunc)
    {
        var task = taskFunc();
        Run(setBusyWhileRunning, task);
        return task;
    }

    /// <inheritdoc cref="ITaskRunner.Run(bool, Task)"/>
    public void Run(bool setBusyWhileRunning, Task task)
    {
        if (task.IsCompleted)
        {
            if (!task.IsCompletedSuccessfully)
                _syncContext.Post(static async t => await (Task)t!, task);

            return;
        }

        _syncContext.Post(StartTaskCallback, (this, setBusyWhileRunning, task));

        static async void StartTaskCallback(object? state)
        {
            var (@this, setBusyWhileRunning, task) = ((UITaskRunner, bool, Task))state!;

            if (setBusyWhileRunning && Interlocked.Increment(ref @this._busyCount) is 0)
            {
                try
                {
                    @this.IsBusyChanged?.Invoke(true);
                }
                catch
                {
                    --@this._busyCount;
                    throw;
                }
            }

            try
            {
                await task;
            }
            catch (Exception ex)
            {
                var exInfo = ExceptionDispatchInfo.Capture(ex);
                @this._syncContext.Post(static exInfoObj => ((ExceptionDispatchInfo)exInfoObj)!.Throw(), exInfo);
            }
            finally
            {
                if (setBusyWhileRunning)
                {
                    @this._syncContext.Post(EndTaskCallback, @this);
                }
            }
        }

        static void EndTaskCallback(object? state)
        {
            var @this = (UITaskRunner)state!;

            if (@this._busyCount is 0)
                throw new UnreachableException("Unbalanced busy count decrement.");

            if (--@this._busyCount is 0)
                @this.IsBusyChanged?.Invoke(false);
        }
    }
}
