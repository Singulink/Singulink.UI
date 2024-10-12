using Singulink.UI.Navigation.Utilities;

namespace Singulink.UI.Navigation;

/// <content>
/// Provides partial navigation related implementations for the navigator.
/// </content>
public partial class Navigator
{
    /// <inheritdoc cref="INavigator.NavigatePartialAsync(RouteOptions)"/>
    public async Task<NavigationResult> NavigatePartialAsync(RouteOptions routeOptions)
    {
        return await NavigateAsync(NavigationType.New, null, routeOptions);
    }

    /// <inheritdoc cref="INavigator.NavigatePartialAsync{TParentViewModel, TNestedViewModel}(ISpecifiedNestedRoute{TParentViewModel, TNestedViewModel}, RouteOptions)"/>
    public async Task<NavigationResult> NavigatePartialAsync<TParentViewModel, TNestedViewModel>(
        ISpecifiedNestedRoute<TParentViewModel, TNestedViewModel> nestedRoute,
        RouteOptions? routeOptions = null)
        where TParentViewModel : class
        where TNestedViewModel : class
    {
        return await NavigatePartialAsync(typeof(TParentViewModel), [nestedRoute], routeOptions);
    }

    /// <inheritdoc cref="INavigator.NavigatePartialAsync{TParentViewModel, TNestedViewModel1, TNestedViewModel2}(ISpecifiedNestedRoute{TParentViewModel, TNestedViewModel1}, ISpecifiedNestedRoute{TNestedViewModel1, TNestedViewModel2}, RouteOptions)"/>
    public async Task<NavigationResult> NavigatePartialAsync<TParentViewModel, TNestedViewModel1, TNestedViewModel2>(
        ISpecifiedNestedRoute<TParentViewModel, TNestedViewModel1> nestedRoute1,
        ISpecifiedNestedRoute<TNestedViewModel1, TNestedViewModel2> nestedRoute2,
        RouteOptions? routeOptions = null)
        where TParentViewModel : class
        where TNestedViewModel1 : class
        where TNestedViewModel2 : class
    {
        return await NavigatePartialAsync(typeof(TParentViewModel), [nestedRoute1, nestedRoute2], routeOptions);
    }

    /// <inheritdoc cref="INavigator.NavigatePartialAsync{TParentViewModel, TNestedViewModel1, TNestedViewModel2, TNestedViewModel3}(ISpecifiedNestedRoute{TParentViewModel, TNestedViewModel1}, ISpecifiedNestedRoute{TNestedViewModel1, TNestedViewModel2}, ISpecifiedNestedRoute{TNestedViewModel2, TNestedViewModel3}, RouteOptions)"/>
    public async Task<NavigationResult> NavigatePartialAsync<TParentViewModel, TNestedViewModel1, TNestedViewModel2, TNestedViewModel3>(
        ISpecifiedNestedRoute<TParentViewModel, TNestedViewModel1> nestedRoute1,
        ISpecifiedNestedRoute<TNestedViewModel1, TNestedViewModel2> nestedRoute2,
        ISpecifiedNestedRoute<TNestedViewModel2, TNestedViewModel3> nestedRoute3,
        RouteOptions? routeOptions = null)
        where TParentViewModel : class
        where TNestedViewModel1 : class
        where TNestedViewModel2 : class
        where TNestedViewModel3 : class
    {
        return await NavigatePartialAsync(typeof(TParentViewModel), [nestedRoute1, nestedRoute2, nestedRoute3], routeOptions);
    }

    private async Task<NavigationResult> NavigatePartialAsync(Type parentViewModelType, List<ISpecifiedRoute> requestedNestedRoutes, RouteOptions? routeOptions)
    {
        var currentRouteInfo = CurrentRouteInfo ?? throw new InvalidOperationException("Cannot navigate partial route when no route is currently active.");

        int parentRouteItemIndex = currentRouteInfo.Items.FindLastIndex(ri => ri.SpecifiedRoute.Route.ViewModelType == parentViewModelType);

        if (parentRouteItemIndex < 0)
            throw new InvalidOperationException($"Current route does not contain a parent view model of type '{parentViewModelType}'.");

        var routes = currentRouteInfo.Items.Take(parentRouteItemIndex + 1).Select(ri => ri.SpecifiedRoute).Concat(requestedNestedRoutes).ToList();
        return await NavigateNewWithEnsureMatched(routes, routeOptions);
    }
}
