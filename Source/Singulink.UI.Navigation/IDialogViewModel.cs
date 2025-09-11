namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a view model that can be used in a dialog.
/// </summary>
public interface IDialogViewModel
{
    /// <summary>
    /// Invoked when the dialog is shown.
    /// </summary>
    /// <remarks>
    /// This method can be used to perform any initialization or setup required when the dialog is shown and can show nested dialogs or close the current
    /// dialog. This method is not invoked again if the dialog is restored when a nested dialog is closed.
    /// </remarks>
    public Task OnDialogShownAsync() => Task.CompletedTask;
}
