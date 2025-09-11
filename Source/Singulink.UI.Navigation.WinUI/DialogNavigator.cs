using Singulink.UI.Tasks;

namespace Singulink.UI.Navigation.WinUI;

internal sealed class DialogNavigator(Navigator navigator, ContentDialog dialog) : IDialogNavigator
{
    internal Navigator RootNavigator => navigator;

    internal ContentDialog Dialog => dialog;

    public ITaskRunner TaskRunner => field ??= new TaskRunner(busy => dialog.IsEnabled = !busy);

    /// <inheritdoc cref="IDialogPresenter.ShowDialogAsync{TViewModel}(TViewModel)"/>
    public async Task ShowDialogAsync<TViewModel>(TViewModel viewModel)
        where TViewModel : class, IDialogViewModel
    {
        await navigator.ShowDialogAsync(dialog, viewModel);
    }

    /// <inheritdoc cref="IDialogNavigator.Close"/>
    public void Close() => navigator.CloseDialog(dialog);
}
