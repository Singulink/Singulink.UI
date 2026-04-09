using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Singulink.UI.Navigation;

/// <content>
/// Provides navigation related implementations for the navigator.
/// </content>
partial class NavigatorCore
{
    /// <inheritdoc cref="INavigator.NavigateAsync(string)"/>
    public async Task<NavigationResult> NavigateAsync(string route)
    {
        EnsureThreadAccess();

        string? anchor = null;
        int anchorIndex = route.IndexOf('#');

        if (anchorIndex >= 0)
        {
            anchor = Uri.UnescapeDataString(route[(anchorIndex + 1)..]);
            route = route[..anchorIndex];
        }

        RouteQuery query = RouteQuery.Empty;
        int queryIndex = route.IndexOf('?');

        if (queryIndex >= 0)
        {
            query = RouteQuery.Parse(route[(queryIndex + 1)..]);
            route = route[..queryIndex];
        }

        if (!TryMatchRoute(route, query, out var routeItems))
            throw new ArgumentException($"No route found for '{route}'.", nameof(route));

        return await NavigateNewAsyncCore(routeItems, anchor);
    }

    /// <inheritdoc cref="INavigator.NavigateAsync(IConcreteRootRoutePart, string?)"/>
    public async Task<NavigationResult> NavigateAsync(IConcreteRootRoutePart rootRoutePart, string? anchor = null)
    {
        EnsureThreadAccess();
        return await NavigateNewWithRouteCheckAsync([rootRoutePart], anchor);
    }

    /// <inheritdoc cref="INavigator.NavigateAsync{TRootViewModel}(IConcreteRootRoutePart{TRootViewModel}, IConcreteChildRoutePart{TRootViewModel}, string?)"/>
    public async Task<NavigationResult> NavigateAsync<TRootViewModel>(
        IConcreteRootRoutePart<TRootViewModel> rootRoutePart,
        IConcreteChildRoutePart<TRootViewModel> childRoutePart,
        string? anchor = null)
        where TRootViewModel : class
    {
        EnsureThreadAccess();
        return await NavigateNewWithRouteCheckAsync([rootRoutePart, childRoutePart], anchor);
    }

    /// <inheritdoc cref="INavigator.NavigateAsync{TRootViewModel, TChildViewModel1}(IConcreteRootRoutePart{TRootViewModel}, IConcreteChildRoutePart{TRootViewModel, TChildViewModel1}, IConcreteChildRoutePart{TChildViewModel1}, string?)"/>
    public async Task<NavigationResult> NavigateAsync<TRootViewModel, TChildViewModel1>(
        IConcreteRootRoutePart<TRootViewModel> rootRoutePart,
        IConcreteChildRoutePart<TRootViewModel, TChildViewModel1> childRoutePart1,
        IConcreteChildRoutePart<TChildViewModel1> childRoutePart2,
        string? anchor = null)
        where TRootViewModel : class
        where TChildViewModel1 : class
    {
        EnsureThreadAccess();
        return await NavigateNewWithRouteCheckAsync([rootRoutePart, childRoutePart1, childRoutePart2], anchor);
    }

    /// <inheritdoc cref="INavigator.NavigateAsync{TRootViewModel, TChildViewModel1, TChildViewModel2}(IConcreteRootRoutePart{TRootViewModel}, IConcreteChildRoutePart{TRootViewModel, TChildViewModel1}, IConcreteChildRoutePart{TChildViewModel1, TChildViewModel2}, IConcreteChildRoutePart{TChildViewModel2}, string?)"/>
    public async Task<NavigationResult> NavigateAsync<TRootViewModel, TChildViewModel1, TChildViewModel2>(
        IConcreteRootRoutePart<TRootViewModel> rootRoutePart,
        IConcreteChildRoutePart<TRootViewModel, TChildViewModel1> childRoutePart1,
        IConcreteChildRoutePart<TChildViewModel1, TChildViewModel2> childRoutePart2,
        IConcreteChildRoutePart<TChildViewModel2> childRoutePart3,
        string? anchor = null)
        where TRootViewModel : class
        where TChildViewModel1 : class
        where TChildViewModel2 : class
    {
        EnsureThreadAccess();
        return await NavigateNewWithRouteCheckAsync([rootRoutePart, childRoutePart1, childRoutePart2, childRoutePart3], anchor);
    }

