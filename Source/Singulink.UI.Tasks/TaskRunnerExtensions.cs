namespace Singulink.UI.Tasks;

/// <summary>
/// Provides extension methods for <see cref="ITaskRunner"/>.
/// </summary>
public static class TaskRunnerExtensions
{
    /// <summary>
    /// Enters a busy task scope, marking the task runner as busy until the scope is disposed.
    /// </summary>
    public static BusyTaskScope EnterBusyScope(this ITaskRunner taskRunner)
    {
        var scope = new BusyTaskScope();
        taskRunner.RunAsBusyAndForget(scope.Task);
        return scope;
    }
}
