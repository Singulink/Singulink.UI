namespace Singulink.UI.Tasks;

/// <summary>
/// Provides extension methods for <see cref="ITaskRunner"/>.
/// </summary>
public static class TaskRunnerExtensions
{
    /// <summary>
    /// Enters a busy task scope, marking the task runner as busy until the scope is disposed. The scope MUST be disposed to signal the completion of the busy
    /// task, otherwise an exception will be thrown when the scope is finalized by the garbage collector. This method is intended to be used with a using
    /// statement to ensure proper disposal of the scope.
    /// </summary>
    public static BusyTaskScope EnterBusyScope(this ITaskRunner taskRunner)
    {
        var scope = new BusyTaskScope();
        taskRunner.RunAsBusyAndForget(scope.Task);
        return scope;
    }
}
