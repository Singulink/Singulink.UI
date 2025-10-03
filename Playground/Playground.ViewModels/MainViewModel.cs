using CommunityToolkit.Mvvm.ComponentModel;
using Singulink.UI.Navigation;

namespace Playground.ViewModels;

public partial class MainViewModel : ObservableObject, IRoutedViewModel, IMessageProvider, IServiceProvider
{
    public INavigator Navigator => ViewModelExtensions.get_Navigator(this);

    public IReadOnlyList<MenuItem> MainMenuItems { get; } = [
        new("Home", Routes.Main.HomeChild),
        new("Dialog Test", Routes.Main.DialogTestChild),
        new("Parameters Test", Routes.Main.ParamsTestChild)
    ];

    public IReadOnlyList<MenuItem> FooterMenuItems { get; } = [
        new("Logout", null),
    ];

    [ObservableProperty]
    public partial MenuItem SelectedMenuItem { get; set; } = new("", null);

    partial void OnSelectedMenuItemChanged(MenuItem value)
    {
        if (Navigator.IsNavigating || Navigator.CurrentRoute.Parts.Last() == value.ChildRoutePart)
            return;

        this.TaskRunner.RunAndForget(async () => {
            if (value.ChildRoutePart is not null)
            {
                await Navigator.NavigatePartialAsync(value.ChildRoutePart);
                return;
            }

            await Task.Delay(500); // Simulate logout
            await Navigator.NavigateAsync(Routes.LoginRoot);
        });
    }

    public async Task OnNavigatedToAsync(NavigationArgs args)
    {
        SelectedMenuItem = MainMenuItems[0];
        await Task.Delay(1000);
    }

    public Task OnRouteNavigatedAsync(NavigationArgs args)
    {
        // If there is no child navigation (because a view routed directly to "/" instead of "/Home", for example),
        // navigate to the selected menu item (which is Home by default).

        if (!args.HasChildNavigation)
            args.Redirect = Redirect.NavigatePartial(SelectedMenuItem.ChildRoutePart!);

        return Task.CompletedTask;
    }

    public void BeginBackRequest() => this.TaskRunner.RunAndForget(async () => {
        await Navigator.GoBackAsync();
        SelectedMenuItem = MainMenuItems.First(mi => Navigator.CurrentPathStartsWith(Routes.MainRoot, mi.ChildRoutePart!));
    });

    string IMessageProvider.GetMessage() => "Hello from MainViewModel via IMessageProvider!";

    object? IServiceProvider.GetService(Type serviceType)
    {
        if (serviceType == typeof(MessageContainer))
            return new MessageContainer("Hello from MainViewModel via IServiceProvider + MessageContainer!");
        else
            return null;
    }
}

public record MenuItem(string Title, IConcreteChildRoutePart<MainViewModel>? ChildRoutePart);
