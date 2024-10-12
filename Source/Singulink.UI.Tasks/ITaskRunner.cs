using System.ComponentModel;

namespace Singulink.UI.Tasks;

/// <summary>
/// Represents a task runner that executes tasks while managing busy state and ensuring that exceptions propagate to the UI thread.
/// </summary>
/// <remarks>
/// Instances are fully thread-safe and all methods can be safely called from any thread.
/// </remarks>
public interface ITaskRunner : INotifyPropertyChanged
{
    /// <summary>
    /// Gets a value indicating whether the current thread is the UI thread associated with this task runner.
    /// </summary>
    public bool HasThreadAccess { get; }

    /// <summary>
    /// Gets a value indicating whether the task runner is currently executing busy tasks. Bindings to this property can be used to control UI state when busy
    /// tasks are running, such as disabling input or showing a loading spinner.
    /// </summary>
    public bool IsBusy { get; }

    /// <inheritdoc cref="RunAndForget(bool, Task)"/>
    public void RunAndForget(bool setBusy, Func<Task> taskFunc);

    /// <summary>
    /// Runs the specified task and optionally sets <see cref="IsBusy"/> while the task is running.
    /// </summary>
    /// <remarks>
    /// This method does not force the task to run on the UI thread if this method is called from a non-UI thread, but unhandled exceptions from the task will
    /// be propagated to the UI thread. If this method is expected to be called from a non-UI thread, the <see cref="SendAsync(Action)"/> method can be used to
    /// synchronize some (or all) of the execution to the UI thread.
    /// </remarks>
    public void RunAndForget(bool setBusy, Task task);

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
    /// Asynchronously posts the specified action to the UI thread for execution. Tracked as a non-busy task.
    /// </summary>
    public void Post(Action action);

    /// <summary>
    /// Synchronously executes the specified action if the current thread is the UI thread, otherwise asynchronously posts it to the UI thread for
    /// execution and returns a task that completes when execution finishes. Tracked as a non-busy task.
    /// </summary>
    public ValueTask SendAsync(Action action);

    /// <summary>
    /// Synchronously executes the specified action if the current thread is the UI thread, otherwise asynchronously posts it to the UI thread
    /// for execution and returns a task that completes when execution finishes. Tracked as a non-busy task.
    /// </summary>
    public ValueTask SendAsync<T>(T state, Action<T> action);

    /// <summary>
    /// Synchronously executes the specified function if the current thread is the UI thread, otherwise asynchronously posts it to the UI
    /// thread for execution and returns a task that contains the result when execution finishes. Tracked as a non-busy task.
    /// </summary>
    public ValueTask<TResult> SendAsync<TResult>(Func<TResult> func);

    /// <summary>
    /// Synchronously executes the specified function using the provided state if the current thread is the UI thread, otherwise asynchronously posts them to
    /// the UI thread for execution and returns a task that contains the result of the function when execution finishes. Tracked as a non-busy task.
    /// </summary>
    public ValueTask<TResult> SendAsync<T, TResult>(T state, Func<T, TResult> func);

    /// <summary>
    /// Synchronously executes the specified function if the current thread is the UI thread, otherwise asynchronously posts it to the UI thread for execution,
    /// and then returns a task that completes when the execution of the task returned by the function completes. Tracked as a non-busy task.
    /// </summary>
    public Task SendAsync(Func<Task> taskFunc);

    /// <summary>
    /// Synchronously executes the specified function if the current thread is the UI thread, otherwise asynchronously posts it to the UI thread for execution,
    /// and then returns a task that contains the result of the task returned by the function when it completes. Tracked as a non-busy task.
    /// </summary>
    public Task<TResult> SendAsync<TResult>(Func<Task<TResult>> taskFunc);
}
