using IconPackBuilder.Core;
using IconPackBuilder.Core.IconSources;
using IconPackBuilder.Core.Services;
using IconPackBuilder.Services;
using IconPackBuilder.ViewModels;
using IconPackBuilder.Views;
using Singulink.UI.Navigation.WinUI;
using Uno.Resizetizer;

namespace IconPackBuilder;

public sealed class AppWindow : Window, IWindow
{
    private readonly Navigator _navigator;

    public AppWindow()
    {
#if DEBUG
        this.UseStudio(launchHotDesignOnStart: false);
#endif

        Title = "Icon Pack Builder";
        this.SetWindowIcon();

        var services = new ServiceCollection();
        services.AddSingleton<IWindow>(this);

#if __WASM__
        // The WebAssembly head runs either inside the VS Code extension's webview, where the document, saving and exporting are all owned by
        // the extension host, or as a stand-alone page where projects live in browser storage and exports are downloaded.
        bool isVsCode = Browser.BrowserHost.IsVsCode();
        VsCode.VsCodeHostServices? vsCode = null;
        Browser.BrowserHostServices? browser = null;

        if (isVsCode)
        {
            vsCode = new VsCode.VsCodeHostServices();
            services.AddSingleton<IconsSource>(sp => vsCode.IconsSource);
            services.AddSingleton<IProjectDocumentFactory>(vsCode);
            services.AddSingleton<IExportService>(vsCode);
            services.AddSingleton<IFileDialogHandler>(vsCode);
            services.AddSingleton<IRecentProjectsStore>(new RecentProjectsStore());
            services.AddSingleton<IHostInfo>(DefaultHostInfo.Instance);
        }
        else
        {
            browser = new Browser.BrowserHostServices();
            services.AddSingleton<IconsSource>(sp => browser.IconsSource);
            services.AddSingleton<IProjectDocumentFactory>(browser);
            services.AddSingleton<IExportService>(browser);
            services.AddSingleton<IFileDialogHandler>(browser);
            services.AddSingleton<IRecentProjectsStore>(browser);
            services.AddSingleton<IHostInfo>(browser);
        }
#else
        services.AddSingleton<IconsSource>(SeagullIconsSource.Instance);
        services.AddSingleton<IProjectDocumentFactory>(new FileProjectDocumentFactory());
        services.AddSingleton<IExportService>(new FileExportService(new PyFtSubsetter(), Exporters.All));
        services.AddSingleton<IFileDialogHandler>(new FileDialogHandler(this));
        services.AddSingleton<IRecentProjectsStore>(new RecentProjectsStore());
        services.AddSingleton<IHostInfo>(DefaultHostInfo.Instance);
#endif

        _navigator = new Navigator(this, builder => {
            builder.Services = services.BuildServiceProvider();

            builder.MapRoutedView<StartRootModel, StartRoot>();
            builder.MapRoutedView<EditorRootModel, EditorRoot>();
            builder.MapDialog<PreviewIconPackDialogModel, PreviewIconPackDialog>();

            builder.AddAllRoutes();
        });

#if __WASM__
        if (vsCode is not null)
        {
            // The host hands us exactly one document, which opens directly in the editor once the bridge and icon metadata are ready.
            _navigator.HookWindowActivatedEvent(this, async n => {
                try
                {
                    await vsCode.InitializeAsync();

                    ApplyTheme(VsCode.VsCodeHost.GetThemeKind());
                    VsCode.VsCodeHost.OnThemeChanged(ApplyTheme);

                    await n.NavigateAsync(Routes.EditorRoot.ToConcrete(vsCode.DocumentPath));
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[IconPackBuilder] Failed to open the document from VS Code: {ex}");
                    throw;
                }
            });
        }
        else
        {
            _navigator.HookWindowActivatedEvent(this, async n => {
                try
                {
                    Browser.BrowserHost.SetTitle("Icon Pack Builder");
                    await browser!.InitializeAsync();

                    ApplyTheme(Browser.BrowserHost.GetThemeKind());
                    Browser.BrowserHost.OnThemeChanged(ApplyTheme);

                    await n.NavigateAsync(Routes.StartRoot);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[IconPackBuilder] Failed to start: {ex}");
                    throw;
                }
            });

            // System navigation is deliberately not hooked in either WebAssembly host. In VS Code there is no address bar to sync and the mouse
            // back/forward buttons belong to VS Code; in the browser, rewriting the URL to the editor route makes the app's relative asset
            // requests (fonts) resolve against that route, and a refresh of such a URL has no page behind it on static hosting.
        }
#else
        // A project file passed on the command line opens directly in the editor.
        string? projectPath = Environment.GetCommandLineArgs().Skip(1).FirstOrDefault(a => a.EndsWith(".ipproj", StringComparison.OrdinalIgnoreCase));

        _navigator.HookWindowActivatedEvent(this, n => projectPath is not null && File.Exists(projectPath)
            ? n.NavigateAsync(Routes.EditorRoot.ToConcrete(Path.GetFullPath(projectPath)))
            : n.NavigateAsync(Routes.StartRoot));

        _navigator.HookSystemNavigationRequests();
#endif
        _navigator.HookWindowClosedEvents(this);
    }

#if __WASM__
    /// <summary>
    /// Matches the app theme to the host's color scheme: <c>light</c>, <c>dark</c>, or VS Code's <c>highContrast</c> / <c>highContrastLight</c>.
    /// </summary>
    private void ApplyTheme(string themeKind)
    {
        if (Content is FrameworkElement root)
            root.RequestedTheme = themeKind is "light" or "highContrastLight" ? ElementTheme.Light : ElementTheme.Dark;
    }
#endif
}
