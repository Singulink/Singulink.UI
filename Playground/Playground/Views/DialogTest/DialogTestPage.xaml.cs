using Playground.ViewModels.DialogTest;

namespace Playground.Views.DialogTest;

public sealed partial class DialogTestPage : UserControl
{
    public DialogTestViewModel Model => (DialogTestViewModel)DataContext;

    public DialogTestPage()
    {
        InitializeComponent();
    }
}
