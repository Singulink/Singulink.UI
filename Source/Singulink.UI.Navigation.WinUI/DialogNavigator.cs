namespace Singulink.UI.Navigation;

internal class DialogNavigator(Navigator navigator, ContentDialog dialog) : IDialogNavigator
{
    public ContentDialog Dialog => dialog;

    /// <inheritdoc cref="IDialogNavigatorBase.ShowDialogAsync{TViewModel}(TViewModel, out IDialogNavigator)"/>
    public Task ShowDialogAsync<TViewModel>(TViewModel viewModel, out IDialogNavigator dialogNavigator) where TViewModel : class
    {
        var task = navigator.ShowDialogAsync(dialog, viewModel, out dialogNavigator);
        return ShowDialogAsync(task);
    }

    /// <inheritdoc cref="IDialogNavigatorBase.ShowDialogAsync{TViewModel}(Func{IDialogNavigator, TViewModel}, out TViewModel)"/>"
    public Task ShowDialogAsync<TViewModel>(Func<IDialogNavigator, TViewModel> getModelFunc, out TViewModel viewModel)
        where TViewModel : class
    {
        var task = navigator.ShowDialogAsync(dialog, getModelFunc, out viewModel);
        return ShowDialogAsync(task);
    }

    /// <inheritdoc cref="IDialogNavigator.Close"/>
    public void Close() => navigator.CloseDialog(dialog);

    private async Task ShowDialogAsync(Task dialogTask) => await dialogTask;
}
