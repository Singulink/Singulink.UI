using CommunityToolkit.Mvvm.ComponentModel;
using Singulink.UI.Navigation;
using Singulink.UI.Tasks;

namespace Playground.ViewModels;

public partial class MainViewModel : ObservableObject, IRoutedViewModel
{
    public INavigator Navigator => this.GetNavigator();

    private ITaskRunner TaskRunner => Navigator.TaskRunner;

    public IReadOnlyList<MenuItem> MainMenuItems { get; } = [
        new("Home", Routes.Main.HomeChild),
        new("Dialog Test", Routes.Main.DialogTestChild),
        new("Parameters Test", Routes.Main.ParamsTestChild)
    ];

    public IReadOnlyList<MenuItem> FooterMenuItems { get; } = [
        new("Logout", null),
    ];

    [ObservableProperty]
    public partial MenuItem SelectedMenuItem { get; set; }

    public MainViewModel()
    {
        SelectedMenuItem = MainMenuItems[0];
    }

    public async Task OnNavigatedToAsync(NavigationArgs args)
    {
        // Simulate first-time loading

        if (!args.AlreadyNavigatedTo)
            await Task.Delay(1000);

        // If there is no child navigation (because a view routed directly to "/" instead of "/Home", for example),
        // navigate to the selected menu item (which is Home by default).

        if (!args.HasChildNavigation)
            await Navigator.NavigatePartialAsync(SelectedMenuItem.ChildRoutePart!);
    }

    public void BeginBackRequest() => TaskRunner.RunAndForget((Func<Task>)(async () => {
        await Navigator.GoBackAsync();
        SelectedMenuItem = MainMenuItems.First((Func<MenuItem, bool>)(mi => Navigator.CurrentRouteStartsWith((IConcreteRootRoutePart<MainViewModel>)Routes.MainRoot, mi.ChildRoutePart!)));
    }));

    partial void OnSelectedMenuItemChanged(MenuItem value)
    {
        if (!this.HasNavigated() || Navigator.CurrentRoute?.Parts.Last() == value.ChildRoutePart)
            return;

        TaskRunner.RunAndForget(async () => {
            if (value.ChildRoutePart is not null)
            {
                await Navigator.NavigatePartialAsync(value.ChildRoutePart);
                return;
            }

            await Task.Delay(500); // Simulate logout
            await Navigator.NavigateAsync(Routes.LoginRoot);
        });
    }
}

public record MenuItem(string Title, IConcreteChildRoutePart<MainViewModel>? ChildRoutePart);
