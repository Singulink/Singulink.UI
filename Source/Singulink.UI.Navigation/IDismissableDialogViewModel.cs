namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a dialog view model that can handle the escape key being pressed, i.e. by closing the dialog.
/// </summary>
public interface IDismissableDialogViewModel
{
    /// <summary>
    /// Called when the escape key is pressed while the dialog is showing.
    /// </summary>
    public void OnDismissRequested();
}
