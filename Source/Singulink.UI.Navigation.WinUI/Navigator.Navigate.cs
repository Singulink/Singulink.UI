using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.InteropServices;

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
        CloseLightDismissPopups();

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

        return await NavigateAsync(NavigationType.New, routeItems, routeOptions);
    }

    /// <inheritdoc cref="INavigator.NavigateAsync(IConcreteRootRoutePart, RouteOptions?)"/>
    public async Task<NavigationResult> NavigateAsync(IConcreteRootRoutePart rootRoutePart, RouteOptions? routeOptions = null)
    {
        EnsureThreadAccess();
        CloseLightDismissPopups();

        return await NavigateNewWithEnsureMatched([rootRoutePart], routeOptions);
    }

    /// <inheritdoc cref="INavigator.NavigateAsync{TRootViewModel}(IConcreteRootRoutePart{TRootViewModel}, IConcreteChildRoutePart{TRootViewModel}, RouteOptions?)"/>
    public async Task<NavigationResult> NavigateAsync<TRootViewModel>(
        IConcreteRootRoutePart<TRootViewModel> rootRoutePart,
        IConcreteChildRoutePart<TRootViewModel> childRoutePart,
        RouteOptions? routeOptions = null)
        where TRootViewModel : class
    {
        EnsureThreadAccess();
        CloseLightDismissPopups();

        return await NavigateNewWithEnsureMatched([rootRoutePart, childRoutePart], routeOptions);
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
        CloseLightDismissPopups();

        return await NavigateNewWithEnsureMatched([rootRoutePart, childRoutePart1, childRoutePart2], routeOptions);
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
        CloseLightDismissPopups();

        return await NavigateNewWithEnsureMatched([rootRoutePart, childRoutePart1, childRoutePart2, childRoutePart3], routeOptions);
    }

    private Task<NavigationResult> NavigateNewWithEnsureMatched(List<IConcreteRoutePart> routeParts, RouteOptions? routeOptions)
    {
        string routeString = GetPath(routeParts);

        if (!TryMatchRoute(routeString, out var foundRouteItems))
            throw new ArgumentException($"No route found for '{routeString}'.", nameof(routeParts));

        if (!foundRouteItems.SequenceEqual(routeParts))
        {
            string message = $"Route '{routeString}' matched a different route than what was specified. " +
                "Routes may be misconfigured, missing or specified in an incorrect order.";
            throw new ArgumentException(message, nameof(routeParts));
        }

        return NavigateAsync(NavigationType.New, routeParts, routeOptions);
    }

    private Task<NavigationResult> NavigateAsync(
        NavigationType navigationType,
        List<IConcreteRoutePart>? routeParts,
        RouteOptions? routeOptions)
    {
        if (_dialogTcsStack.Count > 0)
            throw new InvalidOperationException("Cannot navigate while a dialog is shown.");

        if (_blockNavigation)
            throw new InvalidOperationException("Navigation requested at an invalid time while navigation is blocked.");

        ConcreteRoute route;

        if (navigationType is NavigationType.New)
        {
            routeOptions ??= RouteOptions.Empty;

            if (routeParts is not null)
                route = BuildRoute(routeParts, routeOptions);
            else if (CurrentRouteInternal is not null)
                route = new ConcreteRoute(CurrentRouteInternal.Items, routeOptions);
            else
                throw new InvalidOperationException("Cannot navigate partial route when no route is currently active.");
        }
        else
        {
            Debug.Assert(routeParts is null, "Route parts should only be provided for new navigations.");
            Debug.Assert(routeOptions is null, "Route options should only be provided for new navigations.");

            if (navigationType is NavigationType.Back)
            {
                if (!HasBackHistory)
                    throw new InvalidOperationException("Cannot navigate back because there is no previous view.");

                route = _routeStack[_currentRouteIndex - 1];
            }
            else if (navigationType is NavigationType.Forward)
            {
                if (!HasForwardHistory)
                    throw new InvalidOperationException("Cannot navigate forward because there is no next view.");

                route = _routeStack[_currentRouteIndex + 1];
            }
            else if (navigationType is NavigationType.Refresh)
            {
                route = CurrentRouteInternal ?? throw new InvalidOperationException("Cannot refresh when there is no route is currently active.");
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(navigationType));
            }
        }

        return TaskRunner.RunAsBusyAsync(NavigateAsyncImpl(navigationType, route, routeOptions));

        ConcreteRoute BuildRoute(List<IConcreteRoutePart> routeItems, RouteOptions routeOptions)
        {
            var commonItemInfoCandidates = _routeStack;
            int i; // number of common items

            for (i = 0; i < routeItems.Count; i++)
            {
                var newCandidates = commonItemInfoCandidates
                    .Where(ri => i < ri.Items.Length && ri.Items[i].ConcreteRoutePart.Equals(routeItems[i]))
                    .ToList();

                if (newCandidates.Count is 0)
                    break;

                commonItemInfoCandidates = newCandidates;
            }

            var routeItem = new ConcreteRoute.Item[routeItems.Count];

            // Copy common items

            if (i > 0)
                commonItemInfoCandidates[0].Items.CopyTo(0, routeItem, 0, i);

            // Create remaining items

            for (; i < routeItem.Length; i++)
            {
                var item = routeItems[i];
                var createViewFunc = _viewModelTypeToViewInfo[item.RoutePart.ViewModelType].CreateView;
                routeItem[i] = new(item, createViewFunc);
            }

            return new ConcreteRoute(ImmutableCollectionsMarshal.AsImmutableArray(routeItem), routeOptions);
        }
    }

    private async Task<NavigationResult> NavigateAsyncImpl(
        NavigationType navigationType,
        ConcreteRoute route,
        RouteOptions? routeOptions)
    {
        using var notifier = new PropertyChangedNotifier(this, OnPropertyChanged);
        var lastRoute = CurrentRouteInternal;
        int numCommonItems = 0;

        if (lastRoute is not null)
        {
            for (int i = 0; i < lastRoute.Items.Length; i++)
            {
                numCommonItems = i;

                if (i >= route.Items.Length || lastRoute.Items[i] != route.Items[i])
                    break;
            }

            // Notify view models of navigating away from the last route, starting from the end of the route
            // If a navigation is already in progress, no need to notify it that it is navigating away since it cancelled itself.

            if (_navigationCts is null)
            {
                using (EnterNavigationGuard(alsoBlockDialogs: false))
                {
                    notifier.Update();

                    for (int i = lastRoute.Items.Length - 1; i >= 0; i--)
                    {
                        var routeItem = lastRoute.Items[i];

                        if (routeItem.AlreadyNavigatedTo)
                        {
                            var flags = i >= numCommonItems ? NavigatingFlags.WillBeNavigatedFrom : NavigatingFlags.None;
                            var args = new NavigatingArgs(navigationType, flags);
                            await routeItem.ViewModel.OnNavigatingFromAsync(args);

                            if (_dialogTcsStack.Count > 0)
                                throw new InvalidOperationException("All dialogs must be closed before completing navigating away from a view model.");

                            if (args.Cancel)
                                return NavigationResult.Cancelled;
                        }
                    }
                }
            }
        }

        bool cancelledLastNavigation = false;

        if (_navigationCts is not null)
        {
            _navigationCts.Cancel();
            cancelledLastNavigation = true;
        }

        var cancelNavigateToken = (_navigationCts = new()).Token;

        if (navigationType is NavigationType.New)
        {
            if (cancelledLastNavigation)
            {
                _routeStack[_currentRouteIndex] = route;
            }
            else
            {
                if (HasForwardHistory)
                    _routeStack.RemoveRange(_currentRouteIndex + 1, _routeStack.Count - _currentRouteIndex - 1);

                _routeStack.Add(route);
                _currentRouteIndex = _routeStack.Count - 1;
            }
        }
        else if (navigationType is NavigationType.Back)
        {
            _currentRouteIndex--;
        }
        else if (navigationType is NavigationType.Forward)
        {
            _currentRouteIndex++;
        }

        notifier.Update();

        if (lastRoute is not null)
        {
            using (EnterNavigationGuard(alsoBlockDialogs: true))
            {
                for (int i = lastRoute.Items.Length - 1; i >= numCommonItems; i--)
                {
                    var routeItem = lastRoute.Items[i];

                    if (routeItem.AlreadyNavigatedTo)
                    {
                        routeItem.ViewModel.OnNavigatedFrom();
                        routeItem.AlreadyNavigatedTo = false;
                    }
                }
            }
        }

        var viewNavigator = _viewNavigator;

        for (int i = 0; i < route.Items.Length; i++)
        {
            if (viewNavigator is null)
                throw new UnreachableException("Unexpected null child view navigator.");

            var routeItem = route.Items[i];
            bool hasChildNavigation = i < route.Items.Length - 1;

            using (EnterNavigationGuard(alsoBlockDialogs: true))
            {
                routeItem.EnsureViewCreatedAndModelInitialized(this);
                viewNavigator.SetActiveView(routeItem.View);
            }

            var navFlags = routeItem.IsFirstNavigation ? NavigationFlags.FirstNavigation : NavigationFlags.None;

            if (routeItem.AlreadyNavigatedTo)
                navFlags |= NavigationFlags.AlreadyNavigatedTo;

            if (hasChildNavigation)
                navFlags |= NavigationFlags.HasChildNavigation;

            var args = new NavigationArgs(navigationType, navFlags);
            await routeItem.ViewModel.OnNavigatedToAsync(args);

            routeItem.IsFirstNavigation = false;

            if (cancelNavigateToken.IsCancellationRequested)
                return NavigationResult.Rerouted;

            routeItem.AlreadyNavigatedTo = true;

            if (hasChildNavigation && _dialogTcsStack.Count > 0)
                throw new InvalidOperationException("All dialogs must be closed before completing navigation to a view with child navigations.");

            viewNavigator = routeItem.ChildViewNavigator;
        }

        _navigationCts = null;
        TrimRoutesAndCachedViews();

        return NavigationResult.Success;

        void TrimRoutesAndCachedViews()
        {
            if (_routeStack.Count > _maxStackSize)
            {
                int trimCount = _routeStack.Count - _maxStackSize;
                _routeStack.RemoveRange(0, trimCount);
                _currentRouteIndex -= trimCount;
            }

            if (_routeStack.Count <= 1)
                return;

            var keepViews = new HashSet<UIElement>(_routeStack.Count * 3);

            int cachedStartIndex = Math.Max(0, _currentRouteIndex - _maxBackStackCachedViewDepth);
            int cachedEndIndex = Math.Min(_routeStack.Count - 1, _currentRouteIndex + _maxForwardStackCachedViewDepth);

            // Keep all the views that can be cached within the cached range

            for (int j = cachedStartIndex; j <= cachedEndIndex; j++)
            {
                var route = _routeStack[j];

                foreach (var item in route.Items)
                {
                    // Always keep views from the current route

                    if (item.HasViewAndModel && (j == _currentRouteIndex || item.ViewModel.CanViewBeCached))
                        keepViews.Add(item.View);
                }
            }

            // Remove all the views that are not in the keepViews set

            foreach (var route in _routeStack)
            {
                foreach (var item in route.Items)
                {
                    if (item.HasViewAndModel && !keepViews.Contains(item.View))
                        item.ClearViewAndModel();
                }
            }
        }
    }

    private bool TryMatchRoute(string route, [MaybeNullWhen(false)] out List<IConcreteRoutePart> routeItems)
    {
        routeItems = [];

        if (TryMatchRoute(route, null, routeItems, out _))
        {
            routeItems.Reverse();
            return true;
        }

        return false;
    }

    private bool TryMatchRoute(ReadOnlySpan<char> routeString, Type? parentViewModelType, List<IConcreteRoutePart> routeItems, out ReadOnlySpan<char> rest)
    {
        foreach (var route in _routeParts.Where(r => r.ParentViewModelType == parentViewModelType))
        {
            if (!route.TryMatch(routeString, out var concreteRoute, out rest))
                continue;

            if (rest.Length is 0 || TryMatchRoute(rest, route.ViewModelType, routeItems, out rest))
            {
                routeItems.Add(concreteRoute);
                return true;
            }
        }

        rest = routeString;
        return false;
    }
}
