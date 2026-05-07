using System.Collections.Frozen;
using System.Collections.Immutable;
using System.ComponentModel;
using Singulink.UI.Tasks;

namespace Singulink.UI.Navigation;

/// <summary>
/// Provides the base implementation of a navigator that manages route-based navigation with a hierarchy of views.
/// </summary>
public abstract partial class NavigatorCore : INavigator
{
    private static readonly NavigatorRoute EmptyRoute = new([], null);

    private readonly object _rootViewNavigator;
    private readonly ImmutableArray<RoutePart> _routeParts;
    private readonly FrozenDictionary<Type, MappingInfo> _viewModelTypeToMappingInfo;
    private readonly FrozenDictionary<Type, Func<object>> _viewModelTypeToDialogActivator;
    private readonly FrozenSet<Type> _viewModelTypesWithChildren;
    private readonly Stack<DialogStackEntry> _dialogStack = [];
    private readonly int _maxStackSize;
    private readonly int _maxBackStackCachedDepth;
    private readonly int _maxForwardStackCachedDepth;

    private List<NavigatorRoute> _routeStack = [];
    private int _currentRouteIndex = -1;

    private bool _isNavigating;
    private bool _isRedirecting;

    private bool _blockNavigation;
    private bool _blockDialogs;

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigatorCore"/> class.
    /// </summary>
    protected NavigatorCore(object rootViewNavigator, ITaskRunner taskRunner, NavigatorBuilderCore builder)
    {
        builder.Validate();

        _rootViewNavigator = rootViewNavigator;

        RootServices = builder.Services;
        TaskRunner = taskRunner;

        _routeParts = [.. builder.RouteParts];
        _viewModelTypeToMappingInfo = builder.ViewModelTypeToMappingInfo.ToFrozenDictionary();
        _viewModelTypeToDialogActivator = builder.ViewModelTypeToDialogActivator.ToFrozenDictionary();
        _viewModelTypesWithChildren = builder.ViewModelTypesWithChildren.ToFrozenSet();
        _maxStackSize = builder.MaxNavigationStacksSize + 1; // +1 to account for the current route
        _maxBackStackCachedDepth = builder.MaxBackStackCachedDepth;
        _maxForwardStackCachedDepth = builder.MaxForwardStackCachedDepth;
    }

    /// <inheritdoc/>
    public IServiceProvider RootServices { get; }

    /// <inheritdoc/>
    public ITaskRunner TaskRunner { get; }

    /// <inheritdoc/>
    public bool CanGoBack
    {
        get {
            EnsureThreadAccess();
            return !_isNavigating && !IsShowingDialogCore && _currentRouteIndex > 0;
        }
    }

    /// <inheritdoc/>
    public bool CanGoForward
    {
        get {
            EnsureThreadAccess();
            return !_isNavigating && !IsShowingDialogCore && _currentRouteIndex < _routeStack.Count - 1;
        }
    }

    /// <inheritdoc/>
    public bool CanRefresh
    {
        get {
            EnsureThreadAccess();
            return !_isNavigating && !IsShowingDialogCore && CurrentRouteCore is not null;
        }
    }

    /// <inheritdoc/>
    public bool IsNavigating
    {
        get {
            EnsureThreadAccess();
            return _isNavigating;
        }
    }

    /// <inheritdoc/>
    public bool IsShowingDialog
    {
        get {
            EnsureThreadAccess();
            return IsShowingDialogCore;
        }
    }

    /// <inheritdoc/>
    public NavigatorRoute CurrentRoute
    {
        get {
            EnsureThreadAccess();
            return CurrentRouteCore ?? EmptyRoute;
        }
    }

    /// <inheritdoc/>
    public bool HasBackHistory
    {
        get {
            EnsureThreadAccess();
            return _currentRouteIndex > 0;
        }
    }

