using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

#if WINDOWS
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
#endif

namespace Singulink.UI.Xaml.Behaviors;

/// <summary>
/// Provides attached properties for working with flyouts.
/// Accessors take a <see cref="DependencyObject"/> rather than the supported element type because the WinUI x:Bind code generator passes attached
/// property targets as <see cref="DependencyObject"/>, so narrower parameter types fail to compile when the property is set with x:Bind. Unsupported
/// targets throw when the property is set.
/// </summary>
public static class Flyouts
{
    #region CloseAllOnClick

    /// <summary>
    /// Attached <see cref="DependencyProperty"/> for specifying whether all open flyouts should be closed when a control inheriting from <see
    /// cref="ButtonBase"/> or <see cref="MenuFlyoutItem"/> is clicked.
    /// </summary>
    public static readonly DependencyProperty CloseAllOnClickProperty = DependencyProperty.RegisterAttached(
        "CloseAllOnClick",
        typeof(bool),
        typeof(Flyouts),
        new PropertyMetadata(false, OnCloseAllOnClickChanged));

    /// <summary>
    /// Gets a value indicating whether all open flyouts should be closed when the specified control inheriting from <see cref="ButtonBase"/> or <see
    /// cref="MenuFlyoutItem"/> is clicked.
    /// </summary>
    public static bool GetCloseAllOnClick(DependencyObject element)
    {
        return (bool)element.GetValue(CloseAllOnClickProperty);
    }

    /// <summary>
    /// Sets a value indicating whether all open flyouts should be closed when the specified control inheriting from <see cref="ButtonBase"/> or <see
    /// cref="MenuFlyoutItem"/> is clicked.
    /// </summary>
    public static void SetCloseAllOnClick(DependencyObject element, bool value)
    {
        element.SetValue(CloseAllOnClickProperty, value);
    }

