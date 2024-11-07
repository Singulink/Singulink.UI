using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Singulink.UI.Navigation;

/// <content>
/// Provides navigation related implementations for the navigator.
/// </content>
public partial class Navigator
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

        if (!TryMatchRoute(route, out var requestedSpecifiedRouteItems))
            throw new ArgumentException($"No route found for '{route}'.", nameof(route));

        var routeOptions = anchor is not null ? new RouteOptions(anchor) : null;

        return await NavigateAsync(NavigationType.New, requestedSpecifiedRouteItems, routeOptions);
    }

    /// <inheritdoc cref="INavigator.NavigateAsync{TViewModel}(ISpecifiedRootRoute{TViewModel}, RouteOptions)"/>
    public async Task<NavigationResult> NavigateAsync<TViewModel>(ISpecifiedRootRoute<TViewModel> route, RouteOptions? routeOptions = null)
        where TViewModel : class
    {
        EnsureThreadAccess();
        CloseLightDismissPopups();

        return await NavigateNewWithEnsureMatched([route], routeOptions);
    }

    /// <inheritdoc cref="INavigator.NavigateAsync{TParentViewModel, TNestedViewModel}(ISpecifiedRootRoute{TParentViewModel}, ISpecifiedNestedRoute{TParentViewModel, TNestedViewModel}, RouteOptions)"/>
    public async Task<NavigationResult> NavigateAsync<TParentViewModel, TNestedViewModel>(
        ISpecifiedRootRoute<TParentViewModel> parentRoute,
        ISpecifiedNestedRoute<TParentViewModel, TNestedViewModel> nestedRoute,
        RouteOptions? routeOptions = null)
        where TParentViewModel : class
        where TNestedViewModel : class
    {
        EnsureThreadAccess();
        CloseLightDismissPopups();

        return await NavigateNewWithEnsureMatched([parentRoute, nestedRoute], routeOptions);
    }

    /// <inheritdoc cref="INavigator.NavigateAsync{TParentViewModel, TNestedViewModel1, TNestedViewModel2}(ISpecifiedRootRoute{TParentViewModel}, ISpecifiedNestedRoute{TParentViewModel, TNestedViewModel1}, ISpecifiedNestedRoute{TNestedViewModel1, TNestedViewModel2}, RouteOptions)"/>
    public async Task<NavigationResult> NavigateAsync<TParentViewModel, TNestedViewModel1, TNestedViewModel2>(
        ISpecifiedRootRoute<TParentViewModel> parentRoute,
        ISpecifiedNestedRoute<TParentViewModel, TNestedViewModel1> nestedRoute1,
        ISpecifiedNestedRoute<TNestedViewModel1, TNestedViewModel2> nestedRoute2,
        RouteOptions? routeOptions = null)
        where TParentViewModel : class
        where TNestedViewModel1 : class
        where TNestedViewModel2 : class
    {
        EnsureThreadAccess();
        CloseLightDismissPopups();

        return await NavigateNewWithEnsureMatched([parentRoute, nestedRoute1, nestedRoute2], routeOptions);
    }

    /// <inheritdoc cref="INavigator.NavigateAsync{TParentViewModel, TNestedViewModel1, TNestedViewModel2, TNestedViewModel3}(ISpecifiedRootRoute{TParentViewModel}, ISpecifiedNestedRoute{TParentViewModel, TNestedViewModel1}, ISpecifiedNestedRoute{TNestedViewModel1, TNestedViewModel2}, ISpecifiedNestedRoute{TNestedViewModel2, TNestedViewModel3}, RouteOptions)"/>
    public async Task<NavigationResult> NavigateAsync<TParentViewModel, TNestedViewModel1, TNestedViewModel2, TNestedViewModel3>(
        ISpecifiedRootRoute<TParentViewModel> parentRoute,
        ISpecifiedNestedRoute<TParentViewModel, TNestedViewModel1> nestedRoute1,
        ISpecifiedNestedRoute<TNestedViewModel1, TNestedViewModel2> nestedRoute2,
        ISpecifiedNestedRoute<TNestedViewModel2, TNestedViewModel3> nestedRoute3,
        RouteOptions? routeOptions = null)
        where TParentViewModel : class
        where TNestedViewModel1 : class
        where TNestedViewModel2 : class
        where TNestedViewModel3 : class
    {
        EnsureThreadAccess();
        CloseLightDismissPopups();

        return await NavigateNewWithEnsureMatched([parentRoute, nestedRoute1, nestedRoute2, nestedRoute3], routeOptions);
    }

    private string GetRouteString(List<ISpecifiedRoute> routes) => string.Join("/", routes.Select(r => r.ToString()).Where(r => !string.IsNullOrEmpty(r)));

    private async Task<NavigationResult> NavigateNewWithEnsureMatched(List<ISpecifiedRoute> specifiedRouteItems, RouteOptions? routeOptions)
    {
        string routeString = GetRouteString(specifiedRouteItems);

        if (!TryMatchRoute(routeString, out var requestedRoutes))
            throw new ArgumentException($"No route found for '{routeString}'.", nameof(specifiedRouteItems));

        if (!requestedRoutes.SequenceEqual(specifiedRouteItems))
        {
            string message = $"Route '{routeString}' matched a different route than what was specified. " +
                "Routes may be misconfigured, missing or specified in an incorrect order.";
            throw new ArgumentException(message, nameof(specifiedRouteItems));
        }

        return await NavigateAsync(NavigationType.New, requestedRoutes, routeOptions);
    }

    private Task<NavigationResult> NavigateAsync(
        NavigationType navigationType,
        List<ISpecifiedRoute>? requestedSpecifiedRouteItems,
        RouteOptions? routeOptions)
    {
        var task = GetNavigateTaskAsync(navigationType, requestedSpecifiedRouteItems, routeOptions);

        if (!task.IsCompleted)
            AsyncNavigationTaskReceivers?.Invoke(task);

        return task;
    }

    private async Task<NavigationResult> GetNavigateTaskAsync(
        NavigationType navigationType,
        List<ISpecifiedRoute>? requestedSpecifiedRouteItems,
        RouteOptions? routeOptions)
    {
        if (_dialogInfoStack.Count > 0)
            throw new InvalidOperationException("Cannot navigate while a dialog is shown.");

        if (_blockNavigation)
            throw new InvalidOperationException("Navigation requested at an invalid time while navigation is blocked.");

        var lastRouteInfo = CurrentRouteInfo;

        RouteInfo routeInfo;

        if (navigationType is NavigationType.New)
        {
            routeOptions ??= RouteOptions.Empty;

            if (requestedSpecifiedRouteItems is not null)
                routeInfo = BuildRouteInfo(requestedSpecifiedRouteItems, routeOptions);
            else if (CurrentRouteInfo is not null)
                routeInfo = new RouteInfo(CurrentRouteInfo.Items, routeOptions);
            else
                throw new InvalidOperationException("Cannot navigate partial route when no route is currently active.");
        }
        else
        {
            Debug.Assert(requestedSpecifiedRouteItems is null, "Route items should only be provided for new navigations.");
            Debug.Assert(routeOptions is null, "Route options should only be provided for new navigations.");

            if (navigationType is NavigationType.Back)
            {
                if (!HasBackHistory)
                    throw new InvalidOperationException("Cannot navigate back because there is no previous view.");

                routeInfo = _routeInfoList[_currentRouteIndex - 1];
            }
            else if (navigationType is NavigationType.Forward)
            {
                if (!HasForwardHistory)
                    throw new InvalidOperationException("Cannot navigate forward because there is no next view.");

                routeInfo = _routeInfoList[_currentRouteIndex + 1];
            }
            else if (navigationType is NavigationType.Refresh)
            {
                routeInfo = lastRouteInfo ?? throw new InvalidOperationException("Cannot refresh when no route is currently active.");
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(navigationType));
            }
        }

        using var notifier = new PropertyChangedNotifier(this, OnPropertyChanged);
        int numCommonItems = 0; // number of common items

        if (lastRouteInfo is not null)
        {
            for (int i = 0; i < lastRouteInfo.Items.Length; i++)
            {
                numCommonItems = i;

                if (i >= routeInfo.Items.Length || lastRouteInfo.Items[i] != routeInfo.Items[i])
                    break;
            }

            // Notify view models of navigating away from the last route, starting from the end of the route
            // If a navigation is already in progress, no need to notify it that it is navigating away since it cancelled itself.

            if (_navigationCts is null)
            {
                _blockNavigation = true;
                notifier.Update();

                for (int i = lastRouteInfo.Items.Length - 1; i >= 0; i--)
                {
                    var routeItem = lastRouteInfo.Items[i];

                    if (routeItem.AlreadyNavigatedTo && routeItem.ViewModel is IRoutedViewModelBase viewModel)
                    {
                        var flags = i >= numCommonItems ? NavigatingFlags.WillBeNavigatedFrom : NavigatingFlags.None;
                        var args = new NavigatingArgs(navigationType, flags, routeInfo.Options);
                        await viewModel.OnNavigatingFromAsync(this, args);

                        if (_dialogInfoStack.Count > 0)
                            throw new InvalidOperationException("All dialogs must be closed before completing navigating away from a view model.");

                        if (args.Cancel)
                        {
                            _blockNavigation = false;
                            return NavigationResult.Cancelled;
                        }
                    }
                }

                _blockNavigation = false;
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
                _routeInfoList[_currentRouteIndex] = routeInfo;
            }
            else
            {
                if (HasForwardHistory)
                    _routeInfoList.RemoveRange(_currentRouteIndex + 1, _routeInfoList.Count - _currentRouteIndex - 1);

                _routeInfoList.Add(routeInfo);
                _currentRouteIndex = _routeInfoList.Count - 1;
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

        if (lastRouteInfo is not null)
        {
            _blockNavigation = true;
            _blockDialogs = true;

            for (int i = lastRouteInfo.Items.Length - 1; i >= numCommonItems; i--)
            {
                var routeItem = lastRouteInfo.Items[i];

                if (routeItem.AlreadyNavigatedTo && routeItem.ViewModel is IRoutedViewModelBase viewModel)
                {
                    viewModel.OnNavigatedFrom();
                    routeItem.AlreadyNavigatedTo = false;
                }
            }

            _blockNavigation = false;
            _blockDialogs = false;
        }

        var viewNavigator = _rootViewNavigator;

        for (int i = 0; i < routeInfo.Items.Length; i++)
        {
            if (viewNavigator is null)
                throw new UnreachableException("Unexpected null nested view navigator.");

            var routeItemInfo = routeInfo.Items[i];
            bool hasNestedNavigation = i < routeInfo.Items.Length - 1;

            _blockNavigation = true;
            _blockDialogs = true;

            routeItemInfo.EnsureViewCreatedAndModelInitialized();
            viewNavigator.SetActiveView(routeItemInfo.View);

            _blockNavigation = false;
            _blockDialogs = false;

            var navFlags = routeItemInfo.IsFirstNavigation ? NavigationFlags.FirstNavigation : NavigationFlags.None;

            if (routeItemInfo.AlreadyNavigatedTo)
                navFlags |= NavigationFlags.AlreadyNavigatedTo;

            if (hasNestedNavigation)
                navFlags |= NavigationFlags.HasNestedNavigation;

            var args = new NavigationArgs(navigationType, navFlags, routeInfo.Options);
            await routeItemInfo.ViewModel.OnNavigatedToAsync(this, args);

            routeItemInfo.IsFirstNavigation = false;

            if (cancelNavigateToken.IsCancellationRequested)
                return NavigationResult.Rerouted;

            routeItemInfo.AlreadyNavigatedTo = true;

            if (hasNestedNavigation && _dialogInfoStack.Count > 0)
                throw new InvalidOperationException("All dialogs must be closed before completing navigation to a view with nested navigations.");

            viewNavigator = routeItemInfo.NestedViewNavigator;
        }

        _navigationCts = null;
        TrimRouteInfoList();

        return NavigationResult.Success;

        RouteInfo BuildRouteInfo(List<ISpecifiedRoute> requestedSpecifiedRouteItems, RouteOptions routeOptions)
        {
            var commonItemInfoCandidates = _routeInfoList;
            int i; // number of common items

            for (i = 0; i < requestedSpecifiedRouteItems.Count; i++)
            {
                var newCandidates = commonItemInfoCandidates
                    .Where(ri => i < ri.Items.Length && ri.Items[i].SpecifiedRoute.Equals(requestedSpecifiedRouteItems[i])).ToList();

                if (newCandidates.Count is 0)
                    break;

                commonItemInfoCandidates = newCandidates;
            }

            var routeInfoItems = new RouteInfoItem?[requestedSpecifiedRouteItems.Count];

            // Copy common items

            if (i > 0)
                commonItemInfoCandidates[0].Items.CopyTo(0, routeInfoItems!, 0, i);

            // Create remaining items

            for (; i < routeInfoItems.Length; i++)
            {
                var item = requestedSpecifiedRouteItems[i];
                var createViewFunc = _vmTypeToViewInfo[item.Route.ViewModelType].CreateView;
                routeInfoItems[i] = new(item, createViewFunc);
            }

            return new RouteInfo(Unsafe.As<RouteInfoItem?[], ImmutableArray<RouteInfoItem>>(ref routeInfoItems), routeOptions);
        }

        void TrimRouteInfoList()
        {
            // TODO: Optimization - Remove trimmed views that can no longer be navigated to from view navigators that might cache them.

            if (_routeInfoList.Count > _maxBackStackDepth)
            {
                int trimCount = _routeInfoList.Count - _maxBackStackDepth;
                _routeInfoList.RemoveRange(0, trimCount);
                _currentRouteIndex -= trimCount;
            }
        }
    }

    /// <inheritdoc cref="INavigator.RegisterAsyncNavigationTaskReceiver(Action{Task})"/>
    public void RegisterAsyncNavigationTaskReceiver(Action<Task> asyncNavigationTaskReceiver)
    {
        AsyncNavigationTaskReceivers += asyncNavigationTaskReceiver;
    }

    /// <inheritdoc cref="INavigator.UnregisterAsyncNavigationTaskReceiver(Action{Task})"/>
    public void UnregisterAsyncNavigationTaskReceiver(Action<Task> asyncNavigationTaskReceiver)
    {
        AsyncNavigationTaskReceivers -= asyncNavigationTaskReceiver;
    }

    private bool TryMatchRoute(string route, [MaybeNullWhen(false)] out List<ISpecifiedRoute> specifiedRouteItems)
    {
        specifiedRouteItems = [];

        if (TryMatchRoute(route, null, specifiedRouteItems, out _))
        {
            specifiedRouteItems.Reverse();
            return true;
        }

        return false;
    }

    private bool TryMatchRoute(ReadOnlySpan<char> routeString, Type? parentViewModelType, List<ISpecifiedRoute> specifiedRouteItems, out ReadOnlySpan<char> rest)
    {
        EnsureThreadAccess();

        foreach (var route in _routes.Where(r => r.ParentViewModelType == parentViewModelType))
        {
            if (route.TryMatch(routeString, out var specifiedRoute, out rest))
            {
                if (rest.Length is 0 || TryMatchRoute(rest, route.ViewModelType, specifiedRouteItems, out rest))
                {
                    specifiedRouteItems.Add(specifiedRoute);
                    return true;
                }
            }
        }

        rest = routeString;
        return false;
    }
}
