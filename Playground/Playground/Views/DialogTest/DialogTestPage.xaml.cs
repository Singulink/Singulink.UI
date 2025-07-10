using Playground.ViewModels.DialogTest;
using Singulink.UI.Navigation.WinUI;

namespace Playground.Views.DialogTest;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class DialogTestPage : UserControl, IRoutedView<DialogTestViewModel>
{
    public DialogTestViewModel Model { get; } = new();

    public DialogTestPage()
    {
        InitializeComponent();
    }
}