    /// <inheritdoc/>
    public bool HasForwardHistory
    {
        get {
            EnsureThreadAccess();
            return _currentRouteIndex < _routeStack.Count - 1;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the navigator is currently in a state where <see cref="TryShutDownAsync"/> could begin without immediately returning
    /// <see langword="false"/> due to busy state. This does not run any view model lifecycle methods, so view models that would cancel a navigation away
    /// can still cause an actual shutdown attempt to fail. Useful for synchronous host hooks (e.g. WebAssembly <c>beforeunload</c>) where awaiting is not
    /// possible.
    /// </summary>
    protected bool CanMaybeShutDownNow
    {
        get {
            EnsureThreadAccess();
            return !_isNavigating && !IsShowingDialogCore && !TaskRunner.IsBusy && !_blockNavigation;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the current navigation is occurring as part of a redirect from another navigation.
    /// </summary>
    protected bool IsRedirecting => _isRedirecting;

    private NavigatorRoute? CurrentRouteCore => _currentRouteIndex >= 0 ? _routeStack[_currentRouteIndex] : null;

    private bool IsShowingDialogCore => _dialogStack.Count > 0;

    /// <summary>
    /// Ensures that the current call is happening on the required thread.
    /// </summary>
    protected abstract void EnsureThreadAccess();

    /// <summary>
    /// Closes any light-dismiss popups that are currently open.
    /// </summary>
    /// <returns><see langword="true"/> if any popups were closed; otherwise <see langword="false"/>.</returns>
    protected abstract bool CloseLightDismissPopups();

    /// <summary>
    /// Wires up a newly materialized view with the specified view model. The implementation should attach the view model as the view's data context, hook up
    /// any framework-specific bindings/event handlers, and produce the child view navigator if the view is a parent view.
    /// </summary>
    /// <param name="view">The materialized view instance.</param>
    /// <param name="viewModel">The view model instance.</param>
    /// <param name="childViewNavigator">The child view navigator if the view is a parent view, otherwise <see langword="null"/>.</param>
    protected abstract void WireView(object view, IRoutedViewModelBase viewModel, out object? childViewNavigator);

    /// <summary>
    /// Sets the active view in the specified view navigator.
    /// </summary>
    /// <param name="viewNavigator">The view navigator that should display the view, or <see langword="null"/> for the root.</param>
    /// <param name="view">The view to display, or <see langword="null"/> to clear the view.</param>
    protected abstract void SetActiveView(object viewNavigator, object? view);

    /// <summary>
    /// Called when the current route changes.
    /// </summary>
    /// <param name="route">The new current route.</param>
    protected virtual void OnCurrentRouteChanged(NavigatorRoute route) { }

    /// <summary>
    /// Called synchronously at the start of a navigation, before any view model lifecycle methods are invoked and before any awaits occur. Implementations
    /// should perform any synchronous work required at the start of a navigation, such as updating browser history state on WebAssembly platforms (which
    /// must happen synchronously while user activation is still valid).
    /// </summary>
    /// <param name="navigationType">The type of navigation that is starting.</param>
    /// <param name="targetRoute">The route that the navigation is targeting. Note that this may differ from the final route if a redirect occurs during
    /// navigation, in which case a nested navigation will be started for the redirect target.</param>
    /// <returns>An optional state object that will be passed back to <see cref="OnNavigationCompleted"/> when the navigation completes. This is useful for
    /// implementations that need to track per-navigation state without having to use instance fields (which would be clobbered by nested redirect
    /// navigations).</returns>
    /// <remarks>
    /// <para>If a redirect occurs during navigation, this method will be called again for the redirect target as part of the nested navigation. The <see
    /// cref="IsRedirecting"/> property can be used to detect this case.</para>
    /// </remarks>
    protected virtual object? OnNavigationStarting(NavigationType navigationType, NavigatorRoute targetRoute) => null;

    /// <summary>
    /// Called when a navigation completes, either successfully or due to cancellation. This is always called if <see cref="OnNavigationStarting"/> was
    /// called for the same navigation.
    /// </summary>
    /// <param name="navigationType">The type of navigation that completed.</param>
    /// <param name="targetRoute">The route that the navigation was targeting (the same value passed to <see cref="OnNavigationStarting"/>).</param>
    /// <param name="result">The result of the navigation.</param>
    /// <param name="state">The state object returned from <see cref="OnNavigationStarting"/> for this navigation.</param>
    protected virtual void OnNavigationCompleted(NavigationType navigationType, NavigatorRoute targetRoute, NavigationResult result, object? state) { }

    /// <summary>
    /// Called once on the UI thread after a successful <see cref="TryShutDownAsync"/> has torn down the navigator. Implementations should release any
    /// resources tied to the navigator instance (e.g. unsubscribe from system event handlers) so the instance can be garbage collected and a new navigator
    /// can be hooked in its place.
    /// </summary>
    protected virtual void OnShutDown() { }
}
