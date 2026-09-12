namespace IconPackBuilder;

public partial class App : Application
{
    private AppWindow? _mainWindow;

    public App()
    {
        InitializeComponent();

        // Inside the VS Code webview the extension forwards console output to its output channel, and the windowed heads have no console at
        // all, so unhandled UI exceptions are also appended to a log file. Without this a XAML callback that throws just terminates the process
        // with a stowed exception and no managed detail.
        UnhandledException += (s, e) => LogUnhandledException("UI", e.Exception);

#if !__WASM__
        AppDomain.CurrentDomain.UnhandledException += (s, e) => LogUnhandledException("AppDomain", e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (s, e) => LogUnhandledException("Task", e.Exception);
#endif
    }

    /// <summary>
    /// Gets the path of the crash log written by <see cref="LogUnhandledException"/> on the windowed heads.
    /// </summary>
    public static string CrashLogPath { get; } = Path.Combine(Path.GetTempPath(), "IconPackBuilder-crash.log");

    private static void LogUnhandledException(string source, Exception? exception)
    {
        string text = $"[IconPackBuilder] {DateTime.Now:O} Unhandled {source} exception:{Environment.NewLine}{exception}";
        Console.Error.WriteLine(text);

#if !__WASM__
        try
        {
            File.AppendAllText(CrashLogPath, text + Environment.NewLine + Environment.NewLine);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Diagnostics must never be the reason the app fails.
        }
#endif
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _mainWindow ??= new();
        _mainWindow.Activate();
    }

    public static void InitializeLogging()
    {
#if DEBUG || __WASM__
        // Logging is disabled by default for release builds, as it incurs a significant
        // initialization cost from Microsoft.Extensions.Logging setup. If startup performance
        // is a concern for your application, keep this disabled. If you're running on the web or
        // desktop targets, you can use URL or command line parameters to enable it.
        //
        // For more performance documentation: https://platform.uno/docs/articles/Uno-UI-Performance.html

        var factory = LoggerFactory.Create(builder =>
        {
#if __WASM__
            builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
#elif __IOS__
            builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());

            // Log to the Visual Studio Debug console
            builder.AddConsole();
#else
            builder.AddConsole();
#endif

            // Exclude logs below this level
            builder.SetMinimumLevel(LogLevel.Information);

            // Default filters for Uno Platform namespaces
            builder.AddFilter("Uno", LogLevel.Warning);
            builder.AddFilter("Windows", LogLevel.Warning);
            builder.AddFilter("Microsoft", LogLevel.Warning);

            // Generic Xaml events
            // builder.AddFilter("Microsoft.UI.Xaml", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.VisualStateGroup", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.StateTriggerBase", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.UIElement", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.FrameworkElement", LogLevel.Trace );

            // Layouter specific messages
            // builder.AddFilter("Microsoft.UI.Xaml.Controls", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.Controls.Layouter", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.Controls.Panel", LogLevel.Debug );

            // builder.AddFilter("Windows.Storage", LogLevel.Debug );

            // Binding related messages
            // builder.AddFilter("Microsoft.UI.Xaml.Data", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.Data", LogLevel.Debug );

            // Binder memory references tracking
            // builder.AddFilter("Uno.UI.DataBinding.BinderReferenceHolder", LogLevel.Debug );

            // DevServer and HotReload related
            // builder.AddFilter("Uno.UI.RemoteControl", LogLevel.Information);

            // Debug JS interop
            // builder.AddFilter("Uno.Foundation.WebAssemblyRuntime", LogLevel.Debug );
        });

        global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;

#if HAS_UNO
        global::Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
#endif
#endif
    }
}
