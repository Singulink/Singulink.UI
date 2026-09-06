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
        services.AddSingleton<IconsSource>(SeagullIconsSource.Instance);
        services.AddSingleton<IFontSubsetter>(new PyFtSubsetter());
        services.AddSingleton<IExporter>(CSharpExporter.Instance);
        services.AddSingleton<IFileDialogHandler>(new FileDialogHandler(this));
        services.AddSingleton<IRecentProjectsStore>(new RecentProjectsStore());

        _navigator = new Navigator(this, builder => {
            builder.Services = services.BuildServiceProvider();

            builder.MapRoutedView<StartRootModel, StartRoot>();
            builder.MapRoutedView<EditorRootModel, EditorRoot>();
            builder.MapDialog<PreviewIconPackDialogModel, PreviewIconPackDialog>();

            builder.AddAllRoutes();
        });

        // A project file passed on the command line opens directly in the editor.
        string? projectPath = Environment.GetCommandLineArgs().Skip(1).FirstOrDefault(a => a.EndsWith(".ipproj", StringComparison.OrdinalIgnoreCase));

        _navigator.HookWindowActivatedEvent(this, n => projectPath is not null && File.Exists(projectPath)
            ? n.NavigateAsync(Routes.EditorRoot.ToConcrete(Path.GetFullPath(projectPath)))
            : n.NavigateAsync(Routes.StartRoot));
        _navigator.HookSystemNavigationRequests();
        _navigator.HookWindowClosedEvents(this);
    }
}
