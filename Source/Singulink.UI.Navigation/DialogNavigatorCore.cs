using Singulink.UI.Tasks;

namespace Singulink.UI.Navigation;

/// <summary>
/// Provides the framework-agnostic implementation of <see cref="IDialogNavigator"/>. Holds the framework-specific dialog object and delegates dialog
/// orchestration back to the owning <see cref="NavigatorCore"/>.
/// </summary>
public sealed class DialogNavigatorCore : IDialogNavigator
{
    private readonly NavigatorCore _navigator;

    internal DialogNavigatorCore(NavigatorCore navigator, object dialog, ITaskRunner taskRunner)
    {
        _navigator = navigator;
        Dialog = dialog;
        TaskRunner = taskRunner;
    }

    /// <summary>
    /// Gets the navigator that owns this dialog navigator.
    /// </summary>
    public NavigatorCore RootNavigator => _navigator;

    /// <summary>
    /// Gets the framework-specific dialog object (e.g. a WinUI <c>ContentDialog</c>) associated with this dialog navigator.
    /// </summary>
    public object Dialog { get; }

    /// <inheritdoc/>
    public ITaskRunner TaskRunner { get; }

    /// <inheritdoc cref="IDialogPresenter.ShowDialogAsync(IDialogViewModel)"/>
    public Task ShowDialogAsync(IDialogViewModel viewModel) => _navigator.ShowDialogAsync(this, viewModel);

    /// <inheritdoc cref="IDialogPresenter.ShowDialogAsync{TResult}(IDialogViewModel{TResult})"/>
    public async Task<TResult> ShowDialogAsync<TResult>(IDialogViewModel<TResult> viewModel)
    {
        await _navigator.ShowDialogAsync(this, viewModel);
        return viewModel.Result;
    }

    /// <inheritdoc cref="IDialogNavigator.Close"/>
    public void Close() => _navigator.CloseDialog(this);
}
