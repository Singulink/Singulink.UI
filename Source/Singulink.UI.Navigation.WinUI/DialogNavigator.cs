using Singulink.UI.Tasks;

namespace Singulink.UI.Navigation.WinUI;

internal sealed class DialogNavigator(Navigator navigator, ContentDialog dialog) : IDialogNavigator
{
    internal Navigator RootNavigator => navigator;

    internal ContentDialog Dialog => dialog;

    public ITaskRunner TaskRunner => field ??= new TaskRunner(busy => dialog.IsEnabled = !busy);

    /// <inheritdoc cref="IDialogPresenter.ShowDialogAsync(IDialogViewModel)"/>
    public async Task ShowDialogAsync(IDialogViewModel viewModel) => await navigator.ShowDialogAsync(dialog, viewModel);

    /// <inheritdoc cref="IDialogPresenter.ShowDialogAsync{TResult}(IDialogViewModel{TResult})"/>
    public async Task<TResult> ShowDialogAsync<TResult>(IDialogViewModel<TResult> viewModel)
    {
        await navigator.ShowDialogAsync(dialog, viewModel);
        return viewModel.Result;
    }

    /// <inheritdoc cref="IDialogNavigator.Close"/>
    public void Close() => navigator.CloseDialog(dialog);
}
