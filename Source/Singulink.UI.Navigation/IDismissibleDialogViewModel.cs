namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a dialog view model that can handle close requests when the escape key is pressed or a system back request is received. If a dialog
/// view model does not implement this interface, the dialog will ignore these requests.
/// </summary>
public interface IDismissibleDialogViewModel : IDialogViewModel
{
    /// <summary>
    /// Invoked when the escape key is pressed or a system back navigation is requested while the dialog is showing.
    /// </summary>
    public Task OnDismissRequestedAsync();
}

/// <summary>
/// Represents a dialog view model that can handle close requests when the escape key is pressed or a system back request is received, and produces a result
/// when closed.
/// </summary>
public interface IDismissibleDialogViewModel<TResult> : IDialogViewModel<TResult>, IDismissibleDialogViewModel;
