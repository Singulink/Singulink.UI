using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Singulink.UI.Tasks;
using Windows.Foundation;
using Windows.UI.Core;

namespace Singulink.UI.Navigation.WinUI;

/// <inheritdoc cref="INavigator"/>
public sealed partial class Navigator : NavigatorCore, IDialogPresenter
{
    private readonly ViewNavigator _viewNavigator;

    private bool _isSystemNavigationHooked;
    private bool _isWindowClosedHooked;
#if WINDOWS
    private TypedEventHandler<object, Microsoft.UI.Xaml.WindowActivatedEventArgs>? _windowActivatedHandler;
#else
    private WindowActivatedEventHandler? _windowActivatedHandler;
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="Navigator"/> class with the specified content control for displaying the active view and
    /// navigator build action.
    /// </summary>
    public Navigator(ContentControl contentControl, Action<NavigatorBuilder> buildAction)
        : this(new ContentControlNavigator(contentControl), buildAction) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Navigator"/> class with the specified root view navigator and navigator build action.
    /// </summary>
    public Navigator(ViewNavigator viewNavigator, Action<NavigatorBuilder> buildAction)
        : this(viewNavigator, CreateBuilder(buildAction))
    {
    }

    private Navigator(ViewNavigator viewNavigator, NavigatorBuilder builder)
        : base(viewNavigator, CreateTaskRunner(viewNavigator), builder)
    {
        _viewNavigator = viewNavigator;
        EnsureThreadAccess();

        _viewNavigator.NavigationControl.PointerPressed += (s, e) => {
            if (!e.Handled && e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse)
            {
                if (e.GetCurrentPoint(_viewNavigator.NavigationControl).Properties.IsXButton1Pressed ||
                    e.GetCurrentPoint(_viewNavigator.NavigationControl).Properties.IsXButton2Pressed)
                {
                    _viewNavigator.NavigationControl.CapturePointer(e.Pointer);
                }
            }
        };

        _viewNavigator.NavigationControl.PointerReleased += (s, e) => {
            if (e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse)
            {
                if (e.GetCurrentPoint(_viewNavigator.NavigationControl).Properties.IsXButton1Pressed)
                    _ = HandleSystemBackRequest();
                if (e.GetCurrentPoint(_viewNavigator.NavigationControl).Properties.IsXButton2Pressed)
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
            var deactivatedState = WindowActivationState.Deactivated;
#else
            var deactivatedState = CoreWindowActivationState.Deactivated;
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

#if __WASM__

    /// <summary>
    /// Gets the current browser route (path, query string, and fragment) without the scheme and host.
    /// </summary>
    public static string GetBrowserRoute()
    {
        var uri = new Uri(BrowserNavigationHelper.GetCurrentUrl());
        return uri.PathAndQuery + uri.Fragment;
    }

#endif

    /// <summary>
    /// Configures the navigator to handle system back/forward navigation requests on supported platforms.
    /// </summary>
    public void HookSystemNavigationRequests()
    {
        EnsureThreadAccess();

        if (_isSystemNavigationHooked)
            throw new InvalidOperationException("System navigation requests have already been hooked. Multiple calls to HookSystemNavigationRequests are not allowed.");

        _isSystemNavigationHooked = true;

#if !WINDOWS

        var navManager = SystemNavigationManager.GetForCurrentView();

        navManager.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
        navManager.BackRequested -= OnBackRequested;
        navManager.BackRequested += OnBackRequested;

        void OnBackRequested(object? sender, BackRequestedEventArgs args) => args.Handled = HandleSystemBackRequest();
#endif
    }

    /// <summary>
    /// Configures the navigator to handle window closed events to trigger navigator shutdown and prevent closing the window if
    /// navigation or busy task are in progress, dialogs are showing, or any view models are preventing navigating away from the current view.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if window closed events have already been hooked.</exception>
    public void HookWindowClosedEvents(Window window)
    {
        EnsureThreadAccess();

        if (_isWindowClosedHooked)
            throw new InvalidOperationException("Window closed events have already been hooked. Multiple calls to HookWindowClosed are not allowed.");

        _isWindowClosedHooked = true;

        bool isNavigatorShutdown = false;

        window.Closed += OnWindowClosed;

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

#if __WASM__
    /// <inheritdoc />
    protected override void OnCurrentRouteChanged(NavigatorRoute route)
    {
        BrowserNavigationHelper.ReplaceState(null, string.Empty, "/" + route.ToString());
    }
#endif

    private static NavigatorBuilder CreateBuilder(Action<NavigatorBuilder> buildAction)
    {
        var builder = new NavigatorBuilder();
        buildAction(builder);
        return builder;
    }

    private static TaskRunner CreateTaskRunner(ViewNavigator viewNavigator) =>
        new(busy => viewNavigator.NavigationControl.IsEnabled = !busy);
}
