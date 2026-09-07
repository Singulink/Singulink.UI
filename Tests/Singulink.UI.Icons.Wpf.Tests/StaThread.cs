using System.Runtime.ExceptionServices;

namespace Singulink.UI.Icons.Wpf.Tests;

/// <summary>
/// Runs a test body on a dedicated STA thread, which WPF elements require.
/// </summary>
public static class StaThread
{
    public static void Run(Action action)
    {
        ExceptionDispatchInfo? error = null;

        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                error = ExceptionDispatchInfo.Capture(ex);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        error?.Throw();
    }
}
