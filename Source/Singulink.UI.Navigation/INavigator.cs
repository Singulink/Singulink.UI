using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Singulink.UI.Tasks;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a navigator that can navigate a hierarchy of views using routes and display dialogs for an application or window.
/// </summary>
/// <remarks>
/// Navigator instances are not thread-safe and should only be accessed from the UI thread. Attempting to access methods or properties from a non-UI thread will
/// result in an <see cref="InvalidOperationException"/> being thrown.
/// </remarks>
public interface INavigator : IDialogNavigatorBase, INotifyPropertyChanged
{
    /// <summary>
    /// Gets a value indicating whether the navigator can navigate back to the previous view from a user-initiated request. This property can be used to bind
    /// the enabled state of a back button in the UI.
    /// </summary>
    public bool CanUserGoBack { get; }

    /// <summary>
    /// Gets a value indicating whether the navigator can navigate forward to the next view from a user-initiated request. This property can be used to bind
    /// the enabled state of a forward button in the UI.
    /// </summary>
    public bool CanUserGoForward { get; }

    /// <summary>
    /// Gets a value indicating whether the navigator can refresh the current view from a user-initiated request. This property can be used to bind
    /// the enabled state of a refresh button or "pull to refresh" feature in the UI.
    /// </summary>
    public bool CanUserRefresh { get; }

    /// <summary>
    /// Gets a value indicating whether the navigator has back history. This property should be checked prior to doing a programmatic back navigation.
    /// </summary>
    public bool HasBackHistory { get; }

    /// <summary>
    /// Gets a value indicating whether the navigator has forward history. This property should be checked prior to doing a programmatic forward navigation.
    /// </summary>
    public bool HasForwardHistory { get; }

    /// <summary>
    /// Gets a value indicating whether the navigator has navigated to any route since it was created.
    /// </summary>
    public bool DidNavigate { get; }

    /// <summary>
    /// Gets a value indicating whether the navigator is currently in the process of navigating to a new view.
    /// </summary>
    public bool IsNavigating { get; }

    /// <summary>
    /// Gets a value indicating whether the navigator is currently showing a dialog.
    /// </summary>
    public bool IsShowingDialog { get; }

    /// <summary>
    /// Gets the current route that the navigator is displaying.
    /// </summary>
    public string? CurrentRoute { get; }

    /// <summary>
    /// Gets the task runner for this navigator.
    /// </summary>
    public ITaskRunner TaskRunner { get; }

    /// <summary>
    /// Clears back and forward navigation history.
    /// </summary>
    public void ClearHistory();

    /// <summary>
    /// Returns the routes that are in the back navigation stack, ordered from the most recent to the oldest. Does not include the current route.
    /// </summary>
    public IList<string> GetBackStackRoutes();

    /// <summary>
    /// Returns a list of routes currently in the forward navigation stack. Does not include the current route.
    /// </summary>
    public IList<string> GetForwardStackRoutes();

    /// <summary>
    /// Gets the route options for the current route.
    /// </summary>
    public RouteOptions GetRouteOptions();

    /// <summary>
    /// Navigates back to the previous view.
    /// </summary>
    public Task<NavigationResult> GoBackAsync();

    /// <summary>
    /// Navigates forward to the next view.
    /// </summary>
    /// <param name="userInitiated">Indicates whether the navigation is initiated by the user.</param>
    public Task<NavigationResult> GoForwardAsync(bool userInitiated);