    private static void OnCloseAllOnClickChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        if (obj is ButtonBase button)
        {
            if (e.NewValue.Equals(true))
                button.Click += OnCloseAllClick;
            else
                button.Click -= OnCloseAllClick;
        }
        else if (obj is MenuFlyoutItem menuFlyoutItem)
        {
            if (e.NewValue.Equals(true))
                menuFlyoutItem.Click += OnCloseAllClick;
            else
                menuFlyoutItem.Click -= OnCloseAllClick;
        }
        else
        {
            throw new InvalidOperationException($"The control type '{obj.GetType()}' is not supported by the CloseAllOnClick attached property.");
        }
    }

    private static void OnCloseAllClick(object sender, RoutedEventArgs e)
    {
        var element = (UIElement)sender;
        var flyouts = VisualTreeHelper.GetOpenPopupsForXamlRoot(element.XamlRoot).Where(p => p.IsLightDismissEnabled);

        foreach (var f in flyouts)
            f.IsOpen = false;
    }

    #endregion

    #region HyperlinkFlyout

    /// <summary>
    /// Attached <see cref="DependencyProperty"/> for specifying a <see cref="Flyout"/> that should be shown when a <see cref="Hyperlink"/> is clicked.
    /// </summary>
    public static readonly DependencyProperty HyperlinkFlyoutProperty = DependencyProperty.RegisterAttached(
        "HyperlinkFlyout",
        typeof(Flyout),
        typeof(Flyouts),
        new PropertyMetadata(false, OnHyperlinkFlyoutChanged));

    /// <summary>
    /// Gets the <see cref="Flyout"/> that should be shown when the specified <see cref="Hyperlink"/> is clicked.
    /// </summary>
    public static Flyout GetHyperlinkFlyout(DependencyObject element)
    {
        return (Flyout)element.GetValue(HyperlinkFlyoutProperty);
    }

    /// <summary>
    /// Sets the <see cref="Flyout"/> that should be shown when the specified <see cref="Hyperlink"/> is clicked.
    /// </summary>
    public static void SetHyperlinkFlyout(DependencyObject element, Flyout value)
    {
        element.SetValue(HyperlinkFlyoutProperty, value);
    }

    private static void OnHyperlinkFlyoutChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        if (obj is not Hyperlink hyperlink)
            throw new InvalidOperationException($"The element type '{obj.GetType()}' is not supported by the HyperlinkFlyout attached property.");

        hyperlink.Click -= OnHyperlinkFlyoutClick;

        if (e.NewValue != null)
            hyperlink.Click += OnHyperlinkFlyoutClick;
    }

    private static void OnHyperlinkFlyoutClick(Hyperlink sender, HyperlinkClickEventArgs args)
    {
        var textBlock = (TextBlock)VisualTreeHelper.GetParent(sender);
        var flyout = GetHyperlinkFlyout(sender);

        flyout.ShowAt(textBlock);
    }

    #endregion

    #region PersistOn

    /// <summary>
    /// Attached <see cref="DependencyProperty"/> for specifying the changes of its window that a flyout stays open through, for the drawer or form style
    /// flyouts that the framework would otherwise close along with whatever they were being used for. Every other way of closing the flyout keeps working.
    /// See <see cref="FlyoutPersistence"/> for what each value does on Uno Platform and on WinUI.
    /// </summary>
    public static readonly DependencyProperty PersistOnProperty = DependencyProperty.RegisterAttached(
        "PersistOn", typeof(FlyoutPersistence), typeof(Flyouts), new PropertyMetadata(FlyoutPersistence.None, OnPersistOnChanged));

    // What is kept for a flyout lives in a property of the flyout rather than in a table keyed on it, as WinUI can hand out a new managed object for the same
    // flyout once the previous one has been collected.
    private static readonly DependencyProperty WindowPersistenceProperty = DependencyProperty.RegisterAttached(
        "WindowPersistence", typeof(object), typeof(Flyouts), new PropertyMetadata(null));

    /// <summary>
    /// Gets the changes of its window that the specified flyout stays open through.
    /// </summary>
    public static FlyoutPersistence GetPersistOn(DependencyObject flyout) => (FlyoutPersistence)flyout.GetValue(PersistOnProperty);

    /// <summary>
    /// Sets the changes of its window that the specified flyout stays open through.
    /// </summary>
    public static void SetPersistOn(DependencyObject flyout, FlyoutPersistence value) => flyout.SetValue(PersistOnProperty, value);

    private static bool PersistsOn(FlyoutBase flyout, FlyoutPersistence change) => (GetPersistOn(flyout) & change) != 0;

    private static void OnPersistOnChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        if (obj is not FlyoutBase flyout)
            throw new InvalidOperationException($"The element type '{obj.GetType()}' is not supported by the PersistOn attached property.");

        bool persist = GetPersistOn(flyout) != FlyoutPersistence.None;
        var persistence = (WindowPersistence?)flyout.GetValue(WindowPersistenceProperty);

        if (persist && persistence is null)
        {
            persistence = new WindowPersistence();
            flyout.SetValue(WindowPersistenceProperty, persistence);

            flyout.Opening += OnPersistentFlyoutOpening;
            flyout.Closing += OnPersistentFlyoutClosing;

#if WINDOWS
            flyout.Closed += OnPersistentFlyoutClosed;
#endif

            if (flyout.IsOpen)
                persistence.OnOpening(flyout);
        }
        else if (!persist && persistence is not null)
        {
            flyout.ClearValue(WindowPersistenceProperty);

            flyout.Opening -= OnPersistentFlyoutOpening;
            flyout.Closing -= OnPersistentFlyoutClosing;

#if WINDOWS
            flyout.Closed -= OnPersistentFlyoutClosed;
            persistence.CancelReopen();
#endif
        }
    }

    private static void OnPersistentFlyoutOpening(object? sender, object e)
    {
        if (sender is FlyoutBase flyout && flyout.GetValue(WindowPersistenceProperty) is WindowPersistence persistence)
            persistence.OnOpening(flyout);
    }

    private static void OnPersistentFlyoutClosing(FlyoutBase sender, FlyoutBaseClosingEventArgs args)
    {
        // The framework gives no reason when it closes a flyout, so the reason is read from what the window is doing at that moment.
        if (sender.GetValue(WindowPersistenceProperty) is WindowPersistence persistence && persistence.IsCloseForWindowChange(sender))
            args.Cancel = true;
    }

