using Playground.ViewModels.DialogTest;
using Singulink.UI.Navigation.WinUI;

namespace Playground.Views.DialogTest;

public sealed partial class DialogTestPage : UserControl, IRoutedView<DialogTestViewModel>
{
    public DialogTestViewModel Model { get; } = new();

    public DialogTestPage()
    {
        InitializeComponent();
    }
}
