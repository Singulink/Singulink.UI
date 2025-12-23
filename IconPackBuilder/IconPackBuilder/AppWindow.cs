using IconPackBuilder.Core;
using IconPackBuilder.Core.Services;
using IconPackBuilder.IconSources;
using IconPackBuilder.Services;
using IconPackBuilder.ViewModels;
using IconPackBuilder.Views;
using Singulink.UI.Navigation;
using Singulink.UI.Navigation.WinUI;
using Uno.Resizetizer;

namespace IconPackBuilder;

public sealed class AppWindow : Window
{
    private readonly Navigator _navigator;

    public AppWindow()
    {
#if DEBUG
        this.UseStudio();
#endif

        Title = "Icon Pack Builder";
        this.SetWindowIcon();

        var services = new ServiceCollection();

        services.AddSingleton<IconsSource>(SeagullIconSource.Instance);
        services.AddSingleton<IFontSubsetter>(new PyFtSubsetter());
        services.AddSingleton<IExporter>(CSharpExporter.Instance);
        services.AddSingleton<IFileDialogHandler>(new FileDialogHandler(this));

        var rootNav = new ContentControl() {
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
        };

        Content = rootNav;
        _navigator = CreateNavigator(rootNav, services.BuildServiceProvider());

#if !WINDOWS
        var navManager = Windows.UI.Core.SystemNavigationManager.GetForCurrentView();
        navManager.AppViewBackButtonVisibility = Windows.UI.Core.AppViewBackButtonVisibility.Visible;
        navManager.BackRequested += (s, e) => e.Handled = _navigator.HandleSystemBackRequest();
#endif
    }

    public async void BeginNavigation()
    {
        if (_navigator.CurrentRoute.Parts.Count is 0 && !_navigator.IsNavigating)
            await _navigator.NavigateAsync(Routes.StartRoot);
    }

    private static Navigator CreateNavigator(ContentControl rootNav, IServiceProvider services)
    {
        return new Navigator(ViewNavigator.Create(rootNav), builder => {
            builder.Services = services;

            builder.MapRoutedView<StartRootModel, StartRoot>();
            builder.MapRoutedView<EditorRootModel, EditorRoot>();

            builder.AddAllRoutes();
        });
    }
}
