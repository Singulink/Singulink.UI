using System.Collections.Frozen;
using System.Collections.Immutable;
using Microsoft.UI.Xaml.Media;
using Singulink.UI.Tasks;

namespace Singulink.UI.Navigation.WinUI;

/// <inheritdoc cref="INavigator"/>
public sealed partial class Navigator : INavigator, IAsyncDisposable
{
    private static readonly ConcreteRoute EmptyRoute = new([], RouteOptions.Empty);

    private readonly ViewNavigator _viewNavigator;

    private readonly FrozenDictionary<Type, MappingInfo> _viewModelTypeToMappingInfo;
    private readonly FrozenDictionary<Type, Func<ContentDialog>> _viewModelTypeToDialogActivator;
    private readonly ImmutableArray<RoutePart> _routeParts;

    private readonly IServiceProvider _rootServices;

    private readonly int _maxStackSize;
    private readonly int _maxBackStackCachedDepth;
    private readonly int _maxForwardStackCachedDepth;

    private readonly Stack<(ContentDialog Dialog, TaskCompletionSource Tcs)> _dialogStack = [];

    private List<ConcreteRoute> _routeStack = [];
    private int _currentRouteIndex = -1;

    private bool _isNavigating;
    private bool _isRedirecting;
    private bool _isDisposed;

    private bool _blockNavigation;
    private bool _blockDialogs;

    /// <summary>
    /// Initializes a new instance of the <see cref="Navigator"/> class using the specified content control for displaying the active view and mappings provided
    /// in the build action.
    /// </summary>
    public Navigator(ContentControl contentControl, Action<NavigatorBuilder> buildAction)
        : this(new ContentControlNavigator(contentControl), buildAction) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Navigator"/> class with the specified root view navigator and mappings provided in the build action.
    /// </summary>
    public Navigator(ViewNavigator viewNavigator, Action<NavigatorBuilder> buildAction)
    {
        _viewNavigator = viewNavigator;
        EnsureThreadAccess();
        TaskRunner = new TaskRunner(busy => _viewNavigator.NavigationControl.IsEnabled = !busy);

        var builder = new NavigatorBuilder();
        buildAction(builder);
        builder.Validate();

        _routeParts = [.. builder.RouteParts];
        _viewModelTypeToMappingInfo = builder.ViewModelTypeToMappingInfo.ToFrozenDictionary();

        var vmTypeToDialogActivator = builder.ViewModelTypeToDialogActivator.AsEnumerable();

        if (!builder.ViewModelTypeToDialogActivator.ContainsKey(typeof(MessageDialogViewModel)))
            vmTypeToDialogActivator = vmTypeToDialogActivator.Append(new(typeof(MessageDialogViewModel), () => new MessageDialog()));

        _viewModelTypeToDialogActivator = vmTypeToDialogActivator.ToFrozenDictionary();

        _rootServices = builder.Services;

        _maxStackSize = builder.MaxNavigationStacksSize + 1; // +1 to account for the current route
        _maxBackStackCachedDepth = builder.MaxBackStackCachedDepth;
        _maxForwardStackCachedDepth = builder.MaxForwardStackCachedDepth;
    }

    /// <inheritdoc cref="INavigator.CanGoBack"/>
    public bool CanGoBack
    {
        get {
            EnsureThreadAccess();
            return !IsNavigating && !IsShowingDialog && HasBackHistory;
        }
    }

    /// <inheritdoc cref="INavigator.CanGoForward"/>
    public bool CanGoForward
    {
        get {
            EnsureThreadAccess();
            return !IsNavigating && !IsShowingDialog && HasForwardHistory;
        }
    }

    /// <inheritdoc cref="INavigator.CanRefresh"/>
    public bool CanRefresh
    {
        get {
            EnsureThreadAccess();
            return !IsNavigating && !IsShowingDialog && CurrentRouteImpl is not null;
        }
    }

    /// <inheritdoc/>
    public IConcreteRoute CurrentRoute
    {
        get {
            EnsureThreadAccess();
            return CurrentRouteImpl ?? EmptyRoute;
        }
    }

    /// <inheritdoc cref="INavigator.HasBackHistory"/>
    public bool HasBackHistory
    {
        get {
            EnsureThreadAccess();
            return _currentRouteIndex > 0;
        }
    }

    /// <inheritdoc cref="INavigator.HasForwardHistory"/>
    public bool HasForwardHistory
    {
        get {
            EnsureThreadAccess();
            return _currentRouteIndex < _routeStack.Count - 1;
        }
    }

    /// <inheritdoc cref="INavigator.IsNavigating"/>
    public bool IsNavigating
    {
        get {
            EnsureThreadAccess();
            return _isNavigating;
        }
    }

    /// <inheritdoc cref="INavigator.IsShowingDialog"/>
    public bool IsShowingDialog
    {
        get {
            EnsureThreadAccess();
            return _dialogStack.Count > 0;
        }
    }

    /// <inheritdoc cref="INavigator.Services"/>
    public IServiceProvider Services => _rootServices;

