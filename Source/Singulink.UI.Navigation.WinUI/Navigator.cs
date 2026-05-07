using System.Runtime.CompilerServices;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Singulink.UI.Tasks;
using Windows.Foundation;
using Windows.UI.Core;

namespace Singulink.UI.Navigation.WinUI;

/// <inheritdoc cref="INavigator"/>
public sealed partial class Navigator : NavigatorCore, IDialogPresenter
{
    /// <summary>
    /// The navigator instance that currently owns the system navigation request hook (<see cref="HookSystemNavigationRequests"/>). Cleared on shutdown so a
    /// new navigator can be hooked. Static handlers (popstate on WebAssembly, BackRequested elsewhere) dispatch through this field and no-op when null.
    /// </summary>
    private static Navigator? s_systemNavOwner;

    /// <summary>
    /// Tracks which window each navigator that has hooked window closed events is associated with. Enables conflict detection (same window already hooked,
    /// or same navigator already hooked to another window) and lookup of the active owner from static event handlers (e.g. WebAssembly
    /// <c>beforeunload</c>). Entries are added by <see cref="HookWindowClosedEvents"/> and removed by <see cref="OnShutDown"/>.
    /// </summary>
    private static readonly ConditionalWeakTable<Window, Navigator> s_windowClosedHookOwners = [];

    private readonly ViewNavigator _viewNavigator;

    private bool _isSystemNavigationHooked;
#if WINDOWS
    private TypedEventHandler<object, Microsoft.UI.Xaml.WindowActivatedEventArgs>? _windowActivatedHandler;
#else
    private WindowActivatedEventHandler? _windowActivatedHandler;
#endif

    private Window? _hookedWindow;
#if !__WASM__
    private TypedEventHandler<object, WindowEventArgs>? _windowClosedHandler;
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="Navigator"/> class with the specified window for displaying the active view and navigator build action. The
    /// window's content will be overridden with content managed by the navigator.
    /// </summary>
    public Navigator(Window window, Action<NavigatorBuilder> buildAction)
        : this(ViewNavigator.Create(window), CreateBuilder(buildAction)) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Navigator"/> class with the specified content control for displaying the active view and navigator build
    /// action. The content control's content will be overridden with content managed by the navigator.
    /// </summary>
    public Navigator(ContentControl contentControl, Action<NavigatorBuilder> buildAction)
        : this(ViewNavigator.Create(contentControl), buildAction) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Navigator"/> class with the specified root view navigator and navigator build action.
    /// </summary>
    public Navigator(ViewNavigator viewNavigator, Action<NavigatorBuilder> buildAction)
        : this(viewNavigator, CreateBuilder(buildAction)) { }

    private Navigator(ViewNavigator viewNavigator, NavigatorBuilder builder)
        : base(viewNavigator, CreateTaskRunner(viewNavigator), builder)
    {
        _viewNavigator = viewNavigator;
        EnsureThreadAccess();

#if __WASM__
        // Capture the UI sync context up front so static JS callbacks (popstate, beforeunload) can restore it without depending on which Hook* method
        // happened to run first.
        CaptureSynchronizationContextForJSCallbacks();
#endif

        // Ensures background is set so that nav control can receive pointer events over entire surface.

        if (viewNavigator.NavigationControl.Background is null)
            viewNavigator.NavigationControl.Background = new SolidColorBrush(Colors.Transparent);

        _viewNavigator.NavigationControl.PointerPressed += (s, e) => {
            if (!e.Handled && e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse)
            {
                var properties = e.GetCurrentPoint(_viewNavigator.NavigationControl).Properties;

                if (properties.PointerUpdateKind == Microsoft.UI.Input.PointerUpdateKind.XButton1Pressed ||
                    properties.PointerUpdateKind == Microsoft.UI.Input.PointerUpdateKind.XButton2Pressed)
                {
                    _viewNavigator.NavigationControl.CapturePointer(e.Pointer);
                }
            }
        };

        _viewNavigator.NavigationControl.PointerReleased += (s, e) => {
            if (!e.Handled && e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse)
            {
                var properties = e.GetCurrentPoint(_viewNavigator.NavigationControl).Properties;

                if (properties.PointerUpdateKind == Microsoft.UI.Input.PointerUpdateKind.XButton1Released)
                    _ = HandleSystemBackRequest();
                if (properties.PointerUpdateKind == Microsoft.UI.Input.PointerUpdateKind.XButton2Released)
                    _ = HandleSystemForwardRequest();
            }
        };
    }

