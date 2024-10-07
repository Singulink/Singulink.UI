using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace Singulink.UI.Tasks;

/// <summary>
/// Represents a task runner that executes tasks while managing busy state and ensuring that exceptions propagate to the UI thread.
/// </summary>
public class TaskRunner : ITaskRunner
{
    private readonly Thread _thread;
    private readonly SynchronizationContext _syncContext;
    private readonly object _syncRoot = new();

    private int _busyTaskCount;
    private int _nonBusyTaskCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskRunner"/> class using the current UI thread and synchronization context. Must be called on the
    /// UI thread that this task runner will be associated with.
    /// </summary>
    /// <exception cref="InvalidOperationException">A current synchronization context was not found.</exception>
    public TaskRunner()
    {
        _syncContext = SynchronizationContext.Current ?? throw new InvalidOperationException("Synchronization context not available on the current thread.");
        _thread = Thread.CurrentThread;
    }

    /// <inheritdoc cref="ITaskRunner.IsBusyChanged"/>
    public event Action<ITaskRunner>? IsBusyChanged;

    /// <summary>
    /// Occurs whenever all busy tasks have completed as well as when all tasks (including non-busy tasks) have completed.
    /// </summary>
    private event Action? TasksCompleted;

    /// <inheritdoc cref="ITaskRunner.IsBusy"/>
    public bool IsBusy => Volatile.Read(ref _busyTaskCount) > 0;

    /// <inheritdoc cref="ITaskRunner.HasThreadAccess"/>
    public bool HasThreadAccess => Thread.CurrentThread == _thread;

    /// <inheritdoc cref="ITaskRunner.RunAndForget(bool, Func{Task})"/>
    public void RunAndForget(bool setBusy, Func<Task> taskFunc)
    {
        Task task;

        try
        {
            task = taskFunc();
        }
        catch (Exception ex) when (!HasThreadAccess)
        {
            var edi = ExceptionDispatchInfo.Capture(ex);
            _syncContext.Post(static s => ((ExceptionDispatchInfo)s!).Throw(), edi);

            return;
        }

        RunAndForget(setBusy, task);
    }

    /// <inheritdoc cref="ITaskRunner.RunAndForget(bool, Task)"/>
    public async void RunAndForget(bool setBusy, Task task)
    {
        bool taskWasCompleted = task.IsCompleted;

        if (!taskWasCompleted)
            IncrementTaskCount(setBusy);

        try
        {
            await task;
        }
        catch (Exception ex) when (!HasThreadAccess)
        {
            var edi = ExceptionDispatchInfo.Capture(ex);
            _syncContext.Post(static s => ((ExceptionDispatchInfo)s)!.Throw(), edi);
        }
        finally
        {
            if (!taskWasCompleted)
                DecrementTaskCount(setBusy);
        }
    }

    /// <inheritdoc cref="ITaskRunner.RunAsBusyAsync(Task)"/>
    public Task RunAsBusyAsync(Func<Task> taskFunc)
    {
        var task = taskFunc();
        return RunAsBusyAsync(task);
    }

    /// <inheritdoc cref="ITaskRunner.RunAsBusyAsync{T}(Func{Task{T}})"/>
    public async Task<T> RunAsBusyAsync<T>(Func<Task<T>> taskFunc)
    {
        var task = taskFunc();
        await RunAsBusyAsync(task);
        return task.Result;
    }

    /// <inheritdoc cref="ITaskRunner.RunAsBusyAsync(Task)"/>
    public async Task RunAsBusyAsync(Task task)
    {
        bool taskWasCompleted = task.IsCompleted;

        if (!taskWasCompleted)
            IncrementTaskCount(true);

        try
        {
            await task;
        }
        finally
        {
            if (!taskWasCompleted)
                DecrementTaskCount(true);
        }
    }

    /// <inheritdoc cref="ITaskRunner.Post"/>
    public void Post(Action action)
    {
        IncrementTaskCount(false);

        _syncContext.Post(_ => {
            try
            {
                action.Invoke();
            }
            finally
            {
                DecrementTaskCount(false);
            }
        }, null);
    }

    /// <inheritdoc cref="ITaskRunner.SendAsync(Action)"/>
    public async ValueTask SendAsync(Action action)
    {
        if (HasThreadAccess)
        {
            action.Invoke();
            return;
        }

        IncrementTaskCount(false);

        try
        {
            var tcs = new InvokeActionTaskCompletionSource(action);
            PostInvocable(tcs);

            await tcs.Task;
        }
        finally
        {
            DecrementTaskCount(false);
        }
    }

