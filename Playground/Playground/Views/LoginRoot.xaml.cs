using Playground.ViewModels;
using Singulink.UI.Navigation.WinUI;

namespace Playground.Views;

public sealed partial class LoginRoot : UserControl, IRoutedView<LoginViewModel>
{
    public LoginViewModel Model { get; } = new();

    public LoginRoot()
    {
        InitializeComponent();
    }
}