    /// <inheritdoc cref="INavigator.NavigatePartialAsync(string?)"/>
    public async Task<NavigationResult> NavigatePartialAsync(string? anchor)
    {
        EnsureThreadAccess();
        return await NavigateNewAsyncCore(null, anchor);
    }

    /// <inheritdoc cref="INavigator.NavigatePartialAsync{TParentViewModel}(IConcreteChildRoutePart{TParentViewModel}, string?)"/>
    public async Task<NavigationResult> NavigatePartialAsync<TParentViewModel>(
        IConcreteChildRoutePart<TParentViewModel> childRoutePart,
        string? anchor = null)
        where TParentViewModel : class
    {
        return await NavigatePartialAsync(typeof(TParentViewModel), [childRoutePart], anchor);
    }

    /// <inheritdoc cref="INavigator.NavigatePartialAsync{TParentViewModel, TChildViewModel1}(IConcreteChildRoutePart{TParentViewModel, TChildViewModel1}, IConcreteChildRoutePart{TChildViewModel1}, string?)"/>
    public async Task<NavigationResult> NavigatePartialAsync<TParentViewModel, TChildViewModel1>(
        IConcreteChildRoutePart<TParentViewModel, TChildViewModel1> childRoutePart1,
        IConcreteChildRoutePart<TChildViewModel1> childRoutePart2,
        string? anchor = null)
        where TParentViewModel : class
        where TChildViewModel1 : class
    {
        return await NavigatePartialAsync(typeof(TParentViewModel), [childRoutePart1, childRoutePart2], anchor);
    }

    /// <inheritdoc cref="INavigator.NavigatePartialAsync{TParentViewModel, TChildViewModel1, TChildViewModel2}(IConcreteChildRoutePart{TParentViewModel, TChildViewModel1}, IConcreteChildRoutePart{TChildViewModel1, TChildViewModel2}, IConcreteChildRoutePart{TChildViewModel2}, string?)"/>
    public async Task<NavigationResult> NavigatePartialAsync<TParentViewModel, TChildViewModel1, TChildViewModel2>(
        IConcreteChildRoutePart<TParentViewModel, TChildViewModel1> childRoutePart1,
        IConcreteChildRoutePart<TChildViewModel1, TChildViewModel2> childRoutePart2,
        IConcreteChildRoutePart<TChildViewModel2> childRoutePart3,
        string? anchor = null)
        where TParentViewModel : class
        where TChildViewModel1 : class
        where TChildViewModel2 : class
    {
        return await NavigatePartialAsync(typeof(TParentViewModel), [childRoutePart1, childRoutePart2, childRoutePart3], anchor);
    }

    private async Task<NavigationResult> NavigatePartialAsync(Type parentViewModelType, List<IConcreteRoutePart> requestedChildRoutes, string? anchor)
    {
        var routeParts = GetCurrentRoutePartsToParent(parentViewModelType)
            .Concat(requestedChildRoutes)
            .ToList();

        return await NavigateNewWithRouteCheckAsync(routeParts, anchor);
    }

    /// <inheritdoc cref="INavigator.NavigateToParentAsync{TParentViewModel}(string?)"/>
    public Task<NavigationResult> NavigateToParentAsync<TParentViewModel>(string? anchor = null)
        where TParentViewModel : class
    {
        EnsureThreadAccess();
        return NavigateToParentAsync(typeof(TParentViewModel), anchor);
    }

