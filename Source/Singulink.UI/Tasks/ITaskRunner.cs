namespace Singulink.UI.Tasks;

/// <summary>
/// Represents a task runner than can execute tasks while managing busy state and ensuring exceptions don't get swallowed.
/// </summary>
public interface ITaskRunner
{
    /// <summary>
    /// Occurs when the busy state of the task runner changes.
    /// </summary>
    public event Action<bool> IsBusyChanged;

    /// <summary>
    /// Gets a value indicating whether the task runner is currently busy executing tasks.
    /// </summary>
    public bool IsBusy { get; }

    /// <summary>
    /// Runs the specified task and optionally sets the busy state while the task is running.
    /// </summary>
    public void Run(bool setBusyWhileRunning, Task task);

    /// <summary>
    /// Runs the specified task and optionally sets the busy state while the task is running.
    /// </summary>
    public Task Run(bool setBusyWhileRunning, Func<Task> taskFunc);

    /// <summary>
    /// Runs the specified task and optionally sets the busy state while the task is running.
    /// </summary>
    public Task<T> Run<T>(bool setBusyWhileRunning, Func<Task<T>> taskFunc);
}
