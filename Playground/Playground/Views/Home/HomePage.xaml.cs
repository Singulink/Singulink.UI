using Playground.ViewModels.Home;
using Singulink.UI.Navigation.WinUI;

namespace Playground.Views.Home;

public sealed partial class HomePage : UserControl, IRoutedView<HomeViewModel>
{
    public HomeViewModel Model { get; } = new();

    public HomePage()
    {
        InitializeComponent();
    }
}
