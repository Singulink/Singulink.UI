using System.Diagnostics.CodeAnalysis;
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
    /// Gets a value indicating whether the dialog view model was created by the navigator (see
    /// <see cref="IDialogPresenter.CreateDialogViewModel{TViewModel}(object?[])"/>) rather than constructed by the caller.
    /// </summary>
    internal bool CreatedByNavigator { get; init; }

    /// <summary>
    /// Gets the parent dialog navigator the view model was created from, or <see langword="null"/> if it was created from the root navigator.
    /// </summary>
    internal DialogNavigatorCore? CreatedByParent { get; init; }

    /// <summary>
    /// Gets the view models that supplied scoped services to the dialog view model's constructor. All of them must still be active when the dialog is shown.
    /// </summary>
    internal IReadOnlyList<object> ServiceProviders { get; init; } = [];

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

    /// <inheritdoc/>
    public IServiceProvider RootServices => _navigator.RootServices;

    /// <inheritdoc/>
    public bool CanShowDialog => _navigator.CanShowChildDialog(this);

    /// <inheritdoc cref="IDialogPresenter.CreateDialogViewModel{TViewModel}(object?[])"/>
    public TViewModel CreateDialogViewModel<[DynamicallyAccessedMembers(DAM.AllCtors)] TViewModel>(params object?[] explicitArgs)
        where TViewModel : class, IDialogViewModel
    {
        return (TViewModel)_navigator.CreateDialogViewModel(this, typeof(TViewModel), explicitArgs);
    }

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
