using Nito.AsyncEx;

namespace Singulink.UI.Navigation.Tests.TestSupport;

/// <summary>
/// Helper for running test bodies inside an <see cref="AsyncContext"/> so that <see cref="Singulink.UI.Tasks.TaskRunner"/> has a single-threaded
/// synchronization context to capture, matching the runtime UI-thread scenario.
/// </summary>
public static class AsyncContextTest
{
    /// <summary>
    /// Runs the specified async test body inside a fresh <see cref="AsyncContext"/>.
    /// </summary>
    public static void Run(Func<Task> testBody) => AsyncContext.Run(testBody);
}
