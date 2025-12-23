namespace Singulink.UI.Tasks;

/// <summary>
/// Represents a scope for a busy task.
/// </summary>
public sealed class BusyTaskScope : IDisposable
{
    private readonly TaskCompletionSource _tcs;

    internal Task Task => _tcs.Task;

    internal BusyTaskScope()
    {
        _tcs = new();
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="BusyTaskScope"/> class.
    /// </summary>
    ~BusyTaskScope() => Dispose();

    /// <summary>
    /// Disposes the busy task scope, signaling that the busy task has completed.
    /// </summary>
    public void Dispose()
    {
        _tcs.TrySetResult();
        GC.SuppressFinalize(this);
    }
}
