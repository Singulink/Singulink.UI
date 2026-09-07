using global::Avalonia;
using global::Avalonia.Headless;
using global::Avalonia.Themes.Simple;
using global::Avalonia.Threading;
using PrefixClassName.MsTest;

namespace Singulink.UI.Icons.Avalonia.Tests;

/// <summary>
/// Starts a headless Avalonia application once for the test run and runs test bodies on its UI thread.
/// </summary>
#pragma warning disable RCS1102 // MSTest requires a non-static class for assembly initialization.
[PrefixTestClass]
public class HeadlessApp
{
    [AssemblyInitialize]
    public static void Initialize(TestContext context)
    {
        AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions()).SetupWithoutStarting();
    }

    public static void RunOnUIThread(Action action) => Dispatcher.UIThread.Invoke(action);

    private sealed class App : Application
    {
        public override void Initialize() => Styles.Add(new SimpleTheme());
    }
}
