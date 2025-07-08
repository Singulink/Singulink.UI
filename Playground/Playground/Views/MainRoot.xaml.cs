using Playground.ViewModels;
using Singulink.UI.Navigation;
using Singulink.UI.Navigation.WinUI;

namespace Playground.Views;

public sealed partial class MainRoot : Page, IRoutedView<MainViewModel>, IParentView
{
    public MainViewModel Model { get; } = new();

    public MainRoot()
    {
        InitializeComponent();
    }

    private async void OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        await Model.Navigator.GoBackAsync(true);
    }

    private async void OnNavViewSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        string route = args.SelectedItemContainer.Tag.ToString();
        System.Diagnostics.Debug.Assert(route is not null, "Selected item tag should not be null.");

        await Model.Navigator.NavigateAsync(route);
        Model.Navigator.ClearHistory();
    }

    public IViewNavigator CreateNestedViewNavigator() => ViewNavigator.Create(NavRoot);
}
