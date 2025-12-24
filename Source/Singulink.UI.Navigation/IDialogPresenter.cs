namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a presenter that can show dialogs. Implemented by both <see cref="INavigator"/> and <see cref="IDialogNavigator"/> (to show top-level and child
/// dialogs, respectively).
/// </summary>
public interface IDialogPresenter
{
    // TODO: Consider options for overloads that allow you to show a dialog with a parameterless constructor,
    // e.g. ShowDialogAsync<TViewModel>() where TViewModel : IDialogViewModel, new()

    /// <summary>
    /// Shows a dialog with the specified view model and returns a task that completes when the dialog closes.
    /// </summary>
    /// <param name="viewModel">The view model for the dialog.</param>
    public Task ShowDialogAsync(IDialogViewModel viewModel);

    /// <summary>
    /// Shows a dialog with the specified view model and returns a task that completes with the dialog result when the dialog closes.
    /// </summary>
    /// <param name="viewModel">The view model for the dialog.</param>
    public Task<TResult> ShowDialogAsync<TResult>(IDialogViewModel<TResult> viewModel);
}
