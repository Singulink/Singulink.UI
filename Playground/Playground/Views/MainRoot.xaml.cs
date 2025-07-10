using Playground.ViewModels;
using Singulink.UI.Navigation.WinUI;

namespace Playground.Views;

public sealed partial class MainRoot : UserControl, IRoutedView<MainViewModel>, IParentView
{
    public MainViewModel Model { get; } = new();

    public MainRoot()
    {
        InitializeComponent();
    }

    private async void OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        await Model.Navigator.GoBackAsync();
    }

    private async void OnNavViewSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        string route = args.SelectedItemContainer.Tag.ToString();
        System.Diagnostics.Debug.Assert(route is not null, "Selected item tag should not be null.");

        await Model.Navigator.NavigateAsync(route);
    }

    public ViewNavigator CreateNestedViewNavigator() => ViewNavigator.Create(NavRoot);
}
