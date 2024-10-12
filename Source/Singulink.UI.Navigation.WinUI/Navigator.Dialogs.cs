using System.Runtime.CompilerServices;

namespace Singulink.UI.Navigation;

/// <content>
/// Provides dialog related implementations for the navigator.
/// </content>
public partial class Navigator
{
    private readonly ConditionalWeakTable<object, DialogNavigator> _vmToDialogNavigator = [];

    /// <inheritdoc cref="IDialogNavigatorBase.ShowDialogAsync{TViewModel}(TViewModel, out IDialogNavigator)"/>
    public Task ShowDialogAsync<TViewModel>(TViewModel viewModel, out IDialogNavigator dialogNavigator)
        where TViewModel : class
    {
        return ShowDialogAsync(null, viewModel, out dialogNavigator);
    }

    /// <inheritdoc cref="IDialogNavigatorBase.ShowDialogAsync{TViewModel}(Func{IDialogNavigator, TViewModel}, out TViewModel)"/>"
    public Task ShowDialogAsync<TViewModel>(Func<IDialogNavigator, TViewModel> createModelFunc, out TViewModel viewModel)
        where TViewModel : class
    {
        return ShowDialogAsync(null, createModelFunc, out viewModel);
    }

    internal Task ShowDialogAsync<TViewModel>(ContentDialog? requestingParentDialog, TViewModel viewModel, out IDialogNavigator dialogNavigator)
        where TViewModel : class
    {
        EnsureThreadAccess();
        EnsureCanShowDialog();
        EnsureDialogIsTopDialog(requestingParentDialog);

        var dn = _vmToDialogNavigator.GetValue(viewModel, vm => {
            var dialog = CreateDialogFor<TViewModel>();
            dialog.DataContext = vm;
            return new DialogNavigator(this, dialog);
        });

        dialogNavigator = dn;
        return ShowDialogAsync(dn.Dialog, requestingParentDialog);
    }

    internal Task ShowDialogAsync<TViewModel>(ContentDialog? requestingParentDialog, Func<IDialogNavigator, TViewModel> createModelFunc, out TViewModel viewModel)
        where TViewModel : class
    {
        EnsureThreadAccess();
        EnsureCanShowDialog();
        EnsureDialogIsTopDialog(requestingParentDialog);

        var dialog = CreateDialogFor<TViewModel>();
        var dialogNavigator = new DialogNavigator(this, dialog);
        dialog.DataContext = viewModel = createModelFunc(dialogNavigator);

        if (!_vmToDialogNavigator.TryAdd(viewModel, dialogNavigator))
        {
            const string message = "Function returned an existing view model instance instead of creating a new instance. " +
                "Either provide a function that creates a new instance or use a 'ShowDialogAsync()' overload that supports " +
                "passing in an existing view model instance instead.";

            throw new ArgumentException(message, nameof(createModelFunc));
        }

        return ShowDialogAsync(dialog, requestingParentDialog);
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    internal async void CloseDialog(ContentDialog dialog)
    {
#pragma warning restore CS1998

        EnsureThreadAccess();

        if (!_dialogInfoStack.TryPeek(out var dialogInfo) || dialogInfo.Dialog != dialog)
            throw new InvalidOperationException("Dialog is not currently the top showing dialog.");

        using var notifier = new PropertyChangedNotifier(this, OnPropertyChanged);
        _dialogInfoStack.Pop();

#if !WINDOWS
        // HACK: workaround for https://github.com/unoplatform/uno/issues/18609
        await Task.Yield();
#endif
        dialog.Hide();
        notifier.Update();
        dialogInfo.Tcs.SetResult();

        if (_dialogInfoStack.TryPeek(out var parentDialogInfo))
            _ = parentDialogInfo.Dialog.ShowAsync();
    }

    private void EnsureCanShowDialog()
    {
        if (_blockDialogs)
            throw new InvalidOperationException("Show dialog requested at an invalid time while dialogs are blocked.");
    }

    private void EnsureDialogIsTopDialog(ContentDialog? requestingParentDialog)
    {
        _dialogInfoStack.TryPeek(out var parentDialogInfo);
        var parentDialog = parentDialogInfo.Dialog;

        if (requestingParentDialog != parentDialog)
        {
            if (requestingParentDialog is null)
            {
                const string message = "Another dialog is currently showing. Nested dialogs must be shown using the dialog navigator of the parent dialog.";
                throw new InvalidOperationException(message);
            }
            else
            {
                throw new InvalidOperationException("Dialog cannot show a nested dialog because it is not the currently top showing dialog.");
            }
        }
    }

    private ContentDialog CreateDialogFor<TViewModel>()
    {
        if (!_vmTypeToDialogCtorFunc.TryGetValue(typeof(TViewModel), out var ctorFunc))
            throw new KeyNotFoundException($"No dialog registered for view model of type '{typeof(TViewModel)}'.");

        var dialog = ctorFunc.Invoke();
        dialog.XamlRoot = _rootViewNavigator.XamlRoot;

        dialog.PrimaryButtonClick += OnDialogButtonClick;
        dialog.SecondaryButtonClick += OnDialogButtonClick;
        dialog.CloseButtonClick += OnDialogButtonClick;
        dialog.Closing += OnDialogClosing;

        return dialog;

        void OnDialogButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (_dialogInfoStack.TryPeek(out var topDialogInfo) && topDialogInfo.Dialog == sender)
                args.Cancel = true;
        }

        async void OnDialogClosing(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
            if (_dialogInfoStack.TryPeek(out var topDialogInfo) && topDialogInfo.Dialog == sender)
            {
                args.Cancel = true;

                if (sender.DataContext is IDismissableDialogViewModel dismissableViewModel)
                {
                    // Yield to prevent reentrant Hide() calls if the dismissable view model calls Close() in OnDismissRequested
                    // otherwise the dialog will not hide.

                    await Task.Yield();

                    // Make sure we are still the top dialog and another event didn't close the dialog after the yield.

                    if (_dialogInfoStack.TryPeek(out topDialogInfo) && topDialogInfo.Dialog == sender)
                        dismissableViewModel.OnDismissRequested();
                }
            }
        }
    }

    private async Task ShowDialogAsync(ContentDialog dialog, ContentDialog? parentDialog)
    {
        var tcs = new TaskCompletionSource();

        using (var notifier = new PropertyChangedNotifier(this, OnPropertyChanged))
        {
            _dialogInfoStack.Push((dialog, tcs));

            parentDialog?.Hide();
            _ = dialog.ShowAsync();
        }

        await tcs.Task;
    }
}
