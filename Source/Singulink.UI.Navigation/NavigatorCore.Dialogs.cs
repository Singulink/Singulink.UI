using Singulink.UI.Navigation.InternalServices;
using Singulink.UI.Tasks;

namespace Singulink.UI.Navigation;

/// <content>
/// Provides dialog orchestration for the navigator.
/// </content>
partial class NavigatorCore
{
    /// <inheritdoc cref="IDialogPresenter.ShowDialogAsync(IDialogViewModel)"/>
    public Task ShowDialogAsync(IDialogViewModel viewModel) => ShowDialogAsync(null, viewModel);

    /// <inheritdoc cref="IDialogPresenter.ShowDialogAsync{TResult}(IDialogViewModel{TResult})"/>
    public async Task<TResult> ShowDialogAsync<TResult>(IDialogViewModel<TResult> viewModel)
    {
        await ShowDialogAsync(null, viewModel);
        return viewModel.Result;
    }

    /// <summary>
    /// Wires up a newly created framework-specific dialog object with the specified view model. The implementation should attach the view model as the
    /// dialog's data context, hook up any framework-specific bindings/event handlers, and produce a task runner used to run dialog operations.
    /// </summary>
    /// <param name="dialog">The framework-specific dialog object that was created by the registered dialog activator.</param>
    /// <param name="viewModel">The dialog view model to associate with the dialog.</param>
    /// <param name="taskRunner">The task runner to use for running dialog operations. Typically configured to disable the dialog's interactivity while
    /// busy.</param>
    protected abstract void WireDialog(object dialog, IDialogViewModel viewModel, out ITaskRunner taskRunner);

    /// <summary>
    /// Starts showing the specified framework dialog object. This is invoked in a fire-and-forget manner; awaiting completion of the dialog is handled by
    /// the core orchestration via the dialog's <see cref="TaskCompletionSource"/>.
    /// </summary>
    /// <param name="dialog">The framework-specific dialog object that was wired by <see cref="WireDialog"/>.</param>
    protected abstract void StartShowingDialog(object dialog);

    /// <summary>
    /// Hides the specified framework dialog object.
    /// </summary>
    /// <param name="dialog">The framework-specific dialog object that was wired by <see cref="WireDialog"/>.</param>
    protected abstract void HideDialog(object dialog);

    /// <summary>
    /// Returns the dialog navigator and view model for the top showing dialog, or <see langword="null"/> if no dialog is currently showing.
    /// </summary>
    protected (DialogNavigatorCore Navigator, IDialogViewModel ViewModel)? TryGetTopDialog()
    {
        if (_dialogStack.TryPeek(out var entry))
            return (entry.Navigator, entry.ViewModel);

        return null;
    }

    internal async Task ShowDialogAsync(DialogNavigatorCore? requestingParent, IDialogViewModel viewModel)
    {
        EnsureThreadAccess();

        if (_blockDialogs)
            throw new InvalidOperationException("Show dialog requested at an invalid time while showing dialogs is blocked.");

        EnsureDialogIsTopDialog(requestingParent);
        CloseLightDismissPopups();

        if (MixinManager.GetNavigator(viewModel) is not DialogNavigatorCore dialogNavigator)
        {
            object dialog = CreateDialog(viewModel.GetType());
            WireDialog(dialog, viewModel, out var taskRunner);
            dialogNavigator = new DialogNavigatorCore(this, dialog, taskRunner);
            MixinManager.SetNavigator(viewModel, dialogNavigator);
        }
        else if (dialogNavigator.RootNavigator != this)
        {
            throw new InvalidOperationException("The dialog view model is associated with a different root navigator instance.");
        }

        var tcs = new TaskCompletionSource();

        using (new PropertyChangedNotifier(this))
        {
            _dialogStack.Push(new DialogStackEntry(dialogNavigator, viewModel, tcs));

            if (requestingParent is not null)
                HideDialog(requestingParent.Dialog);

            StartShowingDialog(dialogNavigator.Dialog);
        }

        dialogNavigator.TaskRunner.RunAsBusyAndForget(viewModel.OnDialogShownAsync());
        await tcs.Task;

        void EnsureDialogIsTopDialog(DialogNavigatorCore? requestingParent)
        {
            var parentNavigator = _dialogStack.TryPeek(out var parentEntry) ? parentEntry.Navigator : null;

            if (requestingParent != parentNavigator)
            {
                if (requestingParent is null)
                    throw new InvalidOperationException("Another dialog is currently showing. Child dialogs must be shown using the dialog navigator of the parent dialog.");
                else
                    throw new InvalidOperationException("Dialog cannot show a child dialog because it is not the currently top showing dialog.");
            }
        }
    }

    internal void CloseDialog(DialogNavigatorCore dialogNavigator)
    {
        EnsureThreadAccess();
        CloseLightDismissPopups();

        if (!_dialogStack.TryPeek(out var entry) || entry.Navigator != dialogNavigator)
            throw new InvalidOperationException("Dialog is not currently the top showing dialog.");

        using (new PropertyChangedNotifier(this))
        {
            _dialogStack.Pop();
            HideDialog(dialogNavigator.Dialog);
        }

        if (_dialogStack.TryPeek(out var parentEntry))
            StartShowingDialog(parentEntry.Navigator.Dialog);

        entry.Tcs.SetResult();
    }

    /// <summary>
    /// Tries to dismiss the top-most dialog if it is dismissible and its task runner is not currently busy.
    /// </summary>
    private void TryDismissTopDialog()
    {
        if (_dialogStack.TryPeek(out var entry) &&
            entry.ViewModel is IDismissibleDialogViewModel dismissibleViewModel &&
            !entry.Navigator.TaskRunner.IsBusy)
        {
            entry.Navigator.TaskRunner.RunAsBusyAndForget(dismissibleViewModel.OnDismissRequestedAsync());
        }
    }

    private object CreateDialog(Type viewModelType)
    {
        if (!_viewModelTypeToDialogActivator.TryGetValue(viewModelType, out var activator))
            throw new KeyNotFoundException($"No dialog registered for view model of type '{viewModelType}'.");

        return activator.Invoke();
    }

    private readonly record struct DialogStackEntry(DialogNavigatorCore Navigator, IDialogViewModel ViewModel, TaskCompletionSource Tcs);
}