    /// <inheritdoc cref="INavigator.TaskRunner"/>
    public ITaskRunner TaskRunner { get; }

    private ConcreteRoute? CurrentRouteImpl => _currentRouteIndex >= 0 ? _routeStack[_currentRouteIndex] : null;

    /// <inheritdoc cref="INavigator.ClearHistory"/>
    public async ValueTask ClearHistory()
    {
        EnsureThreadAccess();

        if (_routeStack.Count is 0)
            return;

        var currentRoute = _routeStack[_currentRouteIndex];
        var removedRoutes = _routeStack;

        using (EnterNavigationGuard(blockDialogs: true))
        {
            using (new PropertyChangedNotifier(this))
            {
                removedRoutes.RemoveAt(_currentRouteIndex);
                _routeStack = [currentRoute];
                _currentRouteIndex = 0;
            }

            await TrimRoutesAndCacheAsync(removedRoutes);
        }
    }

    /// <summary>
    /// Disposes the navigator and all cached views and view models.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
            return;

        EnsureThreadAccess();

        if (_dialogStack.Count > 0)
            throw new InvalidOperationException("Cannot dispose the navigator while dialogs are showing.");

        _isDisposed = true;

        await TaskRunner.WaitForIdleAsync(true);

        _blockDialogs = true;
        _blockNavigation = true;

        _viewNavigator.SetActiveView(null);

        if (CurrentRouteImpl is { } currentRoute)
        {
            foreach (var routeItem in currentRoute.Items.Reverse())
            {
                if (routeItem.AlreadyNavigatedTo)
                    await routeItem.ViewModel.OnNavigatedAwayAsync();
            }
        }

        var removedRoutes = _routeStack;

        _routeStack.Clear();
        _currentRouteIndex = -1;

        PropertyChanged = null;

        await TaskRunner.WaitForIdleAsync(true);
        await TrimRoutesAndCacheAsync(removedRoutes);
    }

    /// <inheritdoc cref="INavigator.GetBackStack"/>
    public IReadOnlyList<IConcreteRoute> GetBackStack()
    {
        EnsureThreadAccess();

        if (_currentRouteIndex <= 0)
            return [];

        var stack = _routeStack[.._currentRouteIndex];
        stack.Reverse();

        return stack;
    }

    /// <inheritdoc cref="INavigator.GetForwardStack"/>
    public IReadOnlyList<IConcreteRoute> GetForwardStack()
    {
        EnsureThreadAccess();

        if (_currentRouteIndex >= _routeStack.Count - 1)
            return [];

        return _routeStack[(_currentRouteIndex + 1)..];
    }

    private void EnsureThreadAccess()
    {
        if (_viewNavigator.NavigationControl.DispatcherQueue?.HasThreadAccess is not true)
            throw new InvalidOperationException("Navigator can only be accessed from the UI thread.");

        if (_isDisposed)
            throw new ObjectDisposedException(nameof(Navigator), "Navigator has been disposed.");
    }

    private bool CloseLightDismissPopups()
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

    private async ValueTask TrimRoutesAndCacheAsync(List<ConcreteRoute>? removedRoutes)
    {
        if (_routeStack.Count > _maxStackSize)
        {
            int trimCount = _routeStack.Count - _maxStackSize;
            _routeStack.RemoveRange(0, trimCount);
            _currentRouteIndex -= trimCount;
        }

        if (_routeStack.Count <= 1)
            return;

        var keepMaterialized = new HashSet<ConcreteRoute.Item>(_routeStack.Count * 3);

        int cachedStartIndex = Math.Max(0, _currentRouteIndex - _maxBackStackCachedDepth);
        int cachedEndIndex = Math.Min(_routeStack.Count - 1, _currentRouteIndex + _maxForwardStackCachedDepth);

        // Keep all materialized components on route items that can be cached within the cached range

        for (int j = cachedStartIndex; j <= cachedEndIndex; j++)
        {
            var route = _routeStack[j];

            foreach (var item in route.Items)
            {
                // Always keep views from the current route

                if (item.IsMaterialized && (j == _currentRouteIndex || item.ViewModel.CanBeCached))
                    keepMaterialized.Add(item);
            }
        }

        // Dispose all materialized items in routes that are not in the keepMaterialized set

        var routes = _routeStack.AsEnumerable();

        if (removedRoutes is not null)
            routes = routes.Concat(removedRoutes);

        foreach (var route in routes)
        {
            bool forceDisposeChildren = false;

            foreach (var routeItem in route.Items)
            {
                if (routeItem.IsMaterialized && (forceDisposeChildren || !keepMaterialized.Contains(routeItem)))
                {
                    // If a disposed item has dependent children, we need to dispose its children as well since they may hold references to the disposed parent
                    // or disposed services that the parent provided.

                    if (routeItem.HasDependentChildren)
                        forceDisposeChildren = true;

                    await routeItem.DisposeMaterializedComponents();
                }
            }
        }
    }
}
