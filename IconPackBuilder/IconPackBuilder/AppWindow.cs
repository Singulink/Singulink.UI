using IconPackBuilder.Core;
using IconPackBuilder.Core.Services;
using IconPackBuilder.IconSources;
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

        _navigator = new Navigator(this, builder => {
            builder.Services = services.BuildServiceProvider();

            builder.MapRoutedView<StartRootModel, StartRoot>();
            builder.MapRoutedView<EditorRootModel, EditorRoot>();
            builder.MapDialog<PreviewIconPackDialogModel, PreviewIconPackDialog>();

            builder.AddAllRoutes();
        });

        _navigator.HookWindowActivatedEvent(this, n => n.NavigateAsync(Routes.StartRoot));
        _navigator.HookSystemNavigationRequests();
        _navigator.HookWindowClosedEvents(this);
    }
}