#if WINDOWS
    private static void OnPersistentFlyoutClosed(object? sender, object e)
    {
        if (sender is FlyoutBase flyout && flyout.GetValue(WindowPersistenceProperty) is WindowPersistence persistence)
            persistence.OnClosed(flyout);
    }
#endif

#if HAS_UNO
    /// <summary>
    /// Tells the closes that Uno starts for a change of the window apart from every other close of a flyout. Uno makes them synchronously while it reports
    /// the change, so they are the ones that come before the dispatcher gets to run again after a change was seen. A close that the application makes in
    /// that same stretch cannot be told from them.
    /// </summary>
    private sealed class WindowPersistence
    {
        private static Window? _window;
        private static bool _isDeactivating;
        private static bool _isResizing;

#pragma warning disable CA1822 // Mark members as static (they are instance members on WinUI)

        public void OnOpening(FlyoutBase flyout)
        {
            // The window is asked for here, where there is one for certain, as Window.Current creates one when none exists yet. It is the one window there
            // is on single window targets, and the first one elsewhere.
            if (_window is null && Window.Current is { } window)
            {
                _window = window;

                // The handlers name no argument types, as those differ between Uno 6 and Uno 7. Deactivated is 1 in both the CoreWindow enumeration
                // that Uno 6 reports and the WinUI one that Uno 7 does.
                window.Activated += (_, args) => OnWindowActivated(isDeactivated: (int)args.WindowActivationState == 1);
                window.SizeChanged += (_, _) => OnWindowSizeChanged();
            }
        }

        public bool IsCloseForWindowChange(FlyoutBase flyout)
        {
            return (_isDeactivating && PersistsOn(flyout, FlyoutPersistence.WindowDeactivation)) ||
                (_isResizing && PersistsOn(flyout, FlyoutPersistence.WindowResize));
        }

#pragma warning restore CA1822

        // Light dismiss popups register for the same event when they open, as in WinUI, which puts this handler ahead of theirs.
        private static void OnWindowActivated(bool isDeactivated)
        {
            if (isDeactivated)
            {
                _isDeactivating = true;
                RunWhenReported(static () => _isDeactivating = false);
            }
        }

        // Uno raises this ahead of XamlRoot.Changed, which is what closes light dismiss popups and can come more than once for a resize.
        private static void OnWindowSizeChanged()
        {
            _isResizing = true;
            RunWhenReported(static () => _isResizing = false);
        }

        // At high priority, for as little as possible of what the dispatcher already has queued to run before it.
        private static void RunWhenReported(Microsoft.UI.Dispatching.DispatcherQueueHandler callback)
        {
            if (_window?.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, callback) is not true)
                callback();
        }
    }
