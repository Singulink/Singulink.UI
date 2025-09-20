namespace Singulink.UI.Navigation.WinUI;

internal sealed class RedirectNavigator : IRedirectNavigator
{
    internal Func<Navigator, Task<NavigationResult>>? GetRedirectTask { get; private set; }

    public void GoBack()
    {
        GetRedirectTask = n => n.GoBackAsync();
    }

    public void Navigate(string route)
    {
        GetRedirectTask = n => n.NavigateAsync(route);
    }

    public void Navigate(IConcreteRootRoutePart rootRoutePart, RouteOptions? routeOptions = null)
    {
        GetRedirectTask = n => n.NavigateAsync(rootRoutePart, routeOptions);
    }

    public void Navigate<TRootViewModel>(
        IConcreteRootRoutePart<TRootViewModel> rootRoutePart,
        IConcreteChildRoutePart<TRootViewModel> childRoutePart,
        RouteOptions? routeOptions = null) where TRootViewModel : class
    {
        GetRedirectTask = n => n.NavigateAsync(rootRoutePart, childRoutePart, routeOptions);
    }

    public void Navigate<TRootViewModel, TChildViewModel1>(
        IConcreteRootRoutePart<TRootViewModel> rootRoutePart,
        IConcreteChildRoutePart<TRootViewModel, TChildViewModel1> childRoutePart1,
        IConcreteChildRoutePart<TChildViewModel1> childRoutePart2,
        RouteOptions? routeOptions = null)
        where TRootViewModel : class
        where TChildViewModel1 : class
    {
        GetRedirectTask = n => n.NavigateAsync(rootRoutePart, childRoutePart1, childRoutePart2, routeOptions);
    }

    public void Navigate<TRootViewModel, TChildViewModel1, TChildViewModel2>(
        IConcreteRootRoutePart<TRootViewModel> rootRoutePart,
        IConcreteChildRoutePart<TRootViewModel, TChildViewModel1> childRoutePart1,
        IConcreteChildRoutePart<TChildViewModel1, TChildViewModel2> childRoutePart2,
        IConcreteChildRoutePart<TChildViewModel2> childRoutePart3,
        RouteOptions? routeOptions = null)
        where TRootViewModel : class
        where TChildViewModel1 : class
        where TChildViewModel2 : class
    {
        GetRedirectTask = n => n.NavigateAsync(rootRoutePart, childRoutePart1, childRoutePart2, childRoutePart3, routeOptions);
    }

    public void NavigatePartial(RouteOptions routeOptions)
    {
        GetRedirectTask = n => n.NavigatePartialAsync(routeOptions);
    }

    public void NavigatePartial<TParentViewModel>(
        IConcreteChildRoutePart<TParentViewModel> childRoutePart,
        RouteOptions? routeOptions = null)
        where TParentViewModel : class
    {
        GetRedirectTask = n => n.NavigatePartialAsync(childRoutePart, routeOptions);
    }

    public void NavigatePartial<TParentViewModel, TChildViewModel1>(
        IConcreteChildRoutePart<TParentViewModel, TChildViewModel1> childRoutePart1,
        IConcreteChildRoutePart<TChildViewModel1> childRoutePart2,
        RouteOptions? routeOptions = null)
        where TParentViewModel : class
        where TChildViewModel1 : class
    {
        GetRedirectTask = n => n.NavigatePartialAsync(childRoutePart1, childRoutePart2, routeOptions);
    }

    public void NavigatePartial<TParentViewModel, TChildViewModel1, TChildViewModel2>(
        IConcreteChildRoutePart<TParentViewModel, TChildViewModel1> childRoutePart1,
        IConcreteChildRoutePart<TChildViewModel1, TChildViewModel2> childRoutePart2,
        IConcreteChildRoutePart<TChildViewModel2> childRoutePart3,
        RouteOptions? routeOptions = null)
        where TParentViewModel : class
        where TChildViewModel1 : class
        where TChildViewModel2 : class
    {
        GetRedirectTask = n => n.NavigatePartialAsync(childRoutePart1, childRoutePart2, childRoutePart3, routeOptions);
    }

    public void NavigateToParent<TParentViewModel>(RouteOptions? options = null) where TParentViewModel : class
    {
        GetRedirectTask = n => n.NavigateToParentAsync<TParentViewModel>(options);
    }
}
