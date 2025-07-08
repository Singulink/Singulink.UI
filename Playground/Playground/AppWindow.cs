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
using Singulink.UI.Tasks;
using Uno.Resizetizer;

namespace Playground;

public class AppWindow : Window
{
    private readonly ITaskRunner _taskRunner;
    private readonly INavigator _navigator;

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

        _taskRunner = new TaskRunner(busy => rootNav.IsEnabled = !busy);

        _navigator = CreateNavigator(rootNav);
        _navigator.RegisterAsyncNavigationHandler(t => _taskRunner.RunAndForget(true, t));
        _navigator.RegisterInitializeViewHandler<UIElement, IProvideTaskRunner>((view, vm) =>
        {
            if (view is ContentDialog dialog)
                vm.TaskRunner = new TaskRunner(busy => dialog.IsEnabled = !busy);
            else
                vm.TaskRunner = _taskRunner;
        });

#if !WINDOWS
        var navManager = Windows.UI.Core.SystemNavigationManager.GetForCurrentView();
        navManager.AppViewBackButtonVisibility = Windows.UI.Core.AppViewBackButtonVisibility.Visible;

        navManager.BackRequested += (s, e) =>
        {
            _navigator.GoBackAsync(out bool handled);
            e.Handled = handled;
        };
#endif
    }

    public async void NavigateInitial()
    {
        if (!_navigator.DidNavigate)
            await _navigator.NavigateAsync(Routes.LoginRoute);
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
            builder.MapDialog<DismissableDialogViewModel, DismissableDialog>();

            builder.AddAllRoutes();
        });
    }
}
