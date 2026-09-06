using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
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

        taskRunner = new TaskRunner(busy => LayerPresentation.Get(contentDialog).IsBusy = busy);
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
                else if (top.ViewModel is IDismissibleDialogViewModel)
                {
                    // If the view model is dismissible, we treat the close button like a dismiss action and invoke the same logic as dialog closing to allow
                    // the view model to veto the close if needed.
                    sender.Hide();
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
    protected override void StartShowingDialog(object dialog)
    {
#if WINDOWS
        // WinUI only allows a single popup-placed content dialog per XAML root, so dialogs are shown in-place inside stacked overlay layers instead (see
        // DialogLayer). Uno has no such restriction and its in-place placement is not implemented, so each dialog uses its own popup there.
        _ = ShowDialogInLayerAsync((ContentDialog)dialog);
#else
        _ = ((ContentDialog)dialog).ShowAsync();
#endif
    }

    /// <inheritdoc/>
    protected override void HideDialog(object dialog) => ((ContentDialog)dialog).Hide();

    /// <inheritdoc/>
    protected override object? CaptureDialogFocusState()
    {
        if (_viewNavigator.NavigationControl.XamlRoot is not { } xamlRoot || FocusManager.GetFocusedElement(xamlRoot) is not Control control)
            return null;

        return new FocusStateSnapshot(control, control.FocusState);
    }

    /// <inheritdoc/>
    protected override void OnLayerCoveredChanged(object? dialog, bool isCovered)
    {
        var control = dialog is ContentDialog contentDialog ? contentDialog : _viewNavigator.NavigationControl;
        LayerPresentation.Get(control).IsCovered = isCovered;
    }

    /// <inheritdoc/>
    protected override void RestoreDialogFocusState(object dialog, object? focusState)
    {
        var contentDialog = (ContentDialog)dialog;

        // Deferred because the frameworks touch focus after this point: WinUI restores focus to the element focused before the closed dialog opened once
        // its ShowAsync operation completes (which can be an element hidden beneath the top dialog), and Uno lets focus fall back to the page when the
        // closed dialog's popup unloads. Running after both leaves focus inside the dialog that is now on top.
        contentDialog.DispatcherQueue.TryEnqueue(() => {
            if (TryGetTopDialog() is not { } top || !ReferenceEquals(top.Navigator.Dialog, contentDialog))
                return;

            if (focusState is FocusStateSnapshot state && IsDescendantOf(state.Element, contentDialog) && state.TryRestore())
                return;

            if (contentDialog.XamlRoot is { } xamlRoot && FocusManager.GetFocusedElement(xamlRoot) is DependencyObject focused && IsDescendantOf(focused, contentDialog))
                return;

            FocusIntoLayer(contentDialog);
        });
    }

    /// <summary>
    /// Ensures focus is inside the specified layer control: its first focusable element if it has one, otherwise the layer control itself. Focus must
    /// never be left in a layer that is covered by a dialog, since keyboard input would then operate controls the user cannot see or reach.
    /// </summary>
    private static void FocusIntoLayer(Control layer)
    {
        if (FocusManager.FindFirstFocusableElement(layer) is Control first && first.Focus(FocusState.Programmatic))
            return;

        layer.IsTabStop = true;
        layer.Focus(FocusState.Programmatic);
    }

    private static bool IsDescendantOf(DependencyObject element, DependencyObject ancestor)
    {
        for (var current = element; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (ReferenceEquals(current, ancestor))
                return true;
        }

        return false;
    }

    /// <summary>
    /// The element that had keyboard focus at a point in time, together with its focus state so that restoring it preserves keyboard focus visuals.
    /// </summary>
    private sealed record FocusStateSnapshot(Control Element, FocusState FocusState)
    {
        public static FocusStateSnapshot? Capture(Control scope)
        {
            if (scope.XamlRoot is not { } xamlRoot || FocusManager.GetFocusedElement(xamlRoot) is not Control focused || !IsDescendantOf(focused, scope))
                return null;

            return new FocusStateSnapshot(focused, focused.FocusState);
        }

        public bool TryRestore() => Element.Focus(FocusState is FocusState.Unfocused ? FocusState.Programmatic : FocusState);
    }

    /// <summary>
    /// Tracks how a layer (the root view or a dialog) is presented. A layer is shown as disabled while it is busy, unless it is covered by a child dialog,
    /// in which case the child dialog is what blocks interaction and the layer is left looking normal underneath the dialog smoke. While covered, the
    /// layer also swallows keyboard input so that keys can never operate it even if focus falls into it (e.g. when the dialog above it is disabled while
    /// busy, which drops focus to the next focusable element). Keyboard focus is captured when the layer becomes busy and restored once the layer is
    /// available again.
    /// </summary>
    private sealed class LayerPresentation
    {
        private static readonly ConditionalWeakTable<Control, LayerPresentation> Presentations = new();

        private readonly Control _control;
        private bool _isBusy;
        private bool _isCovered;
        private FocusStateSnapshot? _busyFocusState;

        private LayerPresentation(Control control)
        {
            _control = control;
        }

        public static LayerPresentation Get(Control control) => Presentations.GetValue(control, static c => new LayerPresentation(c));

        public bool IsBusy
        {
            set {
                if (_isBusy == value)
                    return;

                _isBusy = value;

                if (value)
                    _busyFocusState ??= FocusStateSnapshot.Capture(_control);

                Apply();

                if (!value)
                    RestoreFocusWhenAvailable();
            }
        }

        public bool IsCovered
        {
            set {
                if (_isCovered == value)
                    return;

                _isCovered = value;
                Apply();

                if (value)
                {
                    _control.PreviewKeyDown += OnCoveredKeyEvent;
                    _control.PreviewKeyUp += OnCoveredKeyEvent;
#if WINDOWS
                    _control.CharacterReceived += OnCoveredCharacterReceived; // Not implemented on Uno, where text entry goes through the key events.
#endif
                }
                else
                {
                    _control.PreviewKeyDown -= OnCoveredKeyEvent;
                    _control.PreviewKeyUp -= OnCoveredKeyEvent;
#if WINDOWS
                    _control.CharacterReceived -= OnCoveredCharacterReceived;
#endif
                    RestoreFocusWhenAvailable();
                }
            }
        }

        private void Apply() => _control.IsEnabled = _isCovered || !_isBusy;

        // WinUI tunnels preview input from the XAML root content, which includes input aimed at popups (where dialogs live), so only input that originates
        // inside the covered layer's own visual subtree is swallowed. Popup content is never part of that subtree: its visual parent chain runs through the
        // popup root instead.
        private void OnCoveredKeyEvent(object sender, KeyRoutedEventArgs e)
        {
            if (e.OriginalSource is DependencyObject source && IsDescendantOf(source, _control))
                e.Handled = true;
        }

#if WINDOWS
        private void OnCoveredCharacterReceived(UIElement sender, CharacterReceivedRoutedEventArgs e)
        {
            if (e.OriginalSource is DependencyObject source && IsDescendantOf(source, _control))
                e.Handled = true;
        }
#endif

        /// <summary>
        /// Restores the focus captured when the layer became busy, once the layer is neither busy nor covered, falling back to the layer's first focusable
        /// element (or the layer itself) if the original element is no longer available. Deferred so it runs after the framework's own focus handling
        /// (disabled controls drop focus, and closing dialogs move it) and after <c>IsEnabled</c> has propagated.
        /// </summary>
        private void RestoreFocusWhenAvailable()
        {
            if (_isBusy || _isCovered || _busyFocusState is not { } state)
                return;

            _busyFocusState = null;

            _control.DispatcherQueue.TryEnqueue(() => {
                if (_isBusy || _isCovered)
                    return;

                if (_control.XamlRoot is { } xamlRoot && FocusManager.GetFocusedElement(xamlRoot) is DependencyObject focused && IsDescendantOf(focused, _control))
                    return;

                if (IsDescendantOf(state.Element, _control) && state.TryRestore())
                    return;

                // Focus was inside this layer when it became busy, so make sure it ends up back inside it even if the original element is gone.
                FocusIntoLayer(_control);
            });
        }
    }

#if WINDOWS
    private readonly List<DialogLayer> _dialogLayers = [];

    private async Task ShowDialogInLayerAsync(ContentDialog dialog)
    {
        var xamlRoot = _viewNavigator.NavigationControl.XamlRoot ?? throw new InvalidOperationException("XamlRoot is not available");
        var layer = new DialogLayer(dialog, xamlRoot);

        foreach (var lower in _dialogLayers)
            lower.IsInteractive = false;

        _dialogLayers.Add(layer);
        layer.Open();

        try
        {
            await dialog.ShowAsync(ContentDialogPlacement.InPlace);
        }
        finally
        {
            _dialogLayers.Remove(layer);
            layer.Close();

            if (_dialogLayers.Count > 0)
                _dialogLayers[^1].IsInteractive = true;
        }
    }

    /// <summary>
    /// A full-window popup layer that hosts one in-place content dialog: it draws the smoke behind the dialog, blocks input to everything beneath it and
    /// cycles tab focus within the dialog. Layers stack in the order they are opened.
    /// </summary>
    private sealed class DialogLayer
    {
        private readonly XamlRoot _xamlRoot;
        private readonly Grid _root;
        private readonly Popup _popup;

        public DialogLayer(ContentDialog dialog, XamlRoot xamlRoot)
        {
            _xamlRoot = xamlRoot;

            _root = new Grid {
                Background = GetSmokeBrush(),
                TabFocusNavigation = KeyboardNavigationMode.Cycle,
                Width = xamlRoot.Size.Width,
                Height = xamlRoot.Size.Height,
            };

            _root.Children.Add(dialog);

            _popup = new Popup {
                XamlRoot = xamlRoot,
                Child = _root,
                IsLightDismissEnabled = false,
            };
        }

        public bool IsInteractive
        {
            set => _root.IsHitTestVisible = value;
        }

        public void Open()
        {
            _xamlRoot.Changed += OnXamlRootChanged;
            _popup.IsOpen = true;
        }

        public void Close()
        {
            _xamlRoot.Changed -= OnXamlRootChanged;
            _popup.IsOpen = false;
            _root.Children.Clear();
            _popup.Child = null;
        }

        private void OnXamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs args)
        {
            _root.Width = sender.Size.Width;
            _root.Height = sender.Size.Height;
        }

        private static Brush GetSmokeBrush()
        {
            if (Application.Current.Resources.TryGetValue("ContentDialogSmokeFill", out object? resource) && resource is Brush brush)
                return brush;

            return new SolidColorBrush(Windows.UI.Color.FromArgb(0x4D, 0, 0, 0));
        }
    }
#endif

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
