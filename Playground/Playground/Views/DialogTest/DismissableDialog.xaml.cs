using Playground.ViewModels.DialogTest;

namespace Playground.Views.DialogTest;

public sealed partial class DismissableDialog : ContentDialog
{
    public DismissableDialogViewModel Model => (DismissableDialogViewModel)DataContext;

    public DismissableDialog()
    {
        InitializeComponent();
    }
}
