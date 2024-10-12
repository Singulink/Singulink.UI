namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a dialog view model that can handle close requests when the escape key is pressed or a user-initiated back navigation is triggered. If a dialog
/// view model does not implement this interface, the dialog will ignore these requests.
/// </summary>
public interface IDismissableDialogViewModel
{
    /// <summary>
    /// Invoked when the escape key is pressed or a user-initiated back navigation is triggered while the dialog is showing.
    /// </summary>
    public void OnDismissRequested();
}
