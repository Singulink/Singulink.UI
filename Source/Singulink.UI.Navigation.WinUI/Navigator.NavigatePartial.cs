using Singulink.UI.Navigation.WinUI.Utilities;

namespace Singulink.UI.Navigation.WinUI;

/// <content>
/// Provides partial navigation related implementations for the navigator.
/// </content>
partial class Navigator
{
    /// <inheritdoc cref="INavigator.NavigatePartialAsync(RouteOptions)"/>
    public async Task<NavigationResult> NavigatePartialAsync(RouteOptions routeOptions)
    {
        EnsureThreadAccess();
        CloseLightDismissPopups();

        return await NavigateAsync(NavigationType.New, null, routeOptions);
    }

    /// <inheritdoc cref="INavigator.NavigatePartialAsync{TParentViewModel}(IConcreteChildRoutePart{TParentViewModel}, RouteOptions?)"/>
    public async Task<NavigationResult> NavigatePartialAsync<TParentViewModel>(
        IConcreteChildRoutePart<TParentViewModel> childRoute,
        RouteOptions? routeOptions = null)
        where TParentViewModel : class
    {
        EnsureThreadAccess();
        CloseLightDismissPopups();

        return await NavigatePartialAsync(typeof(TParentViewModel), [childRoute], routeOptions);
    }

    /// <inheritdoc cref="INavigator.NavigatePartialAsync{TParentViewModel, TChildViewModel1}(IConcreteChildRoutePart{TParentViewModel, TChildViewModel1}, IConcreteChildRoutePart{TChildViewModel1}, RouteOptions?)"/>
    public async Task<NavigationResult> NavigatePartialAsync<TParentViewModel, TChildViewModel1>(
        IConcreteChildRoutePart<TParentViewModel, TChildViewModel1> childRoutePart1,
        IConcreteChildRoutePart<TChildViewModel1> childRoutePart2,
        RouteOptions? routeOptions = null)
        where TParentViewModel : class
        where TChildViewModel1 : class
    {
        EnsureThreadAccess();
        CloseLightDismissPopups();

        return await NavigatePartialAsync(typeof(TParentViewModel), [childRoutePart1, childRoutePart2], routeOptions);
    }

    /// <inheritdoc cref="INavigator.NavigatePartialAsync{TParentViewModel, TChildViewModel1, TChildViewModel2}(IConcreteChildRoutePart{TParentViewModel, TChildViewModel1}, IConcreteChildRoutePart{TChildViewModel1, TChildViewModel2}, IConcreteChildRoutePart{TChildViewModel2}, RouteOptions?)"/>
    public async Task<NavigationResult> NavigatePartialAsync<TParentViewModel, TChildViewModel1, TChildViewModel2>(
        IConcreteChildRoutePart<TParentViewModel, TChildViewModel1> childRoutePart1,
        IConcreteChildRoutePart<TChildViewModel1, TChildViewModel2> childRoutePart2,
        IConcreteChildRoutePart<TChildViewModel2> childRoutePart3,
        RouteOptions? routeOptions = null)
        where TParentViewModel : class
        where TChildViewModel1 : class
        where TChildViewModel2 : class
    {
        EnsureThreadAccess();
        CloseLightDismissPopups();

        return await NavigatePartialAsync(typeof(TParentViewModel), [childRoutePart1, childRoutePart2, childRoutePart3], routeOptions);
    }

    private async Task<NavigationResult> NavigatePartialAsync(Type parentViewModelType, List<IConcreteRoutePart> requestedChildRoutes, RouteOptions? routeOptions)
    {
        var currentRoute = CurrentRouteInternal ?? throw new InvalidOperationException("Cannot navigate partial route when no route is currently active.");

        int parentRouteItemIndex = currentRoute.Items
            .FindLastIndex(ri => ri.ConcreteRoutePart.RoutePart.ViewModelType == parentViewModelType);

        if (parentRouteItemIndex < 0)
            throw new InvalidOperationException($"Current route does not contain a parent view model of type '{parentViewModelType}'.");

        var routes = currentRoute.Items
            .Take(parentRouteItemIndex + 1)
            .Select(ri => ri.ConcreteRoutePart)
            .Concat(requestedChildRoutes)
            .ToList();

        return await NavigateNewWithEnsureMatched(routes, routeOptions);
    }
}
