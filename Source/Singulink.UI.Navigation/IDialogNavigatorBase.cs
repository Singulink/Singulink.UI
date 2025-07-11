namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a navigator that can show dialogs. Implemented by both <see cref="INavigator"/> and <see cref="IDialogNavigator"/> (to show child dialogs).
/// </summary>
public interface IDialogNavigatorBase
{
    /// <summary>
    /// Shows a dialog with the specified view model and returns a task that completes when the dialog closes.
    /// </summary>
    /// <param name="viewModel">The view model for the dialog.</param>
    public Task ShowDialogAsync<TViewModel>(TViewModel viewModel)
        where TViewModel : class, IDialogViewModel;
}
