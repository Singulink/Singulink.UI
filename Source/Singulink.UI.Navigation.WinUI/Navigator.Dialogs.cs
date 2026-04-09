using System.ComponentModel;
using Microsoft.UI.Xaml.Data;
using Singulink.UI.Tasks;

namespace Singulink.UI.Navigation.WinUI;

/// <content>
/// Provides framework-specific dialog hook implementations for the navigator.
/// </content>
partial class Navigator
{
    /// <inheritdoc/>
    protected override void WireDialog(object dialog, IDialogViewModel viewModel, out ITaskRunner taskRunner)
    {
        var contentDialog = (ContentDialog)dialog;

        contentDialog.DataContext = viewModel;
        contentDialog.XamlRoot = _viewNavigator.NavigationControl.XamlRoot ?? throw new InvalidOperationException("XamlRoot is not available");

        contentDialog.DataContextChanged += (s, e) => {
            var contentDialog = (ContentDialog)s;

            if (e.NewValue != viewModel)
            {
                contentDialog.DataContext = viewModel;
                throw new InvalidOperationException("Navigator managed views cannot change their data context.");
            }
        };

        contentDialog.PrimaryButtonClick += OnPrimaryDialogButtonClick;
        contentDialog.SecondaryButtonClick += OnSecondaryDialogButtonClick;
        contentDialog.CloseButtonClick += OnCloseDialogButtonClick;
        contentDialog.Closing += OnDialogClosing;

        // Set up command-to-enabled syncing for primary and secondary buttons
        ICommand? primaryCommand = null;
        ICommand? secondaryCommand = null;
        EventHandler? primaryCanExecuteChangedHandler = null;
        EventHandler? secondaryCanExecuteChangedHandler = null;
        BoolNotifier? primaryEnabledNotifier = null;
        BoolNotifier? secondaryEnabledNotifier = null;

        contentDialog.RegisterPropertyChangedCallback(ContentDialog.PrimaryButtonCommandProperty, OnPrimaryButtonCommandChanged);
        contentDialog.RegisterPropertyChangedCallback(ContentDialog.SecondaryButtonCommandProperty, OnSecondaryButtonCommandChanged);
        contentDialog.RegisterPropertyChangedCallback(ContentDialog.PrimaryButtonCommandParameterProperty, OnPrimaryButtonCommandParameterChanged);
        contentDialog.RegisterPropertyChangedCallback(ContentDialog.SecondaryButtonCommandParameterProperty, OnSecondaryButtonCommandParameterChanged);

        OnPrimaryButtonCommandChanged(contentDialog, ContentDialog.PrimaryButtonCommandProperty);
        OnSecondaryButtonCommandChanged(contentDialog, ContentDialog.SecondaryButtonCommandProperty);

        taskRunner = new TaskRunner(busy => contentDialog.IsEnabled = !busy);
        return;

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

            if (TryGetTopDialog() is { } top && ReferenceEquals(top.Navigator.Dialog, sender))
            {
                if (sender.PrimaryButtonCommand is { } command)
                {
                    if (command.CanExecute(sender.PrimaryButtonCommandParameter))
                        command.Execute(sender.PrimaryButtonCommandParameter);
                }
                else
                {
                    top.Navigator.Close();
                }
            }
        }

        void OnSecondaryDialogButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (args.Cancel)
                return;

            args.Cancel = true;

            if (TryGetTopDialog() is { } top && ReferenceEquals(top.Navigator.Dialog, sender))
            {
                if (sender.SecondaryButtonCommand is { } command)
                {
                    if (command.CanExecute(sender.SecondaryButtonCommandParameter))
                        command.Execute(sender.SecondaryButtonCommandParameter);
                }
                else
                {
                    top.Navigator.Close();
                }
            }
        }

        void OnCloseDialogButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (args.Cancel)
                return;

            args.Cancel = true;

            if (TryGetTopDialog() is { } top && ReferenceEquals(top.Navigator.Dialog, sender))
            {
                if (sender.CloseButtonCommand is { } command)
                {
                    if (command.CanExecute(sender.CloseButtonCommandParameter))
                        command.Execute(sender.CloseButtonCommandParameter);
                }
                else
                {
                    top.Navigator.Close();
                }
            }
        }

        async void OnDialogClosing(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
            if (TryGetTopDialog() is { } top && ReferenceEquals(top.Navigator.Dialog, sender))
            {
                args.Cancel = true;

                if (top.ViewModel is IDismissibleDialogViewModel dismissibleVm)
                {
                    // Yield to prevent reentrant Hide() calls if the dismissible view model calls Close() in OnDismissRequested
                    // otherwise the dialog will not hide.

                    await Task.Yield();

                    // Make sure we are still the top dialog and another event didn't close the dialog after the yield.

                    if (TryGetTopDialog() is { } top2 && ReferenceEquals(top2.Navigator.Dialog, sender) && !top2.Navigator.TaskRunner.IsBusy)
                        await top2.Navigator.TaskRunner.RunAsBusyAsync(dismissibleVm.OnDismissRequestedAsync());
                }
            }
        }
    }

    /// <inheritdoc/>
    protected override void StartShowingDialog(object dialog) => _ = ((ContentDialog)dialog).ShowAsync();

    /// <inheritdoc/>
    protected override void HideDialog(object dialog) => ((ContentDialog)dialog).Hide();

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