    private async Task<NavigationResult> NavigateToParentAsync(Type parentViewModelType, string? anchor = null)
    {
        var currentRoute = CurrentRouteCore ?? throw new InvalidOperationException("Cannot navigate to parent before the navigator has a route.");

        var routeParts = currentRoute.Items
            .Take(currentRoute.Items.Count - 1)
            .Select(ri => ri.ConcreteRoutePart)
            .ToList();

        int parentIndex = routeParts.FindLastIndex(rp => rp.RoutePart.ViewModelType == parentViewModelType);

        if (parentIndex < 0)
            throw new NavigationRouteException($"Current route does not contain a parent view model of type '{parentViewModelType}'.");

        return await NavigateNewAsyncCore(routeParts[..(parentIndex + 1)], anchor);
    }

    /// <inheritdoc cref="INavigator.GoBackAsync()"/>
    public async Task<NavigationResult> GoBackAsync()
    {
        EnsureThreadAccess();

        if (!HasBackHistory)
            throw new InvalidOperationException("Cannot navigate back when the back history is empty.");

        var route = _routeStack[_currentRouteIndex - 1];

        return await NavigateAsyncCore(NavigationType.Back, route, () => {
            _currentRouteIndex--;
            return null;
        });
    }

    /// <inheritdoc cref="INavigator.GoForwardAsync"/>
    public async Task<NavigationResult> GoForwardAsync()
    {
        EnsureThreadAccess();

        if (!HasForwardHistory)
            throw new InvalidOperationException("Cannot navigate forward when the forward history is empty.");

        var route = _routeStack[_currentRouteIndex + 1];

        return await NavigateAsyncCore(NavigationType.Forward, route, () => {
            _currentRouteIndex++;
            return null;
        });
    }

    /// <inheritdoc cref="INavigator.RefreshAsync"/>
    public async Task<NavigationResult> RefreshAsync()
    {
        EnsureThreadAccess();

        var route = CurrentRouteCore ?? throw new InvalidOperationException("Cannot refresh before the navigator has a route.");
        return await NavigateAsyncCore(NavigationType.Refresh, route, null);
    }

    /// <inheritdoc cref="INavigator.HandleSystemBackRequest()"/>
    public bool HandleSystemBackRequest()
    {
        EnsureThreadAccess();

        if (CloseLightDismissPopups() || _isNavigating)
            return true;

        if (IsShowingDialogCore)
        {
            TryDismissTopDialog();
            return true;
        }

        if (_currentRouteIndex <= 0)
            return false;

        TaskRunner.RunAndForget(GoBackAsync());

        return true;
    }

    /// <inheritdoc cref="INavigator.HandleSystemForwardRequest()"/>
    public bool HandleSystemForwardRequest()
    {
        EnsureThreadAccess();

        if (IsShowingDialogCore || _isNavigating)
            return true;

        if (_currentRouteIndex >= _routeStack.Count - 1)
            return false;

        TaskRunner.RunAndForget(GoForwardAsync());

        return true;
    }

    /// <inheritdoc cref="INavigator.UpdateCurrentRoute(string?)"/>
    public void UpdateCurrentRoute(string? anchor)
    {
        EnsureThreadAccess();

        var currentRoute = CurrentRouteCore ?? throw new InvalidOperationException("Cannot update route options before the navigator has a route.");

        if (currentRoute.Anchor == anchor)
            return;

        using var notifier = new PropertyChangedNotifier(this);
        _routeStack[_currentRouteIndex] = new NavigatorRoute(currentRoute.Items, anchor);
    }

    /// <inheritdoc cref="INavigator.UpdateCurrentRoute(IConcreteRoutePart, string?)"/>
    public void UpdateCurrentRoute(IConcreteRoutePart concreteRoutePart, string? anchor = null)
    {
        EnsureThreadAccess();

        var currentRoute = CurrentRouteCore ?? throw new InvalidOperationException("Cannot update the current route before the navigator has a route.");

        var lastItem = currentRoute.Items[^1];

        if (concreteRoutePart.RoutePart.ViewModelType != lastItem.ViewModelType)
        {
            throw new ArgumentException(
                $"The view model type '{concreteRoutePart.RoutePart.ViewModelType}' of the specified route part does not match " +
                $"the view model type '{lastItem.ViewModelType}' of the last route part in the current route.",
                nameof(concreteRoutePart));
        }

        if (lastItem.ConcreteRoutePart.Equals(concreteRoutePart) && currentRoute.Anchor == anchor)
            return;

        lastItem.ConcreteRoutePart = concreteRoutePart;

        using var notifier = new PropertyChangedNotifier(this);
        _routeStack[_currentRouteIndex] = new NavigatorRoute(currentRoute.Items, anchor);
    }

