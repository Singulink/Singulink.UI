using System.Diagnostics;

namespace Singulink.UI.Tasks;

/// <summary>
/// Represents a task runner that executes tasks while managing busy state and ensuring that unsuccessful tasks can be tracked for testing purposes.
/// </summary>
public class TestTaskRunner : ITaskRunner
{
    private readonly HashSet<Task> _tasks = [];

    private int _busyCount;

    /// <inheritdoc cref="ITaskRunner.IsBusyChanged"/>
    public event Action<bool>? IsBusyChanged;

    /// <inheritdoc cref="ITaskRunner.IsBusy"/>
    public bool IsBusy => _busyCount > 0;

    /// <summary>
    /// Waits for all running tasks to complete and throws an exception if any of the tasks did not complete successfully.
    /// </summary>
    public async Task WaitForAll()
    {
        Task[] tasks;

        lock (_tasks)
        {
            tasks = [.._tasks];
            _tasks.Clear();
        }

        await Task.WhenAll(tasks);

        List<Exception> exceptions = null;

        foreach (var task in tasks)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                (exceptions ??= []).Add(ex);
            }
        }

        if (exceptions is not null)
        {
            if (exceptions.Count is 1)
                throw exceptions[0];

            throw new AggregateException(exceptions);
        }
    }

    /// <inheritdoc cref="ITaskRunner.Run(bool, Task)"/>
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
    public async void Run(bool setBusyWhileRunning, Task task)
    {
        if (task.IsCompleted)
        {
            await task; // throw if faulted or cancelled
            return;
        }

        if (setBusyWhileRunning && Interlocked.Increment(ref _busyCount) is 1)
            IsBusyChanged?.Invoke(true);

        lock (_tasks)
            _tasks.Add(task);

        try
        {
            await task;

            lock (_tasks)
                _tasks.Remove(task);
        }
        finally
        {
            if (setBusyWhileRunning)
            {
                int newBusyCount = Interlocked.Decrement(ref _busyCount);

                if (newBusyCount < 0)
                    throw new UnreachableException("Unbalanced busy count decrement.");

                if (newBusyCount is 0)
                    IsBusyChanged?.Invoke(false);
            }
        }
    }
}