#elif WINDOWS
    /// <summary>
    /// Tells the close that WinUI starts for a window deactivation apart from every other close of a flyout, and shows a flyout again that WinUI closed for
    /// a change of the window's size.
    /// </summary>
    private sealed class WindowPersistence
    {
        private const int NoActivation = -1;

        private static readonly TimeSpan ResizeSettleTime = TimeSpan.FromMilliseconds(100);
        private static readonly Dictionary<ulong, ActivationCounter> _activationCounters = [];

        // The activation of the window that the flyout has already been kept open after, for later closes in the same period of inactivity to go through.
        private int _sparedActivation = NoActivation;
        private bool _isSparing;

        private FrameworkElement? _target;
        private Windows.Foundation.Size _openedSize;
        private bool _wasAskedToClose;
        private DispatcherQueueTimer? _reopenTimer;

        public void OnOpening(FlyoutBase flyout)
        {
            // A flyout opened in an inactive window has no deactivation coming, so there is no close to keep from it until the window is activated again.
            _sparedActivation = IsWindowActive(flyout, out int activation) ? NoActivation : activation;

            CancelReopen();
            _target = flyout.Target ?? _target;
            _openedSize = (flyout.XamlRoot ?? _target?.XamlRoot)?.Size ?? default;
            _wasAskedToClose = false;
        }

        // WinUI closes light dismiss popups when its island loses focus to another window, synchronously and ahead of anything the application does, so
        // the first close that comes while the window is inactive is taken to be that one, together with any other that comes before the dispatcher
        // gets to run again (closing one flyout can close another).
        public bool IsCloseForWindowChange(FlyoutBase flyout)
        {
            bool isForDeactivation = PersistsOn(flyout, FlyoutPersistence.WindowDeactivation) && IsCloseForWindowDeactivation(flyout);

            // The flyout is asked before every close but the one for a change of size, which is how that one is recognized once it has closed.
            _wasAskedToClose = !isForDeactivation;

            return isForDeactivation;
        }

        private bool IsCloseForWindowDeactivation(FlyoutBase flyout)
        {
            if (_isSparing)
                return true;

            if (IsWindowActive(flyout, out int activation) || _sparedActivation == activation)
                return false;

            _sparedActivation = activation;
            _isSparing = DispatcherQueue.GetForCurrentThread()?.TryEnqueue(() => _isSparing = false) is true;

            return true;
        }

        // WinUI closes the popup of a flyout without asking the flyout when the size of its XamlRoot changes, and does so at every step of a resize. A
        // flyout that closed unasked with the window at another size than it opened at is one of those, and is shown again once the size stops changing.
        public void OnClosed(FlyoutBase flyout)
        {
            var target = _target;
            _target = null;

            if (_wasAskedToClose || !PersistsOn(flyout, FlyoutPersistence.WindowResize))
                return;

            if (target is not { IsLoaded: true, XamlRoot: { } xamlRoot } || xamlRoot.Size == _openedSize)
                return;

            CancelReopen();

            var settlingSize = xamlRoot.Size;
            var timer = _reopenTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
            timer.Interval = ResizeSettleTime;

            timer.Tick += (_, _) => {
                if (_reopenTimer != timer)
                {
                    timer.Stop();
                }
                else if (flyout.IsOpen || !PersistsOn(flyout, FlyoutPersistence.WindowResize) || target is not { IsLoaded: true, XamlRoot: { } currentRoot })
                {
                    CancelReopen();
                }
                else if (currentRoot.Size != settlingSize)
                {
                    settlingSize = currentRoot.Size;
                }
                else
                {
                    CancelReopen();
                    flyout.ShowAt(target);
                }
            };

            timer.Start();
        }

        public void CancelReopen()
        {
            _reopenTimer?.Stop();
            _reopenTimer = null;
        }

        private static bool IsWindowActive(FlyoutBase flyout, out int activation)
        {
            activation = 0;

            if ((flyout.XamlRoot ?? flyout.Target?.XamlRoot)?.ContentIslandEnvironment?.AppWindowId is not { } windowId)
                return true;

            if (!_activationCounters.TryGetValue(windowId.Value, out var counter))
                _activationCounters.Add(windowId.Value, counter = new ActivationCounter(InputActivationListener.GetForWindowId(windowId)));

            activation = counter.Activation;

            // The island loses focus after another window has become the foreground window.
            nint window = Win32Interop.GetWindowFromWindowId(windowId);
            return window == 0 || GetForegroundWindow() == window;
        }

        [DllImport("user32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern nint GetForegroundWindow();

        /// <summary>
        /// Counts the activations of a window, for one period of inactivity to be told from the next.
        /// </summary>
        private sealed class ActivationCounter
        {
            // Kept so that the listener, and with it the subscription, lives as long as the counter.
            private readonly InputActivationListener _listener;

            public ActivationCounter(InputActivationListener listener)
            {
                _listener = listener;
                _listener.InputActivationChanged += (sender, _) => {
                    if (sender.State is InputActivationState.Activated)
                        Activation++;
                };
            }

            public int Activation { get; private set; }
        }
    }
#endif

    #endregion
}
