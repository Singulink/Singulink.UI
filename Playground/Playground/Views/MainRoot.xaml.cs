using Playground.ViewModels;
using Singulink.UI.Navigation.WinUI;

namespace Playground.Views;

public sealed partial class MainRoot : UserControl, IParentView
{
    public MainViewModel Model => (MainViewModel)DataContext;

    public MainRoot()
    {
        InitializeComponent();
    }

    public ViewNavigator CreateChildViewNavigator() => ViewNavigator.Create(NavRoot);
}