    /// <summary>
    /// Configures the navigator to handle the first window activation by triggering an initial navigation action. Must be called before the window is activated
    /// for the initial navigation to work.
    /// </summary>
    /// <param name="window">The window to configure initial navigation for.</param>
    /// <param name="navigationAction">The action to perform for initial navigation.</param>
    /// <param name="fallbackAction">The action to perform if resolving the initial navigation fails. Can be used to show an error and/or navigate to a known
    /// good fallback route. If not provided, no fallback action will be performed and exceptions will be propagated to the UI thread.</param>
    /// <remarks>
    /// This method simplifies the common scenario of triggering initial navigation when the window is first activated, while ensuring that initial navigation
    /// is only triggered once and errors while attempting initial navigation can be handled gracefully.
    /// </remarks>
    public void HookWindowActivatedEvent(Window window, Func<Navigator, Task> navigationAction, Func<Navigator, NavigationRouteException, Task>? fallbackAction = null)
    {
        EnsureThreadAccess();

        if (_windowActivatedHandler is not null)
            throw new InvalidOperationException("Initial navigation has already been configured. Multiple calls to HookWindowActivatedEvent are not allowed.");

        bool hasNavigated = false;

        window.Activated += _windowActivatedHandler = OnWindowActivated;

        async void OnWindowActivated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
        {
            var window = (Window)sender;

#if WINDOWS
            const WindowActivationState deactivatedState = WindowActivationState.Deactivated;
#else
            const CoreWindowActivationState deactivatedState = CoreWindowActivationState.Deactivated;
#endif

            if (!hasNavigated && args.WindowActivationState != deactivatedState)
            {
                hasNavigated = true;
                window.Activated -= _windowActivatedHandler;

                if (!CurrentRoute.IsEmpty || IsNavigating)
                    return;

                try
                {
                    await navigationAction(this);
                }
                catch (NavigationRouteException ex) when (fallbackAction is not null)
                {
                    await fallbackAction(this, ex);
                }
            }
        }
    }

    /// <summary>
    /// Configures the navigator to handle system back/forward navigation requests on supported platforms. On WebAssembly, this also enables synchronization
    /// between the browser's history (back/forward buttons and address bar) and the navigator's in-app navigation stack.
    /// </summary>
    public void HookSystemNavigationRequests()
    {
        EnsureThreadAccess();

        if (_isSystemNavigationHooked)
            throw new InvalidOperationException("System navigation requests have already been hooked. Multiple calls to HookSystemNavigationRequests are not allowed.");

        if (s_systemNavOwner is not null)
        {
            throw new InvalidOperationException(
                "System navigation requests are already hooked by another navigator instance. Shut down the existing navigator before hooking a new one.");
        }

        _isSystemNavigationHooked = true;
        s_systemNavOwner = this;

#if __WASM__

        InitializeBrowserHistorySync();

#elif !WINDOWS

        var navManager = SystemNavigationManager.GetForCurrentView();

        navManager.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
        navManager.BackRequested += OnSystemBackRequested;
#endif
    }

#if !WINDOWS && !__WASM__
    private static void OnSystemBackRequested(object? sender, BackRequestedEventArgs args)
    {
        var owner = s_systemNavOwner;

        if (owner is not null)
            args.Handled = owner.HandleSystemBackRequest();
    }
#endif

