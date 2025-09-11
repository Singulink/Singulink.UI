using Playground.ViewModels.Home;

namespace Playground.Views.Home;

public sealed partial class HomePage : UserControl
{
    public HomeViewModel Model => (HomeViewModel)DataContext;

    public HomePage()
    {
        InitializeComponent();
    }
}
