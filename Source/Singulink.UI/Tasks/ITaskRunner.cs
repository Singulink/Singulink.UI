namespace Singulink.UI.Tasks;

/// <summary>
/// Represents a task runner than can execute tasks while managing busy state and ensuring exceptions don't get swallowed.
/// </summary>
public interface ITaskRunner
{
    /// <summary>
    /// Occurs when the value of <see cref="IsBusy"/> changes. Always executed on the UI thread. Can be used to trigger UI changes when busy tasks are running,
    /// such as disabling input or showing a loading spinner.
    /// </summary>
    public event Action<ITaskRunner> IsBusyChanged;

    /// <summary>
    /// Gets a value indicating whether the current thread is the UI thread associated with this task runner.
    /// </summary>
    public bool HasThreadAccess { get; }

    /// <summary>
    /// Gets a value indicating whether the task runner is currently executing busy tasks.
    /// </summary>
    public bool IsBusy { get; }

    /// <inheritdoc cref="RunAndForget(bool, Task)"/>
    public void RunAndForget(bool setBusyWhileRunning, Func<Task> taskFunc);

    /// <summary>
    /// Runs the specified task and optionally sets <see cref="IsBusy"/> while the task is running.
    /// </summary>
    /// <remarks>
    /// This method does not force the task to run on the UI thread if this method is called from a non-UI thread, but unhandled exceptions from the task will
    /// be propagated to the UI thread. If this method is expected to be called from a non-UI thread, the <see cref="SendAsync(Action)"/> method can be used to
    /// run some (or all) of the task on the UI thread.
    /// </remarks>
    public void RunAndForget(bool setBusyWhileRunning, Task task);

    /// <inheritdoc cref="RunAsBusyAsync(Task)"/>
    public Task RunAsBusyAsync(Func<Task> taskFunc);

    /// <inheritdoc cref="RunAsBusyAsync(Task)"/>
    public Task<T> RunAsBusyAsync<T>(Func<Task<T>> taskFunc);

    /// <summary>
    /// Runs the specified task while setting <see cref="IsBusy"/> until the task completes.
    /// </summary>
    /// <remarks>
    /// This method does not force the task to run on the UI thread if this method is called from a non-UI thread.
    /// </remarks>
    public Task RunAsBusyAsync(Task task);

    /// <summary>
    /// Asynchronously posts the specified action to the UI thread for execution. Tracked as a non-busy task until the action completes.
    /// </summary>
    public void Post(Action action);

    /// <summary>
    /// Synchronously executes the specified action if the current thread is the UI thread, otherwise asynchronously posts the action to the UI thread for
    /// execution and returns a task that completes when the action has finished executing. Tracked as a non-busy task until the action completes.
    /// </summary>
    public ValueTask SendAsync(Action action);

    /// <summary>
    /// Synchronously executes the specified action if the current thread is the UI thread, otherwise asynchronously posts the action to the UI thread for
    /// execution and returns a task that completes when the action has finished executing. Tracked as a non-busy task until the action completes.
    /// </summary>
    public ValueTask SendAsync<T>(T state, Action<T> action);
}
