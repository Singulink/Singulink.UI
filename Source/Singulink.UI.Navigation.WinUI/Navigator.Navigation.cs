using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Singulink.UI.Navigation.InternalServices;

namespace Singulink.UI.Navigation.WinUI;

/// <content>
/// Provides navigation related implementations for the navigator.
/// </content>
partial class Navigator
{
    /// <inheritdoc cref="INavigator.NavigateAsync(string)"/>
    public async Task<NavigationResult> NavigateAsync(string route)
    {
        EnsureThreadAccess();

        string anchor = null;
        int anchorIndex = route.IndexOf('#');

        if (anchorIndex >= 0)
        {
            anchor = Uri.UnescapeDataString(route[(anchorIndex + 1)..]);
            route = route[..anchorIndex];
        }

        // TODO: Implement support for query parameters

        int queryIndex = route.IndexOf('?');

        if (queryIndex >= 0)
            route = route[..queryIndex];

        if (!TryMatchRoute(route, out var routeItems))
            throw new ArgumentException($"No route found for '{route}'.", nameof(route));

        var routeOptions = anchor is not null ? new RouteOptions(anchor) : null;

        return await NavigateNewAsync(routeItems, routeOptions);
    }

    /// <inheritdoc cref="INavigator.NavigateAsync(IConcreteRootRoutePart, RouteOptions?)"/>
    public async Task<NavigationResult> NavigateAsync(IConcreteRootRoutePart rootRoutePart, RouteOptions? routeOptions = null)
    {
        EnsureThreadAccess();
        return await NavigateNewWithRouteCheckAsync([rootRoutePart], routeOptions);
    }

    /// <inheritdoc cref="INavigator.NavigateAsync{TRootViewModel}(IConcreteRootRoutePart{TRootViewModel}, IConcreteChildRoutePart{TRootViewModel}, RouteOptions?)"/>
    public async Task<NavigationResult> NavigateAsync<TRootViewModel>(
        IConcreteRootRoutePart<TRootViewModel> rootRoutePart,
        IConcreteChildRoutePart<TRootViewModel> childRoutePart,
        RouteOptions? routeOptions = null)
        where TRootViewModel : class
    {
        EnsureThreadAccess();
        return await NavigateNewWithRouteCheckAsync([rootRoutePart, childRoutePart], routeOptions);
    }

    /// <inheritdoc cref="INavigator.NavigateAsync{TRootViewModel, TChildViewModel1}(IConcreteRootRoutePart{TRootViewModel}, IConcreteChildRoutePart{TRootViewModel, TChildViewModel1}, IConcreteChildRoutePart{TChildViewModel1}, RouteOptions?)"/>
    public async Task<NavigationResult> NavigateAsync<TRootViewModel, TChildViewModel1>(
        IConcreteRootRoutePart<TRootViewModel> rootRoutePart,
        IConcreteChildRoutePart<TRootViewModel, TChildViewModel1> childRoutePart1,
        IConcreteChildRoutePart<TChildViewModel1> childRoutePart2,
        RouteOptions? routeOptions = null)
        where TRootViewModel : class
        where TChildViewModel1 : class
    {
        EnsureThreadAccess();
        return await NavigateNewWithRouteCheckAsync([rootRoutePart, childRoutePart1, childRoutePart2], routeOptions);
    }

    /// <inheritdoc cref="INavigator.NavigateAsync{TRootViewModel, TChildViewModel1, TChildViewModel2}(IConcreteRootRoutePart{TRootViewModel}, IConcreteChildRoutePart{TRootViewModel, TChildViewModel1}, IConcreteChildRoutePart{TChildViewModel1, TChildViewModel2}, IConcreteChildRoutePart{TChildViewModel2}, RouteOptions?)"/>
    public async Task<NavigationResult> NavigateAsync<TRootViewModel, TChildViewModel1, TChildViewModel2>(
        IConcreteRootRoutePart<TRootViewModel> rootRoutePart,
        IConcreteChildRoutePart<TRootViewModel, TChildViewModel1> childRoutePart1,
        IConcreteChildRoutePart<TChildViewModel1, TChildViewModel2> childRoutePart2,
        IConcreteChildRoutePart<TChildViewModel2> childRoutePart3,
        RouteOptions? routeOptions = null)
        where TRootViewModel : class
        where TChildViewModel1 : class
        where TChildViewModel2 : class
    {
        EnsureThreadAccess();
        return await NavigateNewWithRouteCheckAsync([rootRoutePart, childRoutePart1, childRoutePart2, childRoutePart3], routeOptions);
    }

