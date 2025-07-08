using Playground.ViewModels;
using Singulink.UI.Navigation;

namespace Playground.Views;

public sealed partial class LoginRoot : Page, IRoutedView<LoginViewModel>
{
    public LoginViewModel Model { get; } = new();

    public LoginRoot()
    {
        InitializeComponent();
    }
}
