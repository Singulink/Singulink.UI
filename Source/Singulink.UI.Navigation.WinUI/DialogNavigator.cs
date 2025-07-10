using Singulink.UI.Tasks;

namespace Singulink.UI.Navigation.WinUI;

internal class DialogNavigator(Navigator navigator, ContentDialog dialog) : IDialogNavigator
{
    internal Navigator Navigator => navigator;

    public ContentDialog Dialog => dialog;

    public ITaskRunner TaskRunner { get; } = new TaskRunner(busy => dialog.IsEnabled = !busy);

    /// <inheritdoc cref="IDialogNavigatorBase.ShowDialogAsync{TViewModel}(TViewModel)"/>
    public async Task ShowDialogAsync<TViewModel>(TViewModel viewModel)
        where TViewModel : class, IDialogViewModel
    {
        await navigator.ShowDialogAsync(dialog, viewModel);
    }

    /// <inheritdoc cref="IDialogNavigator.Close"/>
    public void Close() => navigator.CloseDialog(dialog);
}
