using Singulink.UI.Tasks;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a navigator for dialogs that can show child dialogs or close the current dialog.
/// </summary>
public interface IDialogNavigator : IDialogNavigatorBase
{
    /// <summary>
    /// Gets the task runner for this navigator.
    /// </summary>
    public ITaskRunner TaskRunner { get; }

    /// <summary>
    /// Closes the dialog this navigator is assigned to. Throws <see cref="InvalidOperationException"/> if the dialog is not currently the top showing dialog.
    /// </summary>
    public void Close();
}
