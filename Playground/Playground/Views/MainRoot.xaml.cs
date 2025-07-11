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

    public ViewNavigator CreateChildViewNavigator() => ViewNavigator.Create(NavRoot);
}
