using System.ComponentModel;
using Microsoft.UI.Xaml.Data;
using Singulink.UI.Navigation.InternalServices;
using Windows.Security.Cryptography.Core;

namespace Singulink.UI.Navigation.WinUI;

/// <content>
/// Provides dialog related implementations for the navigator.
/// </content>
partial class Navigator : IDialogPresenter
{
    /// <inheritdoc cref="IDialogPresenter.ShowDialogAsync(IDialogViewModel)"/>
    public async Task ShowDialogAsync(IDialogViewModel viewModel)
    {
        await ShowDialogAsync(null, viewModel);
    }

    /// <inheritdoc cref="IDialogPresenter.ShowDialogAsync{TResult}(IDialogViewModel{TResult})"/>
    public async Task<TResult> ShowDialogAsync<TResult>(IDialogViewModel<TResult> viewModel)
    {
        await ShowDialogAsync(null, viewModel);
        return viewModel.Result;
    }

    internal async Task ShowDialogAsync(ContentDialog? requestingParentDialog, IDialogViewModel viewModel)
    {
        EnsureThreadAccess();

        if (_blockDialogs)
            throw new InvalidOperationException("Show dialog requested at an invalid time while showing dialogs is blocked.");

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

    private ContentDialog CreateDialogFor(IDialogViewModel viewModel)
    {
        if (!_viewModelTypeToDialogActivator.TryGetValue(viewModel.GetType(), out var ctorFunc))
            throw new KeyNotFoundException($"No dialog registered for view model of type '{viewModel.GetType()}'.");

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

        // Set up command-to-enabled syncing for primary and secondary buttons
        ICommand? primaryCommand = null;
        ICommand? secondaryCommand = null;
        EventHandler? primaryCanExecuteChangedHandler = null;
        EventHandler? secondaryCanExecuteChangedHandler = null;
        BoolNotifier? primaryEnabledNotifier = null;
        BoolNotifier? secondaryEnabledNotifier = null;

        dialog.RegisterPropertyChangedCallback(ContentDialog.PrimaryButtonCommandProperty, OnPrimaryButtonCommandChanged);
        dialog.RegisterPropertyChangedCallback(ContentDialog.SecondaryButtonCommandProperty, OnSecondaryButtonCommandChanged);
        dialog.RegisterPropertyChangedCallback(ContentDialog.PrimaryButtonCommandParameterProperty, OnPrimaryButtonCommandParameterChanged);
        dialog.RegisterPropertyChangedCallback(ContentDialog.SecondaryButtonCommandParameterProperty, OnSecondaryButtonCommandParameterChanged);

        OnPrimaryButtonCommandChanged(dialog, ContentDialog.PrimaryButtonCommandProperty);
        OnSecondaryButtonCommandChanged(dialog, ContentDialog.SecondaryButtonCommandProperty);

        return dialog;

        void OnPrimaryButtonCommandChanged(DependencyObject sender, DependencyProperty dp)
        {
            var dialog = (ContentDialog)sender;

            if (primaryCommand is not null && primaryCanExecuteChangedHandler is not null)
                primaryCommand.CanExecuteChanged -= primaryCanExecuteChangedHandler;

            primaryCommand = null;
            primaryCanExecuteChangedHandler = null;
            primaryEnabledNotifier = null;

            if (dialog.PrimaryButtonCommand is { } newCommand && !IsPropertySetOrBound(dialog, ContentDialog.IsPrimaryButtonEnabledProperty))
            {
                primaryCommand = newCommand;
                primaryEnabledNotifier = new BoolNotifier(newCommand.CanExecute(dialog.PrimaryButtonCommandParameter));
                primaryCanExecuteChangedHandler = (_, _) => primaryEnabledNotifier.Value = newCommand.CanExecute(dialog.PrimaryButtonCommandParameter);
                newCommand.CanExecuteChanged += primaryCanExecuteChangedHandler;

                dialog.SetBinding(ContentDialog.IsPrimaryButtonEnabledProperty, new Binding
                {
                    Source = primaryEnabledNotifier,
                    Path = new PropertyPath(nameof(BoolNotifier.Value)),
                    Mode = BindingMode.OneWay,
                });
            }
        }

        void OnSecondaryButtonCommandChanged(DependencyObject sender, DependencyProperty dp)
        {
            var dialog = (ContentDialog)sender;

            if (secondaryCommand is not null && secondaryCanExecuteChangedHandler is not null)
                secondaryCommand.CanExecuteChanged -= secondaryCanExecuteChangedHandler;

            secondaryCommand = null;
            secondaryCanExecuteChangedHandler = null;
            secondaryEnabledNotifier = null;

            if (dialog.SecondaryButtonCommand is { } newCommand && !IsPropertySetOrBound(dialog, ContentDialog.IsSecondaryButtonEnabledProperty))
            {
                secondaryCommand = newCommand;
                secondaryEnabledNotifier = new BoolNotifier(newCommand.CanExecute(dialog.SecondaryButtonCommandParameter));
                secondaryCanExecuteChangedHandler = (_, _) => secondaryEnabledNotifier.Value = newCommand.CanExecute(dialog.SecondaryButtonCommandParameter);
                newCommand.CanExecuteChanged += secondaryCanExecuteChangedHandler;

                dialog.SetBinding(ContentDialog.IsSecondaryButtonEnabledProperty, new Binding
                {
                    Source = secondaryEnabledNotifier,
                    Path = new PropertyPath(nameof(BoolNotifier.Value)),
                    Mode = BindingMode.OneWay,
                });
            }
        }

        void OnPrimaryButtonCommandParameterChanged(DependencyObject sender, DependencyProperty dp)
        {
            var dialog = (ContentDialog)sender;

            if (primaryEnabledNotifier is not null && dialog.PrimaryButtonCommand is { } command)
                primaryEnabledNotifier.Value = command.CanExecute(dialog.PrimaryButtonCommandParameter);
        }

        void OnSecondaryButtonCommandParameterChanged(DependencyObject sender, DependencyProperty dp)
        {
            var dialog = (ContentDialog)sender;

            if (secondaryEnabledNotifier is not null && dialog.SecondaryButtonCommand is { } command)
                secondaryEnabledNotifier.Value = command.CanExecute(dialog.SecondaryButtonCommandParameter);
        }

        static bool IsPropertySetOrBound(DependencyObject obj, DependencyProperty dp)
        {
            object localValue = obj.ReadLocalValue(dp);
            return localValue != DependencyProperty.UnsetValue;
        }

        void OnPrimaryDialogButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (args.Cancel)
                return;

            args.Cancel = true;

            if (_dialogStack.TryPeek(out var dialogInfo) && dialogInfo.Dialog == sender)
            {
                if (dialogInfo.Dialog.PrimaryButtonCommand is { } command)
                {
                    if (command.CanExecute(dialogInfo.Dialog.PrimaryButtonCommandParameter))
                        command.Execute(dialogInfo.Dialog.PrimaryButtonCommandParameter);
                }
                else
                {
                    CloseDialog(dialogInfo.Dialog);
                }
            }
        }

        void OnSecondaryDialogButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (args.Cancel)
                return;

            args.Cancel = true;

            if (_dialogStack.TryPeek(out var dialogInfo) && dialogInfo.Dialog == sender)
            {
                if (dialogInfo.Dialog.SecondaryButtonCommand is { } command)
                {
                    if (command.CanExecute(dialogInfo.Dialog.SecondaryButtonCommandParameter))
                        command.Execute(dialogInfo.Dialog.SecondaryButtonCommandParameter);
                }
                else
                {
                    CloseDialog(dialogInfo.Dialog);
                }
            }
        }

        void OnCloseDialogButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (args.Cancel)
                return;

            args.Cancel = true;

            if (_dialogStack.TryPeek(out var dialogInfo) && dialogInfo.Dialog == sender)
            {
                if (dialogInfo.Dialog.CloseButtonCommand is { } command)
                {
                    if (command.CanExecute(dialogInfo.Dialog.CloseButtonCommandParameter))
                        command.Execute(dialogInfo.Dialog.CloseButtonCommandParameter);
                }
                else
                {
                    CloseDialog(dialogInfo.Dialog);
                }
            }
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

                    if (_dialogStack.TryPeek(out dialogInfo) && dialogInfo.Dialog == sender && !dismissibleVm.TaskRunner.IsBusy)
                        await dismissibleVm.TaskRunner.RunAsBusyAsync(dismissibleVm.OnDismissRequestedAsync());
                }
            }
        }
    }

    private sealed partial class BoolNotifier(bool initialValue) : INotifyPropertyChanged
    {
        private static readonly PropertyChangedEventArgs ValueChangedEventArgs = new(nameof(Value));

        private bool _value = initialValue;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    PropertyChanged?.Invoke(this, ValueChangedEventArgs);
                }
            }
        }
    }
}
