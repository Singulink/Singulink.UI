using Playground.ViewModels;
using Playground.ViewModels.DialogTest;
using Playground.ViewModels.Home;
using Playground.ViewModels.ParamsTest;
using Playground.Views;
using Playground.Views.DialogTest;
using Playground.Views.Home;
using Playground.Views.ParamsTest;
using Singulink.UI.Navigation;
using Singulink.UI.Navigation.WinUI;
using Uno.Resizetizer;

namespace Playground;

public class AppWindow : Window
{
    private readonly Navigator _navigator;

    public AppWindow()
    {
#if DEBUG
        this.UseStudio();
#endif

        this.SetWindowIcon();

        var rootNav = new ContentControl() {
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
        };

        Content = rootNav;

        _navigator = CreateNavigator(rootNav);
        _navigator.HookWindowClosedEvents(this);
        _navigator.HookSystemNavigationRequests();
    }

    public async void BeginNavigation()
    {
        if (_navigator.CurrentRoute.Parts.Count is 0)
            await _navigator.NavigateAsync(Routes.LoginRoot);
    }

    private static Navigator CreateNavigator(ContentControl rootNav)
    {
        return new Navigator(ViewNavigator.Create(rootNav), builder => {
            builder.MapRoutedView<LoginViewModel, LoginRoot>();
            builder.MapRoutedView<MainViewModel, MainRoot>();
            builder.MapRoutedView<HomeViewModel, HomePage>();
            builder.MapRoutedView<DialogTestViewModel, DialogTestPage>();
            builder.MapRoutedView<ParamsTestViewModel, ParamsTestPage>();
            builder.MapRoutedView<ShowParamsTestViewModel, ShowParamsTestPage>();

            builder.MapDialog<DismissibleDialogViewModel, DismissibleDialog>();

            builder.ConfigureNavigationStacks(maxSize: 10, maxBackCachedDepth: 3, maxForwardCachedDepth: 3);
            builder.AddAllRoutes();
        });
    }
}
