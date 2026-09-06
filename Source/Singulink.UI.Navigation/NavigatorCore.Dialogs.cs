using System.Diagnostics.CodeAnalysis;
using Singulink.UI.Navigation.InternalServices;
using Singulink.UI.Tasks;

namespace Singulink.UI.Navigation;

/// <content>
/// Provides dialog orchestration for the navigator.
/// </content>
partial class NavigatorCore
{
    /// <inheritdoc cref="IDialogPresenter.CreateDialogViewModel{TViewModel}(object?[])"/>
    public TViewModel CreateDialogViewModel<[DynamicallyAccessedMembers(DAM.AllCtors)] TViewModel>(params object?[] explicitArgs)
        where TViewModel : class, IDialogViewModel
    {
        return (TViewModel)CreateDialogViewModel(null, typeof(TViewModel), explicitArgs);
    }

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
    /// Captures the current focus state before a child dialog is shown so it can be restored to the parent dialog when the child closes. The returned
    /// object is opaque to the navigator and is passed back to <see cref="RestoreDialogFocusState"/>. The default implementation returns
    /// <see langword="null"/>.
    /// </summary>
    protected virtual object? CaptureDialogFocusState() => null;

    /// <summary>
    /// Restores focus into the specified framework dialog object after a child dialog closes. Implementations should defer the restoration until after any
    /// focus handling the framework performs when a dialog closes, and should ensure focus ends up inside the dialog even if <paramref name="focusState"/>
    /// is <see langword="null"/> or no longer applicable. The default implementation does nothing.
    /// </summary>
    /// <param name="dialog">The framework-specific dialog object that is now the top showing dialog.</param>
    /// <param name="focusState">The focus state captured by <see cref="CaptureDialogFocusState"/> when the child dialog was shown.</param>
    protected virtual void RestoreDialogFocusState(object dialog, object? focusState) { }

    /// <summary>
    /// Called when a layer (the root view or a dialog) becomes covered by a child dialog, or is uncovered again when the child dialog closes.
    /// Implementations can use this to adjust how the layer is presented while it is covered (e.g. to avoid showing it as disabled underneath the child
    /// dialog while it is busy, since the child dialog is what blocks interaction with it). The default implementation does nothing.
    /// </summary>
    /// <param name="dialog">The framework-specific dialog object that was covered or uncovered, or <see langword="null"/> for the root view.</param>
    /// <param name="isCovered"><see langword="true"/> if the layer is now covered by a child dialog; otherwise <see langword="false"/>.</param>
    protected virtual void OnLayerCoveredChanged(object? dialog, bool isCovered) { }

    /// <summary>
    /// Returns the dialog navigator and view model for the top showing dialog, or <see langword="null"/> if no dialog is currently showing.
    /// </summary>
    protected (DialogNavigatorCore Navigator, IDialogViewModel ViewModel)? TryGetTopDialog()
    {
        if (_dialogStack.TryPeek(out var entry))
            return (entry.Navigator, entry.ViewModel);

        return null;
    }

    internal IDialogViewModel CreateDialogViewModel(
        DialogNavigatorCore? requestingParent,
        [DynamicallyAccessedMembers(DAM.AllCtors)] Type viewModelType,
        object?[] explicitArgs)
    {
        EnsureThreadAccess();

        if (_blockDialogs)
            throw new InvalidOperationException("Create dialog view model requested at an invalid time while showing dialogs is blocked.");

        EnsureDialogIsTopDialog(requestingParent);

        var activationInfo = DialogActivationInfo.Get(viewModelType);
        var providers = new List<object>();
        object?[] args = activationInfo.ResolveArguments(explicitArgs, serviceType => ResolveScopedDialogService(requestingParent, serviceType, providers));

        // The navigator (and with it the dialog's task runner) must exist before the constructor runs so the view model can use it there, which means
        // the framework dialog object is created and wired to the still-uninitialized view model. Views only read their data context once shown.
        var viewModel = activationInfo.AllocateUninitialized();
        object dialog = CreateDialog(viewModelType);
        WireDialog(dialog, viewModel, out var taskRunner);

        var dialogNavigator = new DialogNavigatorCore(this, dialog, taskRunner) {
            CreatedByNavigator = true,
            CreatedByParent = requestingParent,
            ServiceProviders = providers,
        };

        MixinManager.SetNavigator(viewModel, dialogNavigator);
        activationInfo.InvokeConstructor(viewModel, args);

        return viewModel;
    }

    /// <summary>
    /// Resolves a service for a dialog view model being created from the specified presenter: child services of the showing dialogs (top first), then of
    /// the active route's view models (leaf first), then root services. View models that supplied a service are added to <paramref name="providers"/>.
    /// </summary>
    private object? ResolveScopedDialogService(DialogNavigatorCore? requestingParent, Type serviceType, List<object> providers)
    {
        if (requestingParent is not null)
        {
            foreach (var entry in _dialogStack)
            {
                if (GetProvidedService(entry.ViewModel, serviceType) is { } service)
                {
                    providers.Add(entry.ViewModel);
                    return service;
                }
            }
        }

