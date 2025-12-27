namespace Singulink.UI.Navigation.WinUI;

/// <content>
/// Provides parent navigation related implementations for the navigator.
/// </content>
partial class Navigator
{
    /// <inheritdoc cref="INavigator.CurrentRouteHasParent{TParentViewModel}"/>
    public bool CurrentRouteHasParent<TViewModel>()
    {
        EnsureThreadAccess();

        var currentRoute = CurrentRouteImpl;

        if (currentRoute is null)
            return false;

        return currentRoute.Items
            .Take(currentRoute.Items.Count - 1)
            .Any(ri => ri.ConcreteRoutePart.RoutePart.ViewModelType == typeof(TViewModel));
    }

    /// <inheritdoc cref="INavigator.NavigateToParentAsync{TParentViewModel}(RouteOptions?)"/>
    public Task<NavigationResult> NavigateToParentAsync<TParentViewModel>(RouteOptions? options = null)
        where TParentViewModel : class
    {
        EnsureThreadAccess();
        return NavigateToParentAsync(typeof(TParentViewModel), options);
    }

    private async Task<NavigationResult> NavigateToParentAsync(Type parentViewModelType, RouteOptions? options = null)
    {
        var currentRoute = CurrentRouteImpl ?? throw new InvalidOperationException("Cannot navigate to parent before the navigator has a route.");

        var routeParts = currentRoute.Items
            .Take(currentRoute.Items.Count - 1)
            .Select(ri => ri.ConcreteRoutePart)
            .ToList();

        int parentIndex = routeParts.FindLastIndex(rp => rp.RoutePart.ViewModelType == parentViewModelType);

        if (parentIndex < 0)
            throw new NavigationRouteException($"Current route does not contain a parent view model of type '{parentViewModelType}'.");

        return await NavigateNewAsyncCore(routeParts[..(parentIndex + 1)], options);
    }
}
