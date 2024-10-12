namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a navigator that can show dialogs.
/// </summary>
public interface IDialogNavigatorBase
{
    /// <summary>
    /// Shows a dialog with the specified view model and returns a task that completes when the dialog closes.
    /// </summary>
    /// <param name="getModelFunc">A function that creates the view model for the dialog. The dialog navigator the function provides should be used to show
    /// nested dialogs or close the dialog.</param>
    /// <param name="viewModel">Outputs the view model for the dialog when the method returns.</param>
    public Task ShowDialogAsync<TViewModel>(Func<IDialogNavigator, TViewModel> getModelFunc, out TViewModel viewModel);
}
