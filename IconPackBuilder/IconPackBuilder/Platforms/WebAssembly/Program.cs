using Uno.UI.Hosting;

namespace IconPackBuilder;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        App.InitializeLogging();

        // The WebAssembly head runs inside a VS Code webview where nothing else surfaces .NET failures, so report them to the browser console
        // (the extension forwards console output to its "Icon Pack Builder" output channel).
        AppDomain.CurrentDomain.UnhandledException += (s, e) => Console.Error.WriteLine($"[IconPackBuilder] Unhandled exception: {e.ExceptionObject}");
        TaskScheduler.UnobservedTaskException += (s, e) => Console.Error.WriteLine($"[IconPackBuilder] Unobserved task exception: {e.Exception}");

        try
        {
            var host = UnoPlatformHostBuilder.Create()
                .App(() => new App())
                .UseWebAssembly()
                .Build();

            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[IconPackBuilder] Startup failed: {ex}");
            throw;
        }
    }
}
