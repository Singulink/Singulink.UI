namespace Singulink.UI.Navigation;

internal class DialogNavigator(Navigator navigator, ContentDialog dialog) : IDialogNavigator
{
    /// <inheritdoc cref="IDialogNavigatorBase.ShowDialogAsync{TViewModel}(Func{IDialogNavigator, TViewModel}, out TViewModel)"/>"
    public Task ShowDialogAsync<TViewModel>(Func<IDialogNavigator, TViewModel> getModelFunc, out TViewModel viewModel)
    {
        var dialogTask = navigator.ShowDialogAsync(dialog, getModelFunc, out viewModel);
        return ShowDialogAsync(dialogTask);
    }

    /// <inheritdoc cref="IDialogNavigator.Close"/>
    public void Close() => navigator.CloseDialog(dialog);

    private async Task ShowDialogAsync(Task dialogTask) => await dialogTask;
}