    /// <inheritdoc cref="INavigator.ClearHistoryAsync"/>
    public async ValueTask ClearHistoryAsync()
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

    /// <inheritdoc cref="INavigator.GetBackStack"/>
    public IReadOnlyList<NavigatorRoute> GetBackStack()
    {
        EnsureThreadAccess();

        if (_currentRouteIndex <= 0)
            return [];

        var stack = _routeStack[.._currentRouteIndex];
        stack.Reverse();

        return stack;
    }

    /// <inheritdoc cref="INavigator.GetForwardStack"/>
    public IReadOnlyList<NavigatorRoute> GetForwardStack()
    {
        EnsureThreadAccess();

        if (_currentRouteIndex >= _routeStack.Count - 1)
            return [];

        return _routeStack[(_currentRouteIndex + 1)..];
    }

    /// <inheritdoc cref="INavigator.CurrentRouteHasParent{TViewModel}"/>
    public bool CurrentRouteHasParent<TViewModel>()
    {
        EnsureThreadAccess();

        var currentRoute = CurrentRouteCore;

        if (currentRoute is null)
            return false;

        return currentRoute.Items
            .Take(currentRoute.Items.Count - 1)
            .Any(ri => ri.ConcreteRoutePart.RoutePart.ViewModelType == typeof(TViewModel));
    }

    /// <inheritdoc cref="INavigator.GetCurrentRoutePartsToParent(Type)"/>
    public IEnumerable<IConcreteRoutePart> GetCurrentRoutePartsToParent(Type parentViewModelType)
    {
        EnsureThreadAccess();

        var currentRoute = CurrentRouteCore ?? throw new InvalidOperationException("Cannot perform partial routing operations before the navigator has a route.");

        int parentRouteItemIndex = -1;

        for (int i = currentRoute.Items.Count - 1; i >= 0; i--)
        {
            if (currentRoute.Items[i].ViewModelType == parentViewModelType)
            {
                parentRouteItemIndex = i;
                break;
            }
        }

        if (parentRouteItemIndex < 0)
            throw new NavigationRouteException($"Current route does not contain a parent view model of type '{parentViewModelType}'.");

        return currentRoute.Items
            .Take(parentRouteItemIndex + 1)
            .Select(ri => ri.ConcreteRoutePart);
    }

    /// <inheritdoc cref="INavigator.CurrentPathStartsWith{TRootViewModel}(IConcreteRootRoutePart{TRootViewModel}, IConcreteChildRoutePart{TRootViewModel})"/>
    public bool CurrentPathStartsWith<TRootViewModel>(
        IConcreteRootRoutePart<TRootViewModel> rootRoutePart,
        IConcreteChildRoutePart<TRootViewModel> childRoutePart)
        where TRootViewModel : class
    {
        EnsureThreadAccess();
        return CurrentPathStartsWith([rootRoutePart, childRoutePart]);
    }

    /// <inheritdoc cref="INavigator.CurrentPathStartsWith{TRootViewModel, TChildViewModel1}(IConcreteRootRoutePart{TRootViewModel}, IConcreteChildRoutePart{TRootViewModel, TChildViewModel1}, IConcreteChildRoutePart{TChildViewModel1})"/>
    public bool CurrentPathStartsWith<TRootViewModel, TChildViewModel1>(
        IConcreteRootRoutePart<TRootViewModel> rootRoutePart,
        IConcreteChildRoutePart<TRootViewModel, TChildViewModel1> childRoutePart1,
        IConcreteChildRoutePart<TChildViewModel1> childRoutePart2)
        where TRootViewModel : class
        where TChildViewModel1 : class
    {
        EnsureThreadAccess();
        return CurrentPathStartsWith([rootRoutePart, childRoutePart1, childRoutePart2]);
    }

