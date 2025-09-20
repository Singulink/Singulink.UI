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
        return await NavigateNewAsync(null, routeOptions);
    }

    /// <inheritdoc cref="INavigator.NavigatePartialAsync{TParentViewModel}(IConcreteChildRoutePart{TParentViewModel}, RouteOptions?)"/>
    public async Task<NavigationResult> NavigatePartialAsync<TParentViewModel>(
        IConcreteChildRoutePart<TParentViewModel> childRoutePart,
        RouteOptions? routeOptions = null)
        where TParentViewModel : class
    {
        EnsureThreadAccess();
        return await NavigatePartialAsync(typeof(TParentViewModel), [childRoutePart], routeOptions);
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
        return await NavigatePartialAsync(typeof(TParentViewModel), [childRoutePart1, childRoutePart2, childRoutePart3], routeOptions);
    }

    private async Task<NavigationResult> NavigatePartialAsync(Type parentViewModelType, List<IConcreteRoutePart> requestedChildRoutes, RouteOptions? routeOptions)
    {
        var currentRoute = CurrentRouteImpl ?? throw new InvalidOperationException("Cannot navigate to a partial route before the navigator has a route.");

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

        var routes = currentRoute.Items
            .Take(parentRouteItemIndex + 1)
            .Select(ri => ri.ConcreteRoutePart)
            .Concat(requestedChildRoutes)
            .ToList();

        return await NavigateNewWithRouteCheckAsync(routes, routeOptions);
    }
}