    /// <inheritdoc cref="ITaskRunner.SendAsync{T}(T, Action{T})"/>
    public async ValueTask SendAsync<T>(T state, Action<T> action)
    {
        if (HasThreadAccess)
        {
            action.Invoke(state);
            return;
        }

        IncrementTaskCount(false);

        try
        {
            var tcs = new InvokeActionTaskCompletionSource<T>(state, action);
            PostInvocable(tcs);

            await tcs.Task;
        }
        finally
        {
            DecrementTaskCount(false);
        }
    }

    /// <inheritdoc cref="ITaskRunner.SendAsync{TResult}(Func{TResult})"/>
    public async ValueTask<TResult> SendAsync<TResult>(Func<TResult> func)
    {
        if (HasThreadAccess)
            return func.Invoke();

        IncrementTaskCount(false);

        try
        {
            var tcs = new InvokeFuncTaskCompletionSource<TResult>(func);
            PostInvocable(tcs);

            return await tcs.Task;
        }
        finally
        {
            DecrementTaskCount(false);
        }
    }

    /// <inheritdoc cref="ITaskRunner.SendAsync{T, TResult}(T, Func{T, TResult})"/>
    public async ValueTask<TResult> SendAsync<T, TResult>(T state, Func<T, TResult> func)
    {
        if (HasThreadAccess)
            return func.Invoke(state);

        IncrementTaskCount(false);

        try
        {
            var tcs = new InvokeFuncTaskCompletionSource<T, TResult>(state, func);
            PostInvocable(tcs);

            return await tcs.Task;
        }
        finally
        {
            DecrementTaskCount(false);
        }
    }

    /// <summary>
    /// Waits for all busy tasks to complete, optionally also waiting for non-busy tasks (and posted/sent messages).
    /// </summary>
    public async Task WaitForIdle(bool waitForNonBusyTasks = false)
    {
        TaskCompletionSource tcs = null;

        lock (_syncRoot)
        {
            if (_busyTaskCount is not 0 || (waitForNonBusyTasks && _nonBusyTaskCount is not 0))
            {
                tcs = new();

                void OnTasksCompleted()
                {
                    lock (_syncRoot)
                    {
                        if (_busyTaskCount > 0 || (waitForNonBusyTasks && _nonBusyTaskCount > 0))
                            return;
                    }

                    tcs.TrySetResult();
                    TasksCompleted -= OnTasksCompleted;
                }

                TasksCompleted += OnTasksCompleted;
            }
        }

        if (tcs is not null)
            await tcs.Task;

        // Yield one more message cycle to ensure all currently posted messages are processed.

        if (HasThreadAccess)
            await Task.Yield();
        else
            await SendAsync(() => { });
    }

    private void IncrementTaskCount(bool busyTask)
    {
        int busyCount;
        int nonBusyCount;

        lock (_syncRoot)
        {
            if (busyTask)
                (busyCount, nonBusyCount) = (++_busyTaskCount, _nonBusyTaskCount);
            else
                (busyCount, nonBusyCount) = (_busyTaskCount, ++_nonBusyTaskCount);
        }

        if ((busyTask && busyCount <= 0) || (!busyTask && nonBusyCount <= 0))
            throw new UnreachableException($"Invalid incremented task counts ({busyCount} / {nonBusyCount}).");

        if (!busyTask)
            return;

        if (busyCount is 1)
        {
            if (HasThreadAccess)
                OnIsBusyChangedWithCountRollback();
            else
                Post(OnIsBusyChanged);
        }

        void OnIsBusyChangedWithCountRollback()
        {
            try
            {
                OnIsBusyChanged();
            }
            catch
            {
                lock (_syncRoot)
                    _busyTaskCount--;

                throw;
            }
        }
    }

    private void DecrementTaskCount(bool busyTask)
    {
        int busyCount;
        int nonBusyCount;

        lock (_syncRoot)
        {
            if (busyTask)
                (busyCount, nonBusyCount) = (--_busyTaskCount, _nonBusyTaskCount);
            else
                (busyCount, nonBusyCount) = (_busyTaskCount, --_nonBusyTaskCount);
        }

        if (busyCount < 0 || nonBusyCount < 0)
            throw new UnreachableException($"Invalid decremented task counts ({busyCount} / {nonBusyCount}).");

        if (busyCount is 0)
        {
            if (busyTask)
            {
                try
                {
                    if (HasThreadAccess)
                        OnIsBusyChanged();
                    else
                        Post(OnIsBusyChanged);
                }
                finally
                {
                    TasksCompleted?.Invoke();
                }
            }
            else if (nonBusyCount is 0)
            {
                TasksCompleted?.Invoke();
            }
        }
    }

    private void PostInvocable(IInvokable invokable) => _syncContext.Post(static s => ((IInvokable)s!).Invoke(), invokable);

    private void OnIsBusyChanged() => IsBusyChanged?.Invoke(this);
}