    /// <inheritdoc cref="INavigator.CurrentPathStartsWith{TRootViewModel, TChildViewModel1, TChildViewModel2}(IConcreteRootRoutePart{TRootViewModel}, IConcreteChildRoutePart{TRootViewModel, TChildViewModel1}, IConcreteChildRoutePart{TChildViewModel1, TChildViewModel2}, IConcreteChildRoutePart{TChildViewModel2})"/>
    public bool CurrentPathStartsWith<TRootViewModel, TChildViewModel1, TChildViewModel2>(
        IConcreteRootRoutePart<TRootViewModel> rootRoutePart,
        IConcreteChildRoutePart<TRootViewModel, TChildViewModel1> childRoutePart1,
        IConcreteChildRoutePart<TChildViewModel1, TChildViewModel2> childRoutePart2,
        IConcreteChildRoutePart<TChildViewModel2> childRoutePart3)
        where TRootViewModel : class
        where TChildViewModel1 : class
        where TChildViewModel2 : class
    {
        EnsureThreadAccess();
        return CurrentPathStartsWith([rootRoutePart, childRoutePart1, childRoutePart2, childRoutePart3]);
    }

    private bool CurrentPathStartsWith(List<IConcreteRoutePart> routeParts)
    {
        string? current = CurrentRouteCore?.Path;

        if (current is null)
            return false;

        string partial = Route.GetRoute(routeParts);

        if (!current.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
            return false;

        return partial.Length == current.Length || current[partial.Length] is '/';
    }

    /// <inheritdoc cref="INavigator.TryShutDownAsync"/>
    public async Task<bool> TryShutDownAsync()
    {
        EnsureThreadAccess();

        if (_isNavigating || IsShowingDialogCore || TaskRunner.IsBusy)
            return false;

        using var notifier = new PropertyChangedNotifier(this);

        _isNavigating = true;

        try
        {
            CloseLightDismissPopups();
            notifier.Update();

            bool navigatedAway = await TaskRunner.RunAsBusyAsync(
                NavigateAwayAsyncCore(NavigationType.New, numRouteItemsToKeep: 0, notifier, () => {
                    var removedRoutes = _routeStack.ToList();
                    _routeStack.Clear();
                    _currentRouteIndex = -1;

                    return removedRoutes;
                }));

            if (!navigatedAway)
                return false;

            PropertyChanged = null;
            _blockDialogs = true;
            _blockNavigation = true;

            await TaskRunner.WaitForIdleAsync(waitForNonBusyTasks: false);
            return true;
        }
        finally
        {
            _isNavigating = false;
        }
    }

    private Task<NavigationResult> NavigateNewWithRouteCheckAsync(IReadOnlyList<IConcreteRoutePart> routeParts, string? anchor)
    {
        EnsureNonLeafPartsHaveNoQuery(routeParts);
        EnsureRoutePartsResolveToExpectedRoute(routeParts);
        return NavigateNewAsyncCore(routeParts, anchor);
    }

    private async Task<NavigationResult> NavigateNewAsyncCore(IReadOnlyList<IConcreteRoutePart>? routeParts, string? anchor)
    {
        var currentRoute = CurrentRouteCore;
        var routeItems = routeParts is not null ? BuildRouteItems(routeParts) : currentRoute?.Items ??
            throw new InvalidOperationException("Cannot perform partial routing operations before the navigator has a route.");

        var route = currentRoute is not null && routeItems.SequenceEqual(currentRoute.Items) && anchor == currentRoute.Anchor ?
            currentRoute : new NavigatorRoute(routeItems, anchor);

        return await NavigateAsyncCore(NavigationType.New, route, () => {
            List<NavigatorRoute>? removedRoutes = null;

            if (_currentRouteIndex < _routeStack.Count - 1)
            {
                removedRoutes = _routeStack.Skip(_currentRouteIndex + 1).ToList();
                _routeStack.RemoveRange(_currentRouteIndex + 1, removedRoutes.Count);
            }

            if (_isRedirecting && _currentRouteIndex > 0)
            {
                (removedRoutes ??= []).Add(_routeStack[_currentRouteIndex]);
                _routeStack[_currentRouteIndex] = route;
            }
            else if (route != CurrentRouteCore)
            {
                _routeStack.Add(route);
                _currentRouteIndex = _routeStack.Count - 1;
            }

            return removedRoutes;
        });

        IReadOnlyList<NavigationItem> BuildRouteItems(IReadOnlyList<IConcreteRoutePart> routeParts)
        {
            var commonRouteCandidates = _routeStack;
            int i; // number of common route parts

            for (i = 0; i < routeParts.Count; i++)
            {
                var newCandidates = commonRouteCandidates
                    .Where(ri => i < ri.Items.Count && ri.Items[i].ConcreteRoutePart.Equals(routeParts[i]))
                    .ToList();

                if (newCandidates.Count is 0)
                    break;

                commonRouteCandidates = newCandidates;
            }

            var routeItems = new List<NavigationItem>(routeParts.Count);

            // Copy common items

            if (i > 0)
                routeItems.AddRange(commonRouteCandidates[0].Items.Take(i));

            // Create remaining items

            for (; i < routeParts.Count; i++)
            {
                var parentRouteItem = i > 0 ? routeItems[i - 1] : null;
                var routePart = routeParts[i];
                var viewModelType = routePart.RoutePart.ViewModelType;

                routeItems.Add(new(parentRouteItem, routePart, viewModelType));
            }

            return routeItems;
        }
    }

    private async Task<NavigationResult> NavigateAsyncCore(
        NavigationType navigationType, NavigatorRoute route, Func<List<NavigatorRoute>?>? updateRouteStackAndGetRemovedRoutes)
    {
        if (IsShowingDialogCore)
            throw new InvalidOperationException("Cannot navigate while a dialog is shown.");

        if (_blockNavigation)
            throw new InvalidOperationException("Navigation requested at an invalid time while navigation is blocked.");

        using var notifier = new PropertyChangedNotifier(this);

        bool wasNavigating = _isNavigating;
        _isNavigating = true;

        try
        {
            CloseLightDismissPopups();
            notifier.Update();

            return await TaskRunner.RunAsBusyAsync(DoNavigateAsync);
        }
        finally
        {
            _isNavigating = wasNavigating;
        }

        async Task<NavigationResult> DoNavigateAsync()
        {
            var currentRoute = CurrentRouteCore ?? EmptyRoute;

            int numRouteItemsToKeep = currentRoute.Items.Zip(route.Items)
                .TakeWhile(pair => pair.First == pair.Second)
                .Count();

            if (!await NavigateAwayAsyncCore(navigationType, numRouteItemsToKeep, notifier, updateRouteStackAndGetRemovedRoutes))
                return NavigationResult.Cancelled;

            object? viewNavigator = _rootViewNavigator;

            for (int i = 0; i < route.Items.Count; i++)
            {
                var routeItem = route.Items[i];
                bool hasChildNavigation = i < route.Items.Count - 1;

                if (viewNavigator is null)
                {
                    throw new InvalidOperationException(
                        $"Cannot navigate to view model of type '{routeItem.ViewModelType}' because the parent view navigator is null " +
                        $"(view model '{route.Items[i - 1].ViewModelType}').");
                }

                using (EnterNavigationGuard(blockDialogs: true))
                {
                    EnsureMaterialized(routeItem);
                    SetActiveView(viewNavigator, routeItem.View);
                }

                var args = new NavigationArgs(this, navigationType, hasChildNavigation);

                using (EnterNavigationGuard(blockDialogs: false))
                {
                    void EnsureDialogsClosed()
                    {
                        if (hasChildNavigation && IsShowingDialogCore)
                        {
                            throw new InvalidOperationException(
                                $"All dialogs must be closed before completing navigated event tasks with child navigations " +
                                $"(view model '{routeItem.ViewModel.GetType()}').");
                        }
                    }

                    if (!routeItem.AlreadyNavigatedTo)
                    {
                        routeItem.AlreadyNavigatedTo = true;
                        await routeItem.ViewModel!.OnNavigatedToAsync(args);
                        EnsureDialogsClosed();
                    }

                    if (args.Redirect is null)
                    {
                        await routeItem.ViewModel.OnRouteNavigatedAsync(args);
                        EnsureDialogsClosed();
                    }
                }

                if (args.Redirect is not null)
                {
                    bool wasRedirecting = _isRedirecting;

                    try
                    {
                        _isRedirecting = true;
                        return await args.Redirect.ExecuteAsync(this);
                    }
                    finally
                    {
                        _isRedirecting = wasRedirecting;
                    }
                }

                viewNavigator = routeItem.ChildViewNavigator;
            }

            return NavigationResult.Success;
        }
    }

    private async Task<bool> NavigateAwayAsyncCore(
        NavigationType navigationType,
        int numRouteItemsToKeep,
        PropertyChangedNotifier notifier,
        Func<List<NavigatorRoute>?>? updateRouteStackAndGetRemovedRoutes)
    {
        var currentRoute = CurrentRouteCore;

        if (!_isRedirecting && currentRoute is not null)
        {
            using (EnterNavigationGuard(blockDialogs: false))
            {
                notifier.Update();

                for (int i = currentRoute.Items.Count - 1; i >= 0; i--)
                {
                    var routeItem = currentRoute.Items[i];

                    if (routeItem.AlreadyNavigatedTo)
                    {
                        bool willNavigateAway = i >= numRouteItemsToKeep;
                        var args = new NavigatingArgs(this, navigationType);

                        void EnsureDialogsClosed()
                        {
                            if (IsShowingDialogCore)
                            {
                                throw new InvalidOperationException(
                                    $"All dialogs must be closed before completing navigating event tasks " +
                                    $"(view model '{routeItem.ViewModel.GetType()}').");
                            }
                        }

                        if (willNavigateAway)
                        {
                            await routeItem.ViewModel.OnNavigatingAwayAsync(args);
                            EnsureDialogsClosed();

                            if (args.Cancel)
                                return false;
                        }

                        await routeItem.ViewModel.OnRouteNavigatingAsync(args);
                        EnsureDialogsClosed();

                        if (args.Cancel)
                            return false;
                    }
                }
            }
        }

        using (EnterNavigationGuard(blockDialogs: true))
        {
            if (currentRoute is not null)
            {
                // Clear child view navigators for items being navigated away from
                for (int i = Math.Max(0, numRouteItemsToKeep - 1); i < currentRoute.Items.Count; i++)
                {
                    if (currentRoute.Items[i].ChildViewNavigator is { } childNavigator)
                        SetActiveView(childNavigator, null);
                }

                if (numRouteItemsToKeep is 0)
                    SetActiveView(_rootViewNavigator, null);

                foreach (var routeItem in currentRoute.Items.Skip(numRouteItemsToKeep).Reverse())
                {
                    if (routeItem.AlreadyNavigatedTo)
                    {
                        routeItem.AlreadyNavigatedTo = false;
                        await routeItem.ViewModel.OnNavigatedAwayAsync();
                    }
                }
            }

            if (updateRouteStackAndGetRemovedRoutes is not null)
            {
                var removedRoutes = updateRouteStackAndGetRemovedRoutes.Invoke();
                await TrimRoutesAndCacheAsync(removedRoutes);
            }

            notifier.Update();
        }

        return true;
    }

    private async ValueTask TrimRoutesAndCacheAsync(List<NavigatorRoute>? removedRoutes)
    {
        if (_routeStack.Count > _maxStackSize)
        {
            int trimCount = _routeStack.Count - _maxStackSize;
            _routeStack.RemoveRange(0, trimCount);
            _currentRouteIndex -= trimCount;
        }

        if (_routeStack.Count <= 1)
            return;

        var keepMaterialized = new HashSet<NavigationItem>(_routeStack.Count * 3);

        int cachedStartIndex = Math.Max(0, _currentRouteIndex - _maxBackStackCachedDepth);
        int cachedEndIndex = Math.Min(_routeStack.Count - 1, _currentRouteIndex + _maxForwardStackCachedDepth);

        // Keep all materialized components on route items that can be cached within the cached range

        for (int j = cachedStartIndex; j <= cachedEndIndex; j++)
        {
            var route = _routeStack[j];

            foreach (var item in route.Items)
            {
                // Always keep items from the current route

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

    private bool TryMatchRoute(string routeString, RouteQuery query, [MaybeNullWhen(false)] out List<IConcreteRoutePart> routeParts)
    {
        routeParts = [];

        if (TryMatchRoute(routeString, query, null, routeParts, out _))
        {
            routeParts.Reverse();
            return true;
        }

        return false;
    }

    private bool TryMatchRoute(ReadOnlySpan<char> routeString, RouteQuery query, Type? parentViewModelType, List<IConcreteRoutePart> routeParts, out ReadOnlySpan<char> rest)
    {
        foreach (var routePart in _routeParts)
        {
            if (routePart.ParentViewModelType != parentViewModelType)
                continue;

            var partQuery = _viewModelTypesWithChildren.Contains(routePart.ViewModelType) ? RouteQuery.Empty : query;

            if (!routePart.TryMatch(routeString, partQuery, out var concreteRoute, out rest))
                continue;

            if (rest.Length is 0 || TryMatchRoute(rest, query, routePart.ViewModelType, routeParts, out rest))
            {
                routeParts.Add(concreteRoute);
                return true;
            }
        }

        rest = routeString;
        return false;
    }

    private void EnsureNonLeafPartsHaveNoQuery(IReadOnlyList<IConcreteRoutePart> routeParts)
    {
        foreach (var routePart in routeParts)
        {
            if (routePart.Query.Count > 0 && _viewModelTypesWithChildren.Contains(routePart.RoutePart.ViewModelType))
            {
                throw new ArgumentException(
                    $"Route part '{routePart}' for view model '{routePart.RoutePart.ViewModelType}' has registered child routes but its route contains " +
                    $"query parameters ('{routePart.Query}'). Non-leaf route parts cannot have query parameters. This can happen when a " +
                    $"parameter model has optional properties that are not path holes in the route, or when a route group does not have a " +
                    $"candidate whose path holes cover all supplied parameters, causing extra values to overflow into the query string.");
            }
        }
    }

    private void EnsureRoutePartsResolveToExpectedRoute(IReadOnlyList<IConcreteRoutePart> routeParts)
    {
        string pathString = string.Join("/", routeParts.Select(p => p.Path).Where(p => p.Length > 0));
        var query = routeParts.Count > 0 ? routeParts[^1].Query : RouteQuery.Empty;

        if (!TryMatchRoute(pathString, query, out var foundRouteItems))
            throw new NavigationRouteException($"No route found for '{pathString}'.");

        if (!foundRouteItems.SequenceEqual(routeParts))
        {
            throw new NavigationRouteException(
                $"Route '{pathString}' matched a different route than what was specified. " +
                $"Routes may be misconfigured, missing or specified in an incorrect order.");
        }
    }

    private void EnsureMaterialized(NavigationItem item)
    {
        if (item.IsMaterialized)
            return;

        var mappingInfo = _viewModelTypeToMappingInfo[item.ViewModelType];
        object view = mappingInfo.CreateView();
        var viewModel = mappingInfo.CreateViewModel(this, item);

        WireView(view, viewModel, out object? childViewNavigator);

        item.ViewModel = viewModel;
        item.View = view;
        item.ChildViewNavigator = childViewNavigator;
    }
}
