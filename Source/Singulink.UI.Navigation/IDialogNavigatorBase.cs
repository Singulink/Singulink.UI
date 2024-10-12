namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a navigator that can show dialogs.
/// </summary>
public interface IDialogNavigatorBase
{
    /// <summary>
    /// Shows a dialog with the specified view model and returns a task that completes when the dialog closes.
    /// </summary>
    /// <param name="viewModel">The view model for the dialog.</param>
    /// <param name="dialogNavigator">Contains the dialog navigator for the dialog when the method returns.</param>
    /// <remarks>
    /// <para>
    /// This method is typically used to:
    /// </para>
    /// <list type="bullet">
    /// <item>Show the dialog for a view model that receives an <see cref="IDialogNavigator"/> instance through a mechanism other than a constructor parameter
    /// (like a property setter or method call, for example). The output <paramref name="dialogNavigator"/> should be provided to the view model prior to
    /// awaiting the returned task.</item>
    /// <item>Show a dialog where the caller of this method controls closing the dialog instead of the dialog itself, using the output <paramref
    /// name="dialogNavigator"/>. The caller should call <see cref="IDialogNavigator.Close"/> before awaiting the returned task (or pass it to something else
    /// that will call it when the dialog should close).</item>
    /// <item>Show a dialog that was previously shown again. The output <paramref name="dialogNavigator"/> can be ignored in this case, as it will be the same
    /// instance that was provided when the dialog was first shown.</item>
    /// </list>
    /// </remarks>
    public Task ShowDialogAsync<TViewModel>(TViewModel viewModel, out IDialogNavigator dialogNavigator) where TViewModel : class;

    /// <summary>
    /// Shows a dialog with the view model created by the specified function and returns a task that completes when the dialog closes.
    /// </summary>
    /// <param name="createModelFunc">A function that creates the view model for the dialog. The dialog navigator provided by the function can be used to show
    /// nested dialogs or close the dialog.</param>
    /// <param name="viewModel">Contains the view model for the dialog when the method returns.</param>
    /// <remarks>
    /// This method should only be used when showing a dialog for the first time when its view model has an <see cref="IDialogNavigator"/> constructor parameter
    /// that needs to be provided, which should be done in <paramref name="createModelFunc"/>. For all other cases, use the <see
    /// cref="ShowDialogAsync{TViewModel}(TViewModel, out IDialogNavigator)"/> overload instead.
    /// </remarks>
    public Task ShowDialogAsync<TViewModel>(Func<IDialogNavigator, TViewModel> createModelFunc, out TViewModel viewModel) where TViewModel : class;
}
