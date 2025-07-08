namespace Singulink.UI.Navigation.WinUI;

internal class DialogNavigator(Navigator navigator, ContentDialog dialog) : IDialogNavigator
{
    public ContentDialog Dialog => dialog;

    /// <inheritdoc cref="IDialogNavigatorBase.ShowDialogAsync{TViewModel}(TViewModel, out IDialogNavigator)"/>
    public Task ShowDialogAsync<TViewModel>(TViewModel viewModel, out IDialogNavigator dialogNavigator) where TViewModel : class
    {
        var task = navigator.ShowDialogAsync(dialog, viewModel, out dialogNavigator);
        return ShowDialogAsync(task);
    }

    /// <inheritdoc cref="IDialogNavigatorBase.ShowDialogAsync{TViewModel}(Func{IDialogNavigator, TViewModel})"/>
    public async Task<TViewModel> ShowDialogAsync<TViewModel>(Func<IDialogNavigator, TViewModel> createModelFunc) where TViewModel : class
    {
        await navigator.ShowDialogAsync(dialog, out var viewModel, createModelFunc);
        return viewModel;
    }

    /// <inheritdoc cref="IDialogNavigatorBase.ShowDialogAsync{TViewModel}(out TViewModel, Func{IDialogNavigator, TViewModel})"/>"
    public Task ShowDialogAsync<TViewModel>(out TViewModel viewModel, Func<IDialogNavigator, TViewModel> getModelFunc)
        where TViewModel : class
    {
        var task = navigator.ShowDialogAsync(dialog, out viewModel, getModelFunc);
        return ShowDialogAsync(task);
    }

    /// <inheritdoc cref="IDialogNavigator.Close"/>
    public void Close() => navigator.CloseDialog(dialog);

    private async Task ShowDialogAsync(Task dialogTask) => await dialogTask;
}
