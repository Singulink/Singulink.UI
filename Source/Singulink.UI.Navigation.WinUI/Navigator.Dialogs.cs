using Singulink.UI.Navigation.InternalServices;

namespace Singulink.UI.Navigation.WinUI;

/// <content>
/// Provides dialog related implementations for the navigator.
/// </content>
partial class Navigator
{
    /// <inheritdoc cref="IDialogNavigatorBase.ShowDialogAsync{TViewModel}(TViewModel)"/>
    public Task ShowDialogAsync<TViewModel>(TViewModel viewModel)
        where TViewModel : class, IDialogViewModel
    {
        return ShowDialogAsync(null, viewModel);
    }

    internal async Task ShowDialogAsync<TViewModel>(ContentDialog? requestingParentDialog, TViewModel viewModel)
        where TViewModel : class, IDialogViewModel
    {
        EnsureThreadAccess();

        if (_blockDialogs)
            throw new InvalidOperationException("Show dialog requested at an invalid time while dialogs are blocked.");

        EnsureDialogIsTopDialog(requestingParentDialog);
        CloseLightDismissPopups();

        if (MixinManager.GetDialogNavigator(viewModel) is not DialogNavigator dn)
            MixinManager.SetDialogNavigator(viewModel, dn = new DialogNavigator(this, CreateDialogFor(viewModel)));
        else if (dn.Navigator != this)
            throw new InvalidOperationException("The dialog view model is associated with a different root navigator instance.");

        var tcs = new TaskCompletionSource();

        using (var notifier = new PropertyChangedNotifier(this, OnPropertyChanged))
        {
            _dialogTcsStack.Push((dn.Dialog, tcs));

            requestingParentDialog?.Hide();
            _ = dn.Dialog.ShowAsync();
        }

        await tcs.Task;

        void EnsureDialogIsTopDialog(ContentDialog? requestingParentDialog)
        {
            _dialogTcsStack.TryPeek(out var parentDialogInfo);
            var parentDialog = parentDialogInfo.Dialog;

            if (requestingParentDialog != parentDialog)
            {
                if (requestingParentDialog is null)
                {
                    const string message = "Another dialog is currently showing. Child dialogs must be shown using the dialog navigator of the parent dialog.";
                    throw new InvalidOperationException(message);
                }
                else
                {
                    throw new InvalidOperationException("Dialog cannot show a child dialog because it is not the currently top showing dialog.");
                }
            }
        }
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    internal async void CloseDialog(ContentDialog dialog)
    {
#pragma warning restore CS1998

        EnsureThreadAccess();
        CloseLightDismissPopups();

        if (!_dialogTcsStack.TryPeek(out var dialogInfo) || dialogInfo.Dialog != dialog)
            throw new InvalidOperationException("Dialog is not currently the top showing dialog.");

        using var notifier = new PropertyChangedNotifier(this, OnPropertyChanged);
        _dialogTcsStack.Pop();

        dialog.Hide();
        notifier.Update();

        if (_dialogTcsStack.TryPeek(out var parentDialogInfo))
            _ = parentDialogInfo.Dialog.ShowAsync();

        dialogInfo.Tcs.SetResult();
    }

    private ContentDialog CreateDialogFor<TViewModel>(TViewModel viewModel)
    {
        if (!_viewModelTypeToDialogActivator.TryGetValue(typeof(TViewModel), out var ctorFunc))
            throw new KeyNotFoundException($"No dialog registered for view model of type '{typeof(TViewModel)}'.");

        var dialog = ctorFunc.Invoke();
        dialog.DataContext = viewModel;
        dialog.XamlRoot = _viewNavigator.NavigationControl.XamlRoot ?? throw new InvalidOperationException("XamlRoot is not available");

        dialog.PrimaryButtonClick += OnPrimaryDialogButtonClick;
        dialog.SecondaryButtonClick += OnSecondaryDialogButtonClick;
        dialog.CloseButtonClick += OnCloseDialogButtonClick;
        dialog.Closing += OnDialogClosing;

        return dialog;

        void OnPrimaryDialogButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (_dialogTcsStack.TryPeek(out var topDialogInfo) && topDialogInfo.Dialog == sender)
            {
                args.Cancel = true;
                topDialogInfo.Dialog.PrimaryButtonCommand?.Execute(topDialogInfo.Dialog.PrimaryButtonCommandParameter);
            }
        }

        void OnSecondaryDialogButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (_dialogTcsStack.TryPeek(out var topDialogInfo) && topDialogInfo.Dialog == sender)
            {
                args.Cancel = true;
                topDialogInfo.Dialog.SecondaryButtonCommand?.Execute(topDialogInfo.Dialog.SecondaryButtonCommandParameter);
            }
        }

        void OnCloseDialogButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (_dialogTcsStack.TryPeek(out var topDialogInfo) && topDialogInfo.Dialog == sender)
            {
                args.Cancel = true;
                topDialogInfo.Dialog.CloseButtonCommand?.Execute(topDialogInfo.Dialog.CloseButtonCommandParameter);
            }
        }

        async void OnDialogClosing(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
            if (_dialogTcsStack.TryPeek(out var topDialogInfo) && topDialogInfo.Dialog == sender)
            {
                args.Cancel = true;

                if (sender.DataContext is IDismissableDialogViewModel dismissableViewModel)
                {
                    // Yield to prevent reentrant Hide() calls if the dismissable view model calls Close() in OnDismissRequested
                    // otherwise the dialog will not hide.

                    await Task.Yield();

                    // Make sure we are still the top dialog and another event didn't close the dialog after the yield.

                    if (_dialogTcsStack.TryPeek(out topDialogInfo) && topDialogInfo.Dialog == sender)
                        dismissableViewModel.OnDismissRequested();
                }
            }
        }
    }
}