    private Task<NavigationResult> NavigateNewWithRouteCheckAsync(IReadOnlyList<IConcreteRoutePart> routeParts, RouteOptions? routeOptions)
    {
        EnsureRoutePartsResolveToExpectedRoute(routeParts);
        return NavigateNewAsync(routeParts, routeOptions);
    }

    private async Task<NavigationResult> NavigateNewAsync(IReadOnlyList<IConcreteRoutePart>? routeParts, RouteOptions? routeOptions)
    {
        var currentRoute = CurrentRouteImpl;
        var routeItems = routeParts is not null ? BuildRouteItems(routeParts) : currentRoute?.Items ??
            throw new InvalidOperationException("Cannot navigate to a partial route before the navigator has a route.");

        var route = currentRoute is not null && routeItems.SequenceEqual(currentRoute.Items) && routeOptions == currentRoute.Options ?
            currentRoute : new ConcreteRoute(routeItems, routeOptions ?? RouteOptions.Empty);

        return await NavigateAsyncCore(NavigationType.New, route, () => {
            List<ConcreteRoute> removedRoutes = null;

            if (HasForwardHistory)
            {
                removedRoutes = _routeStack.Skip(_currentRouteIndex + 1).ToList();
                _routeStack.RemoveRange(_currentRouteIndex + 1, removedRoutes.Count);
            }

            if (_isRedirecting && _currentRouteIndex > 0)
            {
                (removedRoutes ??= []).Add(_routeStack[_currentRouteIndex]);
                _routeStack[_currentRouteIndex] = route;
            }
            else if (route != CurrentRouteImpl)
            {
                _routeStack.Add(route);
                _currentRouteIndex = _routeStack.Count - 1;
            }

            return removedRoutes;
        });

        IReadOnlyList<ConcreteRoute.Item> BuildRouteItems(IReadOnlyList<IConcreteRoutePart> routeParts)
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

            var routeItems = new List<ConcreteRoute.Item>(routeParts.Count);

            // Copy common items

            if (i > 0)
                routeItems.AddRange(commonRouteCandidates[0].Items.Take(i));

            // Create remaining items

            for (; i < routeParts.Count; i++)
            {
                var parentRouteItem = i > 0 ? routeItems[i - 1] : null;
                var routePart = routeParts[i];
                var mappingInfo = _viewModelTypeToMappingInfo[routePart.RoutePart.ViewModelType];

                routeItems.Add(new(parentRouteItem, routePart, mappingInfo));
            }

            return routeItems;
        }
    }

    private async Task<NavigationResult> NavigateAsyncCore(
        NavigationType navigationType, ConcreteRoute route, Func<List<ConcreteRoute>?>? updateRouteStack)
    {
        if (_dialogStack.Count > 0)
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
            var currentRoute = CurrentRouteImpl ?? EmptyRoute;

            int numCommonItems = currentRoute.Items.Zip(route.Items)
                .TakeWhile(pair => pair.First == pair.Second)
                .Count();

            if (!_isRedirecting)
            {
                using (EnterNavigationGuard(blockDialogs: false))
                {
                    notifier.Update();

                    for (int i = currentRoute.Items.Count - 1; i >= 0; i--)
                    {
                        var routeItem = currentRoute.Items[i];

                        if (routeItem.AlreadyNavigatedTo)
                        {
                            bool willNavigateAway = i >= numCommonItems;
                            var args = new NavigatingArgs(this, navigationType);

                            void EnsureDialogsClosed()
                            {
                                if (_dialogStack.Count > 0)
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
                                    return NavigationResult.Cancelled;
                            }

                            await routeItem.ViewModel.OnRouteNavigatingAsync(args);
                            EnsureDialogsClosed();

                            if (args.Cancel)
                                return NavigationResult.Cancelled;
                        }
                    }
                }
            }

            using (EnterNavigationGuard(blockDialogs: true))
            {
                foreach (var routeItem in currentRoute.Items.Skip(numCommonItems - 1))
                    routeItem.ChildViewNavigator?.SetActiveView(null);

                foreach (var routeItem in currentRoute.Items.Skip(numCommonItems).Reverse())
                {
                    if (routeItem.AlreadyNavigatedTo)
                    {
                        routeItem.AlreadyNavigatedTo = false;
                        await routeItem.ViewModel.OnNavigatedAwayAsync();
                    }
                }

                if (updateRouteStack is not null)
                {
                    var removedRoutes = updateRouteStack.Invoke();
                    await TrimRoutesAndCacheAsync(removedRoutes);
                }

                notifier.Update();
            }

            var viewNavigator = _viewNavigator;

            for (int i = 0; i < route.Items.Count; i++)
            {
                var routeItem = route.Items[i];
                bool hasChildNavigation = i < route.Items.Count - 1;

                using (EnterNavigationGuard(blockDialogs: true))
                {
                    if (viewNavigator is null)
                        throw new UnreachableException("Unexpected null child view navigator.");

                    routeItem.EnsureMaterialized(this);
                    viewNavigator.SetActiveView(routeItem.View);
                }

                var args = new NavigationArgs(this, navigationType, hasChildNavigation);

                using (EnterNavigationGuard(blockDialogs: false))
                {
                    void EnsureDialogsClosed()
                    {
                        if (hasChildNavigation && _dialogStack.Count > 0)
                        {
                            throw new InvalidOperationException(
                                $"All dialogs must be closed before completing navigated event tasks with child navigations " +
                                $"(view model '{routeItem.ViewModel.GetType()}').");
                        }
                    }

                    if (!routeItem.AlreadyNavigatedTo)
                    {
                        routeItem.AlreadyNavigatedTo = true;
                        await routeItem.ViewModel.OnNavigatedToAsync(args);
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

    private bool TryMatchRoute(string routeString, [MaybeNullWhen(false)] out List<IConcreteRoutePart> routeParts)
    {
        routeParts = [];

        if (TryMatchRoute(routeString, null, routeParts, out _))
        {
            routeParts.Reverse();
            return true;
        }

        return false;
    }

    private bool TryMatchRoute(ReadOnlySpan<char> routeString, Type? parentViewModelType, List<IConcreteRoutePart> routeParts, out ReadOnlySpan<char> rest)
    {
        foreach (var routePart in _routeParts)
        {
            if (routePart.ParentViewModelType != parentViewModelType || !routePart.TryMatch(routeString, out var concreteRoute, out rest))
                continue;

            if (rest.Length is 0 || TryMatchRoute(rest, routePart.ViewModelType, routeParts, out rest))
            {
                routeParts.Add(concreteRoute);
                return true;
            }
        }

        rest = routeString;
        return false;
    }

    private void EnsureRoutePartsResolveToExpectedRoute(IReadOnlyList<IConcreteRoutePart> routeParts)
    {
        string routeString = RoutingHelpers.GetPath(routeParts);

        if (!TryMatchRoute(routeString, out var foundRouteItems))
            throw new NavigationRouteException($"No route found for '{routeString}'.");

        if (!foundRouteItems.SequenceEqual(routeParts))
        {
            throw new NavigationRouteException(
                $"Route '{routeString}' matched a different route than what was specified. " +
                $"Routes may be misconfigured, missing or specified in an incorrect order.");
        }
    }
}
