using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace Singulink.UI.Tasks;

/// <inheritdoc cref="ITaskRunner"/>
public partial class TaskRunner : ITaskRunner
{
    private static readonly PropertyChangedEventArgs IsBusyPropertyChangedArgs = new(nameof(IsBusy));

    private readonly Thread _thread;
    private readonly SynchronizationContext _syncContext;
    private readonly Action<bool>? _busyChangedAction;
    private readonly object _syncRoot = new();

    private int _busyTaskCount;
    private int _nonBusyTaskCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskRunner"/> class using the current UI thread and synchronization context. Must be called on the
    /// UI thread that this task runner will be associated with.
    /// </summary>
    /// <param name="busyChangedAction">Optional action to execute on the UI thread whenever busy state changes. Can be used to control UI state when busy
    /// tasks are running, such as disabling input or showing a loading spinner.</param>
    /// <exception cref="InvalidOperationException">A current synchronization context was not found.</exception>
    public TaskRunner(Action<bool>? busyChangedAction = null)
    {
        _syncContext = SynchronizationContext.Current ?? throw new InvalidOperationException("Synchronization context not available on the current thread.");
        _thread = Thread.CurrentThread;
        _busyChangedAction = busyChangedAction;
    }

    /// <inheritdoc cref="INotifyPropertyChanged.PropertyChanged"/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Occurs whenever all busy tasks have completed as well as when all tasks (including non-busy tasks) have completed.
    /// </summary>
    private event Action? BusyTasksOrAllTasksCompleted;

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
        catch (Exception ex)
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
        catch (Exception ex)
        {
            CaptureAndPostException(ex);
        }
        finally
        {
            if (!taskWasCompleted)
                DecrementTaskCount(setBusy);
        }
    }

    private void CaptureAndPostException(Exception ex)
    {
        var edi = ExceptionDispatchInfo.Capture(ex);
        _syncContext.Post(static s => ((ExceptionDispatchInfo)s)!.Throw(), edi);
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

    /// <inheritdoc cref="ITaskRunner.SendAsync(Func{Task})"/>
    public async Task SendAsync(Func<Task> taskFunc)
    {
        Task task = null;

        if (HasThreadAccess)
        {
            task = taskFunc.Invoke();

            if (task.IsCompleted)
            {
                await task;
                return;
            }
        }

        IncrementTaskCount(false);

        try
        {
            if (task is null)
            {
                var tcs = new InvokeFuncTaskCompletionSource<Task>(taskFunc);
                PostInvocable(tcs);
                task = await tcs.Task;
            }

            await task;
        }
        finally
        {
            DecrementTaskCount(false);
        }
    }

    /// <inheritdoc cref="ITaskRunner.SendAsync{TResult}(Func{Task{TResult}})"/>
    public async Task<TResult> SendAsync<TResult>(Func<Task<TResult>> taskFunc)
    {
        Task<TResult> task = null;

        if (HasThreadAccess)
        {
            task = taskFunc.Invoke();

            if (task.IsCompleted)
                return await task;
        }

        IncrementTaskCount(false);

        try
        {
            if (task is null)
            {
                var tcs = new InvokeFuncTaskCompletionSource<Task<TResult>>(taskFunc);
                PostInvocable(tcs);
                task = await tcs.Task;
            }

            return await task;
        }
        finally
        {
            DecrementTaskCount(false);
        }
    }

    /// <summary>
    /// Waits for all busy tasks to complete, optionally also waiting for non-busy tasks (and posted/sent messages).
    /// </summary>
    public async Task WaitForIdleAsync(bool waitForNonBusyTasks = false)
    {
        TaskCompletionSource tcs = null;

        lock (_syncRoot)
        {
            if (_busyTaskCount > 0 || (waitForNonBusyTasks && _nonBusyTaskCount > 0))
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
                    BusyTasksOrAllTasksCompleted -= OnTasksCompleted;
                }

                BusyTasksOrAllTasksCompleted += OnTasksCompleted;
            }
        }

        if (tcs is not null)
            await tcs.Task;

        // Yield one more message cycle to ensure all currently posted messages are processed,
        // i.e. if exceptions were posted in one of the tasks we were waiting for.

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
                    OnIsBusyChanged();
                }
                finally
                {
                    BusyTasksOrAllTasksCompleted?.Invoke();
                }
            }
            else if (nonBusyCount is 0)
            {
                BusyTasksOrAllTasksCompleted?.Invoke();
            }
        }
    }

    private void PostInvocable(IInvokable invokable) => _syncContext.Post(static s => ((IInvokable)s!).Invoke(), invokable);

    private void OnIsBusyChanged()
    {
        if (!HasThreadAccess)
        {
            if (_busyChangedAction is not null || PropertyChanged is not null)
                Post(OnIsBusyChanged);

            return;
        }

        _busyChangedAction?.Invoke(IsBusy);
        PropertyChanged?.Invoke(this, IsBusyPropertyChangedArgs);
    }
}