    /// <summary>
    /// Handles a system back request, such as when the user presses the back button on a mobile device, a hardware back button on a desktop or the back button
    /// in a web browser. Returns <see langword="true"/> if the request was handled and navigation was initiated, otherwise <see langword="false"/> (i.e. if
    /// there is no back history or if back navigation is not possible because a non-dismissable dialog is showing).
    /// </summary>
    public bool HandleSystemBackRequest();

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    public Task<NavigationResult> NavigateAsync(string route);

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    public Task<NavigationResult> NavigateAsync<TViewModel>(IConcreteRootRoute<TViewModel> route, RouteOptions? routeOptions = null)
        where TViewModel : class;

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    public Task<NavigationResult> NavigateAsync<TRootViewModel, TNestedViewModel>(
        IConcreteRootRoute<TRootViewModel> rootRoute,
        IConcreteNestedRoute<TRootViewModel, TNestedViewModel> nestedRoute,
        RouteOptions? routeOptions = null)
        where TRootViewModel : class
        where TNestedViewModel : class;

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    public Task<NavigationResult> NavigateAsync<TRootViewModel, TNestedViewModel1, TNestedViewModel2>(
        IConcreteRootRoute<TRootViewModel> rootRoute,
        IConcreteNestedRoute<TRootViewModel, TNestedViewModel1> nestedRoute1,
        IConcreteNestedRoute<TNestedViewModel1, TNestedViewModel2> nestedRoute2,
        RouteOptions? routeOptions = null)
        where TRootViewModel : class
        where TNestedViewModel1 : class
        where TNestedViewModel2 : class;

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    public Task<NavigationResult> NavigateAsync<TRootViewModel, TNestedViewModel1, TNestedViewModel2, TNestedViewModel3>(
        IConcreteRootRoute<TRootViewModel> rootRoute,
        IConcreteNestedRoute<TRootViewModel, TNestedViewModel1> nestedRoute1,
        IConcreteNestedRoute<TNestedViewModel1, TNestedViewModel2> nestedRoute2,
        IConcreteNestedRoute<TNestedViewModel2, TNestedViewModel3> nestedRoute3,
        RouteOptions? routeOptions = null)
        where TRootViewModel : class
        where TNestedViewModel1 : class
        where TNestedViewModel2 : class
        where TNestedViewModel3 : class;

    /// <summary>
    /// Navigates to a partial route that has the same path as the current route but with the specified options.
    /// </summary>
    public Task<NavigationResult> NavigatePartialAsync(RouteOptions routeOptions);

    /// <summary>
    /// Navigates to the specified partial route. The current route must contain a view with the specified parent view model type otherwise an <see
    /// cref="InvalidOperationException"/> is thrown.
    /// </summary>
    public Task<NavigationResult> NavigatePartialAsync<TParentViewModel, TNestedViewModel>(
        IConcreteNestedRoute<TParentViewModel, TNestedViewModel> nestedRoute,
        RouteOptions? routeOptions = null)
        where TParentViewModel : class
        where TNestedViewModel : class;

    /// <summary>
    /// Navigates to the specified partial route. The current route must contain a view with the specified parent view model type otherwise an <see
    /// cref="InvalidOperationException"/> is thrown.
    /// </summary>
    public Task<NavigationResult> NavigatePartialAsync<TParentViewModel, TNestedViewModel1, TNestedViewModel2>(
        IConcreteNestedRoute<TParentViewModel, TNestedViewModel1> nestedRoute1,
        IConcreteNestedRoute<TNestedViewModel1, TNestedViewModel2> nestedRoute2,
        RouteOptions? routeOptions = null)
        where TParentViewModel : class
        where TNestedViewModel1 : class
        where TNestedViewModel2 : class;

    /// <summary>
    /// Navigates to the specified partial route. The current route must contain a view with the specified parent view model type otherwise an <see
    /// cref="InvalidOperationException"/> is thrown.
    /// </summary>
    public Task<NavigationResult> NavigatePartialAsync<TParentViewModel, TNestedViewModel1, TNestedViewModel2, TNestedViewModel3>(
        IConcreteNestedRoute<TParentViewModel, TNestedViewModel1> nestedRoute1,
        IConcreteNestedRoute<TNestedViewModel1, TNestedViewModel2> nestedRoute2,
        IConcreteNestedRoute<TNestedViewModel2, TNestedViewModel3> nestedRoute3,
        RouteOptions? routeOptions = null)
        where TParentViewModel : class
        where TNestedViewModel1 : class
        where TNestedViewModel2 : class
        where TNestedViewModel3 : class;

    /// <summary>
    /// Refreshes the current route.
    /// </summary>
    /// <param name="userInitiated">Indicates whether the refresh was initiated by the user.</param>
    public Task<NavigationResult> RefreshAsync(bool userInitiated);

    /// <summary>
    /// Gets the route parameter from the current route.
    /// </summary>
    public bool TryGetRouteParameter<TViewModel, TParam>(RouteBase<TViewModel, TParam> route, [MaybeNullWhen(false)] out TParam parameter)
        where TViewModel : class, IRoutedViewModel<TParam>
        where TParam : notnull;

    /// <summary>
    /// Returns the last view model that matches the specified view model type from the current route. Can only be used to reliably get parent view models since
    /// child view models may have not been initialized yet.
    /// </summary>
    public bool TryGetRouteViewModel<TViewModel>([MaybeNullWhen(false)] out TViewModel viewModel) where TViewModel : class;
}
