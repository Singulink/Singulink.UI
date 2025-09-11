namespace Singulink.UI.Navigation;

/// <summary>
/// Provides methods to redirect a navigation to a different route.
/// </summary>
public interface IRedirectNavigator
{
    /// <summary>
    /// Navigates back to the previous view.
    /// </summary>
    public void GoBack();

    /// <summary>
    /// Navigates forward to the next view.
    /// </summary>
    public void GoForward();

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    public void Navigate(string route);

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    public void Navigate(IConcreteRootRoutePart rootRoutePart, RouteOptions? routeOptions = null);

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    public void Navigate<TRootViewModel>(
        IConcreteRootRoutePart<TRootViewModel> rootRoutePart,
        IConcreteChildRoutePart<TRootViewModel> childRoutePart,
        RouteOptions? routeOptions = null)
        where TRootViewModel : class;

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    public void Navigate<TRootViewModel, TChildViewModel1>(
        IConcreteRootRoutePart<TRootViewModel> rootRoutePart,
        IConcreteChildRoutePart<TRootViewModel, TChildViewModel1> childRoutePart1,
        IConcreteChildRoutePart<TChildViewModel1> childRoutePart2,
        RouteOptions? routeOptions = null)
        where TRootViewModel : class
        where TChildViewModel1 : class;

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    public void Navigate<TRootViewModel, TChildViewModel1, TChildViewModel2>(
        IConcreteRootRoutePart<TRootViewModel> rootRoutePart,
        IConcreteChildRoutePart<TRootViewModel, TChildViewModel1> childRoutePart1,
        IConcreteChildRoutePart<TChildViewModel1, TChildViewModel2> childRoutePart2,
        IConcreteChildRoutePart<TChildViewModel2> childRoutePart3,
        RouteOptions? routeOptions = null)
        where TRootViewModel : class
        where TChildViewModel1 : class
        where TChildViewModel2 : class;

    /// <summary>
    /// Navigates to a partial route that has the same path as the current route but with the specified options.
    /// </summary>
    public void NavigatePartial(RouteOptions routeOptions);

    /// <summary>
    /// Navigates to the specified partial route. The current route must contain a view with the specified parent view model type otherwise an <see
    /// cref="InvalidOperationException"/> is thrown.
    /// </summary>
    public void NavigatePartial<TParentViewModel>(
        IConcreteChildRoutePart<TParentViewModel> childRoutePart,
        RouteOptions? routeOptions = null)
        where TParentViewModel : class;

    /// <summary>
    /// Navigates to the specified partial route. The current route must contain a view with the specified parent view model type otherwise an <see
    /// cref="InvalidOperationException"/> is thrown.
    /// </summary>
    public void NavigatePartial<TParentViewModel, TChildViewModel1>(
        IConcreteChildRoutePart<TParentViewModel, TChildViewModel1> childRoutePart1,
        IConcreteChildRoutePart<TChildViewModel1> childRoutePart2,
        RouteOptions? routeOptions = null)
        where TParentViewModel : class
        where TChildViewModel1 : class;

    /// <summary>
    /// Navigates to the specified partial route. The current route must contain a view with the specified parent view model type otherwise an <see
    /// cref="InvalidOperationException"/> is thrown.
    /// </summary>
    public void NavigatePartial<TParentViewModel, TChildViewModel1, TChildViewModel2>(
        IConcreteChildRoutePart<TParentViewModel, TChildViewModel1> childRoutePart1,
        IConcreteChildRoutePart<TChildViewModel1, TChildViewModel2> childRoutePart2,
        IConcreteChildRoutePart<TChildViewModel2> childRoutePart3,
        RouteOptions? routeOptions = null)
        where TParentViewModel : class
        where TChildViewModel1 : class
        where TChildViewModel2 : class;

    /// <summary>
    /// Navigates to the parent view in the current route that has the specified view model type.
    /// </summary>
    public void NavigateToParent<TParentViewModel>(RouteOptions? options = null)
        where TParentViewModel : class;
}
