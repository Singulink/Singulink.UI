using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Singulink.UI.Tasks;

/// <summary>
/// Represents a scope for a busy task.
/// </summary>
public sealed partial class BusyTaskScope : IDisposable
{
    private static readonly bool _captureStack = GetDefaultCaptureStack();

    private readonly TaskCompletionSource _tcs;
    private StackTrace? _stackTrace;

    internal Task Task => _tcs.Task;

    internal BusyTaskScope()
    {
        _tcs = new();

        if (_captureStack)
            _stackTrace = new StackTrace(true);
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2045:AttributeRemoval",
        Justification = "DebuggableAttribute may be removed by the trimmer; the null-conditional lookup then defaults stack capture to off, which is the desired behavior for trimmed/optimized builds.")]
    private static bool GetDefaultCaptureStack()
    {
        if (AppContext.TryGetSwitch("Singulink.UI.Tasks.CaptureBusyTaskStackTraces", out bool captureStack))
            return captureStack;

        return Assembly.GetEntryAssembly()?.GetCustomAttribute<DebuggableAttribute>()?.IsJITOptimizerDisabled ?? false;
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="BusyTaskScope"/> class. This will throw an exception if the scope was not disposed properly.
    /// </summary>
    ~BusyTaskScope()
    {
        string message = "BusyTaskScope was not disposed properly. Ensure that Dispose is called to signal the completion of the busy task.";

        if (_stackTrace != null)
            message = $"{message}{Environment.NewLine}{Environment.NewLine}Allocation stack trace:{Environment.NewLine}{_stackTrace}";

        throw new InvalidOperationException(message);
    }

    /// <summary>
    /// Disposes the busy task scope, signaling that the busy task has completed.
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _tcs.TrySetResult();
    }
}
