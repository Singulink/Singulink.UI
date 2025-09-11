using Playground.ViewModels;

namespace Playground.Views;

public sealed partial class LoginRoot : UserControl
{
    public LoginViewModel Model => (LoginViewModel)DataContext;

    public LoginRoot()
    {
        InitializeComponent();
    }
}