    /// <summary>
    /// Configures the navigator to handle window closed events to trigger navigator shutdown and prevent closing the window if
    /// navigation or busy task are in progress, dialogs are showing, or any view models are preventing navigating away from the current view.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if this navigator already has window closed events hooked, or if another navigator already has
    /// window closed events hooked to the specified window.</exception>
    public void HookWindowClosedEvents(Window window)
    {
        EnsureThreadAccess();

        foreach (var (existingWindow, existingNavigator) in s_windowClosedHookOwners)
        {
            if (existingNavigator == this)
            {
                throw new InvalidOperationException(
                    "Window closed events have already been hooked by this navigator. Multiple calls to HookWindowClosedEvents are not allowed.");
            }

            if (existingWindow == window)
            {
                throw new InvalidOperationException(
                    "Window closed events are already hooked on this window by another navigator instance. " +
                    "Shut down the existing navigator before hooking a new one.");
            }

#if __WASM__
            throw new InvalidOperationException("Hooking multiple windows is not supported on WebAssembly.");
#endif
        }

        s_windowClosedHookOwners.Add(window, this);
        _hookedWindow = window;

#if __WASM__
        // On WebAssembly the Window.Closed event still fires but cannot cancel the close, and by the time it fires the browser has already committed to
        // tearing down the page. Hook the browser's beforeunload event instead, which is the only place the close can actually be blocked.
        InitializeBrowserUnloadGuard();
#else
        bool isNavigatorShutdown = false;

        _windowClosedHandler = OnWindowClosed;
        window.Closed += _windowClosedHandler;

        async void OnWindowClosed(object sender, WindowEventArgs args)
        {
            if (!isNavigatorShutdown)
            {
                args.Handled = true;

                if (await TryShutDownAsync())
                {
                    isNavigatorShutdown = true;
                    window.Close();
                }
            }
        }
#endif
    }

    /// <inheritdoc />
    protected override void OnShutDown()
    {
        if (s_systemNavOwner == this)
            s_systemNavOwner = null;

        if (_hookedWindow is not null)
        {
            s_windowClosedHookOwners.Remove(_hookedWindow);

#if !__WASM__
            if (_windowClosedHandler is not null)
            {
                _hookedWindow.Closed -= _windowClosedHandler;
                _windowClosedHandler = null;
            }
#endif
            _hookedWindow = null;
        }
    }

    /// <inheritdoc />
    protected override void EnsureThreadAccess()
    {
        if (_viewNavigator.NavigationControl.DispatcherQueue?.HasThreadAccess is not true)
            throw new InvalidOperationException("Navigator can only be accessed from the UI thread.");
    }

    /// <inheritdoc />
    protected override bool CloseLightDismissPopups()
    {
        var xamlRoot = _viewNavigator.NavigationControl.XamlRoot;
        bool closedPopup = false;

        if (xamlRoot is not null)
        {
            var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(xamlRoot);

            foreach (var popup in popups.Where(p => p.IsLightDismissEnabled))
            {
                popup.IsOpen = false;
                closedPopup = true;
            }
        }

        return closedPopup;
    }

    /// <inheritdoc />
    protected override void WireView(object view, IRoutedViewModelBase viewModel, out object? childViewNavigator)
    {
        var frameworkElement = (FrameworkElement)view;
        frameworkElement.DataContext = viewModel;

        frameworkElement.DataContextChanged += (s, e) => {
            if (e.NewValue != viewModel)
            {
                s.DataContext = viewModel;
                throw new InvalidOperationException("Navigator managed views cannot change their data context.");
            }
        };

        childViewNavigator = (frameworkElement as IParentView)?.CreateChildViewNavigator();
    }

    /// <inheritdoc />
    protected override void SetActiveView(object viewNavigator, object? view)
    {
        var frameworkElement = (FrameworkElement?)view;
        ((ViewNavigator)viewNavigator).SetActiveView(frameworkElement);
    }

    private static NavigatorBuilder CreateBuilder(Action<NavigatorBuilder> buildAction)
    {
        var builder = new NavigatorBuilder();
        buildAction(builder);
        return builder;
    }

    private static TaskRunner CreateTaskRunner(ViewNavigator viewNavigator) =>
        new(busy => viewNavigator.NavigationControl.IsEnabled = !busy);
}