        if (CurrentRouteCore is { } currentRoute)
        {
            for (int i = currentRoute.Items.Count - 1; i >= 0; i--)
            {
                if (currentRoute.Items[i].ViewModel is { } routedViewModel && GetProvidedService(routedViewModel, serviceType) is { } service)
                {
                    providers.Add(routedViewModel);
                    return service;
                }
            }
        }

        return RootServices.GetService(serviceType);

        static object? GetProvidedService(object viewModel, Type serviceType)
        {
            object? service = MixinManager.GetChildService(viewModel, serviceType) ?? (viewModel as IServiceProvider)?.GetService(serviceType);

            if (service is not null && !service.GetType().IsAssignableTo(serviceType))
            {
                throw new InvalidOperationException(
                    $"View model type '{viewModel.GetType()}' returned a service type '{service.GetType()}' which is not assignable to requested service type '{serviceType}'.");
            }

            return service;
        }
    }

    /// <summary>
    /// Verifies that a navigator-created dialog view model is being shown by the presenter that created it and that every view model that supplied it a
    /// scoped service is still active.
    /// </summary>
    private void EnsureCreatedDialogCanBeShown(DialogNavigatorCore dialogNavigator, DialogNavigatorCore? requestingParent)
    {
        if (!dialogNavigator.CreatedByNavigator)
            return;

        if (dialogNavigator.CreatedByParent != requestingParent)
        {
            throw new InvalidOperationException(
                "The dialog view model was created by a different presenter than the one showing it. Dialog view models created with CreateDialogViewModel() " +
                "can only be shown by the presenter that created them, since their services were resolved in that presenter's scope.");
        }

        foreach (object provider in dialogNavigator.ServiceProviders)
        {
            bool active = CurrentRouteCore?.Items.Any(item => ReferenceEquals(item.ViewModel, provider)) is true ||
                _dialogStack.Any(entry => ReferenceEquals(entry.ViewModel, provider));

            if (!active)
            {
                throw new InvalidOperationException(
                    $"The dialog view model received a service from a view model of type '{provider.GetType()}' that is no longer active. Dialog view models " +
                    "created with CreateDialogViewModel() must be shown while the view models that supplied their services are still active; create them " +
                    "immediately before showing them.");
            }
        }
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
        else
        {
            EnsureCreatedDialogCanBeShown(dialogNavigator, requestingParent);
        }

        var tcs = new TaskCompletionSource();

        // The parent dialog stays showing underneath the child (the framework is responsible for dimming it and blocking its input), so capture where focus
        // is inside the parent before the child takes it so it can be restored when the child closes.
        object? parentFocusState = requestingParent is not null ? CaptureDialogFocusState() : null;

        using (new PropertyChangedNotifier(this))
        {
            _dialogStack.Push(new DialogStackEntry(dialogNavigator, viewModel, tcs, parentFocusState));
            OnLayerCoveredChanged(requestingParent?.Dialog, isCovered: true);
            StartShowingDialog(dialogNavigator.Dialog);
        }

        dialogNavigator.TaskRunner.RunAsBusyAndForget(viewModel.OnDialogShownAsync());
        await tcs.Task;
    }

    private void EnsureDialogIsTopDialog(DialogNavigatorCore? requestingParent)
    {
        var parentNavigator = _dialogStack.TryPeek(out var parentEntry) ? parentEntry.Navigator : null;

        if (requestingParent != parentNavigator)
        {
            if (requestingParent is null)
                throw new InvalidOperationException("Another dialog is currently showing. Child dialogs must be shown or created using the dialog navigator of the parent dialog.");
            else
                throw new InvalidOperationException("Dialog cannot show or create a child dialog because it is not the currently top showing dialog.");
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
        {
            OnLayerCoveredChanged(parentEntry.Navigator.Dialog, isCovered: false);
            RestoreDialogFocusState(parentEntry.Navigator.Dialog, entry.ParentFocusState);
        }
        else
        {
            OnLayerCoveredChanged(null, isCovered: false);
        }

        entry.Tcs.SetResult();
    }

    /// <summary>
    /// Tries to dismiss the top-most dialog if it is dismissible and its task runner is not currently busy.
    /// </summary>
    protected void TryDismissTopDialog()
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
        if (_viewModelTypeToDialogActivator.TryGetValue(viewModelType, out var activator))
            return activator.Invoke();

        return CreateDefaultDialog(viewModelType) ??
            throw new KeyNotFoundException($"No dialog registered for view model of type '{viewModelType}'.");
    }

    /// <summary>
    /// Creates a framework dialog object for a dialog view model type that has no registered dialog mapping, or returns <see langword="null"/> if no
    /// default is available (in which case showing the dialog throws). The default implementation returns <see langword="null"/>.
    /// </summary>
    /// <param name="viewModelType">The dialog view model type.</param>
    protected virtual object? CreateDefaultDialog(Type viewModelType) => null;

    internal bool CanShowChildDialog(DialogNavigatorCore dialogNavigator)
    {
        EnsureThreadAccess();
        return !_blockDialogs && _dialogStack.TryPeek(out var entry) && entry.Navigator == dialogNavigator;
    }

    private readonly record struct DialogStackEntry(DialogNavigatorCore Navigator, IDialogViewModel ViewModel, TaskCompletionSource Tcs, object? ParentFocusState);
}
