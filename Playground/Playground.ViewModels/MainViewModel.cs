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

    partial void OnSelectedMenuItemChanged(MenuItem oldValue, MenuItem newValue)
    {
        if (Navigator.IsNavigating || Navigator.CurrentRoute.Parts.Contains(newValue.ChildRoutePart))
            return;

        this.TaskRunner.RunAndForget(async () => {
            NavigationResult result;

            if (newValue.ChildRoutePart is not null)
            {
                result = await Navigator.NavigatePartialAsync(newValue.ChildRoutePart);
            }
            else
            {
                await Task.Delay(500); // Simulate logout
                result = await Navigator.NavigateAsync(Routes.LoginRoot);
            }

            if (result is not NavigationResult.Success)
                SelectedMenuItem = oldValue;
        });
    }

    public async Task OnNavigatedToAsync(NavigationArgs args)
    {
        this.SetChildService(new MessageContainer("Hello from MainViewModel via child service MessageContainer!"));
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

    string IMessageProvider.GetMessage() => "Hello from MainViewModel via direct IMessageProvider!";

    object? IServiceProvider.GetService(Type serviceType)
    {
        if (serviceType == typeof(MessageContainer2))
            return new MessageContainer2("Hello from MainViewModel via IServiceProvider + MessageContainer2!");
        else
            return null;
    }
}

public record MenuItem(string Title, IConcreteChildRoutePart<MainViewModel>? ChildRoutePart);
