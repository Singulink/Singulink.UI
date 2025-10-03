using System.ComponentModel;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a redirection to a different route.
/// </summary>
public sealed class Redirect
{
    private readonly Func<INavigator, Task<NavigationResult>> _executeFunc;

    private Redirect(Func<INavigator, Task<NavigationResult>> getRedirectTask)
    {
        _executeFunc = getRedirectTask;
    }

    /// <summary>
    /// Navigates back to the previous view.
    /// </summary>
    public static Redirect GoBack() => new(n => n.GoBackAsync());

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    public static Redirect Navigate(string route) => new(n => n.NavigateAsync(route));

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    public static Redirect Navigate(IConcreteRootRoutePart rootRoutePart, RouteOptions? routeOptions = null)
    {
        return new(n => n.NavigateAsync(rootRoutePart, routeOptions));
    }

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    public static Redirect Navigate<TRootViewModel>(
        IConcreteRootRoutePart<TRootViewModel> rootRoutePart,
        IConcreteChildRoutePart<TRootViewModel> childRoutePart,
        RouteOptions? routeOptions = null) where TRootViewModel : class
    {
        return new(n => n.NavigateAsync(rootRoutePart, childRoutePart, routeOptions));
    }

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    public static Redirect Navigate<TRootViewModel, TChildViewModel1>(
        IConcreteRootRoutePart<TRootViewModel> rootRoutePart,
        IConcreteChildRoutePart<TRootViewModel, TChildViewModel1> childRoutePart1,
        IConcreteChildRoutePart<TChildViewModel1> childRoutePart2,
        RouteOptions? routeOptions = null)
        where TRootViewModel : class
        where TChildViewModel1 : class
    {
        return new(n => n.NavigateAsync(rootRoutePart, childRoutePart1, childRoutePart2, routeOptions));
    }

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    public static Redirect Navigate<TRootViewModel, TChildViewModel1, TChildViewModel2>(
        IConcreteRootRoutePart<TRootViewModel> rootRoutePart,
        IConcreteChildRoutePart<TRootViewModel, TChildViewModel1> childRoutePart1,
        IConcreteChildRoutePart<TChildViewModel1, TChildViewModel2> childRoutePart2,
        IConcreteChildRoutePart<TChildViewModel2> childRoutePart3,
        RouteOptions? routeOptions = null)
        where TRootViewModel : class
        where TChildViewModel1 : class
        where TChildViewModel2 : class
    {
        return new(n => n.NavigateAsync(rootRoutePart, childRoutePart1, childRoutePart2, childRoutePart3, routeOptions));
    }

    /// <summary>
    /// Navigates to a partial route that has the same path as the current route but with the specified options.
    /// </summary>
    public static Redirect NavigatePartial(RouteOptions routeOptions)
    {
        return new(n => n.NavigatePartialAsync(routeOptions));
    }

    /// <summary>
    /// Navigates to the specified partial route. The current route must contain a view with the specified parent view model type otherwise an <see
    /// cref="InvalidOperationException"/> is thrown.
    /// </summary>
    public static Redirect NavigatePartial<TParentViewModel>(
        IConcreteChildRoutePart<TParentViewModel> childRoutePart,
        RouteOptions? routeOptions = null)
        where TParentViewModel : class
    {
        return new(n => n.NavigatePartialAsync(childRoutePart, routeOptions));
    }

    /// <summary>
    /// Navigates to the specified partial route. The current route must contain a view with the specified parent view model type otherwise an <see
    /// cref="InvalidOperationException"/> is thrown.
    /// </summary>
    public static Redirect NavigatePartial<TParentViewModel, TChildViewModel1>(
        IConcreteChildRoutePart<TParentViewModel, TChildViewModel1> childRoutePart1,
        IConcreteChildRoutePart<TChildViewModel1> childRoutePart2,
        RouteOptions? routeOptions = null)
        where TParentViewModel : class
        where TChildViewModel1 : class
    {
        return new(n => n.NavigatePartialAsync(childRoutePart1, childRoutePart2, routeOptions));
    }

    /// <summary>
    /// Navigates to the specified partial route. The current route must contain a view with the specified parent view model type otherwise an <see
    /// cref="InvalidOperationException"/> is thrown.
    /// </summary>
    public static Redirect NavigatePartial<TParentViewModel, TChildViewModel1, TChildViewModel2>(
        IConcreteChildRoutePart<TParentViewModel, TChildViewModel1> childRoutePart1,
        IConcreteChildRoutePart<TChildViewModel1, TChildViewModel2> childRoutePart2,
        IConcreteChildRoutePart<TChildViewModel2> childRoutePart3,
        RouteOptions? routeOptions = null)
        where TParentViewModel : class
        where TChildViewModel1 : class
        where TChildViewModel2 : class
    {
        return new(n => n.NavigatePartialAsync(childRoutePart1, childRoutePart2, childRoutePart3, routeOptions));
    }

    /// <summary>
    /// Navigates to the parent view in the current route that has the specified view model type.
    /// </summary>
    public static Redirect NavigateToParent<TParentViewModel>(RouteOptions? options = null) where TParentViewModel : class
    {
        return new(n => n.NavigateToParentAsync<TParentViewModel>(options));
    }

    /// <summary>
    /// Executes the redirect using the specified navigator. Internal use only.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public async Task<NavigationResult> ExecuteAsync(INavigator navigator) => await _executeFunc(navigator).ConfigureAwait(false);
}
