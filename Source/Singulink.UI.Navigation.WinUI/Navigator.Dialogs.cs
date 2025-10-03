using Singulink.UI.Navigation.InternalServices;

namespace Singulink.UI.Navigation.WinUI;

/// <content>
/// Provides dialog related implementations for the navigator.
/// </content>
partial class Navigator
{
    /// <inheritdoc cref="IDialogPresenter.ShowDialogAsync{TViewModel}(TViewModel)"/>
    public async Task ShowDialogAsync<TViewModel>(TViewModel viewModel) where TViewModel : class, IDialogViewModel
    {
        await ShowDialogAsync(null, viewModel);
    }

    internal async Task ShowDialogAsync<TViewModel>(ContentDialog? requestingParentDialog, TViewModel viewModel) where TViewModel : class, IDialogViewModel
    {
        EnsureThreadAccess();

        if (_blockDialogs)
            throw new InvalidOperationException("Show dialog requested at an invalid time while dialogs are blocked.");

        EnsureDialogIsTopDialog(requestingParentDialog);
        CloseLightDismissPopups();

        if (MixinManager.GetNavigator(viewModel) is not DialogNavigator dialogNavigator)
            MixinManager.SetNavigator(viewModel, dialogNavigator = new DialogNavigator(this, CreateDialogFor(viewModel)));
        else if (dialogNavigator.RootNavigator != this)
            throw new InvalidOperationException("The dialog view model is associated with a different root navigator instance.");

        var tcs = new TaskCompletionSource();

        using (new PropertyChangedNotifier(this))
        {
            _dialogStack.Push((dialogNavigator.Dialog, tcs));

            requestingParentDialog?.Hide();
            _ = dialogNavigator.Dialog.ShowAsync();
        }

        dialogNavigator.TaskRunner.RunAsBusyAndForget(viewModel.OnDialogShownAsync());
        await tcs.Task;

        void EnsureDialogIsTopDialog(ContentDialog? requestingParentDialog)
        {
            _dialogStack.TryPeek(out var parentDialogInfo);
            var parentDialog = parentDialogInfo.Dialog;

            if (requestingParentDialog != parentDialog)
            {
                if (requestingParentDialog is null)
                    throw new InvalidOperationException("Another dialog is currently showing. Child dialogs must be shown using the dialog navigator of the parent dialog.");
                else
                    throw new InvalidOperationException("Dialog cannot show a child dialog because it is not the currently top showing dialog.");
            }
        }
    }

    internal void CloseDialog(ContentDialog dialog)
    {
        EnsureThreadAccess();
        CloseLightDismissPopups();

        if (!_dialogStack.TryPeek(out var dialogInfo) || dialogInfo.Dialog != dialog)
            throw new InvalidOperationException("Dialog is not currently the top showing dialog.");

        using (new PropertyChangedNotifier(this))
        {
            _dialogStack.Pop();
            dialog.Hide();
        }

        if (_dialogStack.TryPeek(out var parentDialogInfo))
            _ = parentDialogInfo.Dialog.ShowAsync();

        dialogInfo.Tcs.SetResult();
    }

    private ContentDialog CreateDialogFor<TViewModel>(TViewModel viewModel)
        where TViewModel : class, IDialogViewModel
    {
        if (!_viewModelTypeToDialogActivator.TryGetValue(typeof(TViewModel), out var ctorFunc))
            throw new KeyNotFoundException($"No dialog registered for view model of type '{typeof(TViewModel)}'.");

        var dialog = ctorFunc.Invoke();
        dialog.DataContext = viewModel;
        dialog.XamlRoot = _viewNavigator.NavigationControl.XamlRoot ?? throw new InvalidOperationException("XamlRoot is not available");

        dialog.DataContextChanged += (_, e) => {
            if (e.NewValue != viewModel)
            {
                dialog.DataContext = viewModel;
                throw new InvalidOperationException("Navigator managed views cannot change their data context.");
            }
        };

        dialog.PrimaryButtonClick += OnPrimaryDialogButtonClick;
        dialog.SecondaryButtonClick += OnSecondaryDialogButtonClick;
        dialog.CloseButtonClick += OnCloseDialogButtonClick;
        dialog.Closing += OnDialogClosing;

        return dialog;

        void OnPrimaryDialogButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;

            if (_dialogStack.TryPeek(out var dialogInfo) && dialogInfo.Dialog == sender)
                dialogInfo.Dialog.PrimaryButtonCommand?.Execute(dialogInfo.Dialog.PrimaryButtonCommandParameter);
        }

        void OnSecondaryDialogButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;

            if (_dialogStack.TryPeek(out var dialogInfo) && dialogInfo.Dialog == sender)
                dialogInfo.Dialog.SecondaryButtonCommand?.Execute(dialogInfo.Dialog.SecondaryButtonCommandParameter);
        }

        void OnCloseDialogButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;

            if (_dialogStack.TryPeek(out var dialogInfo) && dialogInfo.Dialog == sender)
                dialogInfo.Dialog.CloseButtonCommand?.Execute(dialogInfo.Dialog.CloseButtonCommandParameter);
        }

        async void OnDialogClosing(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
            if (_dialogStack.TryPeek(out var dialogInfo) && dialogInfo.Dialog == sender)
            {
                args.Cancel = true;

                if (sender.DataContext is IDismissibleDialogViewModel dismissibleVm)
                {
                    // Yield to prevent reentrant Hide() calls if the dismissible view model calls Close() in OnDismissRequested
                    // otherwise the dialog will not hide.

                    await Task.Yield();

                    // Make sure we are still the top dialog and another event didn't close the dialog after the yield.

                    if (_dialogStack.TryPeek(out dialogInfo) && dialogInfo.Dialog == sender &&
                        MixinManager.GetNavigator(dismissibleVm) is { } navigator && !navigator.TaskRunner.IsBusy)
                    {
                        await navigator.TaskRunner.RunAsBusyAsync(dismissibleVm.OnDismissRequestedAsync());
                    }
                }
            }
        }
    }
}
