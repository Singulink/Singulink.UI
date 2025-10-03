using Playground.ViewModels.DialogTest;

namespace Playground.Views.DialogTest;

public sealed partial class DismissibleDialog : ContentDialog
{
    public DismissibleDialogViewModel Model => (DismissibleDialogViewModel)DataContext;

    public DismissibleDialog()
    {
        InitializeComponent();
    }
}
