using Playground.ViewModels.DialogTest;
using Singulink.UI.Navigation;

namespace Playground.Views.DialogTest;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class DialogTestPage : Page, IRoutedView<DialogTestViewModel>
{
    public DialogTestViewModel Model { get; } = new();

    public DialogTestPage()
    {
        InitializeComponent();
    }
}
