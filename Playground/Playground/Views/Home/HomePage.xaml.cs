using Playground.ViewModels.Home;
using Singulink.UI.Navigation;

namespace Playground.Views.Home;

public sealed partial class HomePage : Page, IRoutedView<HomeViewModel>
{
    public HomeViewModel Model { get; } = new();

    public HomePage()
    {
        InitializeComponent();
    }
}
