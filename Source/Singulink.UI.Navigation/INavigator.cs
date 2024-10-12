using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

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
    /// Gets a value indicating whether the navigator is currently in the process of navigating to a new view.
    /// </summary>
    public bool IsNavigating { get; }

    /// <summary>
    /// Gets a value indicating whether the navigator is currently showing a dialog.
    /// </summary>
    public bool IsShowingDialog { get; }

    /// <summary>
    /// Gets a value indicating whether the navigator has back history. This property should be checked prior to doing a programmatic back navigation.
    /// </summary>
    public bool HasBackHistory { get; }

    /// <summary>
    /// Gets a value indicating whether the navigator has forward history. This property should be checked prior to doing a programmatic forward navigation.
    /// </summary>
    public bool HasForwardHistory { get; }

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
    /// Gets the route options for the current route.
    /// </summary>
    public RouteOptions GetRouteOptions();

    /// <summary>
    /// Navigates back to the previous view.
    /// </summary>
    /// <param name="userInitiated">Indicates whether the navigation was initiated by the user.</param>
    public Task<NavigationResult> GoBackAsync(bool userInitiated);

    /// <summary>
    /// Navigates forward to the next view.
    /// </summary>
    /// <param name="userInitiated">Indicates whether the navigation is initiated by the user.</param>
    public Task<NavigationResult> GoForwardAsync(bool userInitiated);

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    public Task<NavigationResult> NavigateAsync(string route);

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    public Task<NavigationResult> NavigateAsync<TViewModel>(ISpecifiedRootRoute<TViewModel> route, RouteOptions? routeOptions = null)
        where TViewModel : class;

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    public Task<NavigationResult> NavigateAsync<TRootViewModel, TNestedViewModel>(
        ISpecifiedRootRoute<TRootViewModel> rootRoute,
        ISpecifiedNestedRoute<TRootViewModel, TNestedViewModel> nestedRoute,
        RouteOptions? routeOptions = null)
        where TRootViewModel : class
        where TNestedViewModel : class;

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    public Task<NavigationResult> NavigateAsync<TRootViewModel, TNestedViewModel1, TNestedViewModel2>(
        ISpecifiedRootRoute<TRootViewModel> rootRoute,
        ISpecifiedNestedRoute<TRootViewModel, TNestedViewModel1> nestedRoute1,
        ISpecifiedNestedRoute<TNestedViewModel1, TNestedViewModel2> nestedRoute2,
        RouteOptions? routeOptions = null)
        where TRootViewModel : class
        where TNestedViewModel1 : class
        where TNestedViewModel2 : class;

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    public Task<NavigationResult> NavigateAsync<TRootViewModel, TNestedViewModel1, TNestedViewModel2, TNestedViewModel3>(
        ISpecifiedRootRoute<TRootViewModel> rootRoute,
        ISpecifiedNestedRoute<TRootViewModel, TNestedViewModel1> nestedRoute1,
        ISpecifiedNestedRoute<TNestedViewModel1, TNestedViewModel2> nestedRoute2,
        ISpecifiedNestedRoute<TNestedViewModel2, TNestedViewModel3> nestedRoute3,
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
        ISpecifiedNestedRoute<TParentViewModel, TNestedViewModel> nestedRoute,
        RouteOptions? routeOptions = null)
        where TParentViewModel : class
        where TNestedViewModel : class;

    /// <summary>
    /// Navigates to the specified partial route. The current route must contain a view with the specified parent view model type otherwise an <see
    /// cref="InvalidOperationException"/> is thrown.
    /// </summary>
    public Task<NavigationResult> NavigatePartialAsync<TParentViewModel, TNestedViewModel1, TNestedViewModel2>(
        ISpecifiedNestedRoute<TParentViewModel, TNestedViewModel1> nestedRoute1,
        ISpecifiedNestedRoute<TNestedViewModel1, TNestedViewModel2> nestedRoute2,
        RouteOptions? routeOptions = null)
        where TParentViewModel : class
        where TNestedViewModel1 : class
        where TNestedViewModel2 : class;

    /// <summary>
    /// Navigates to the specified partial route. The current route must contain a view with the specified parent view model type otherwise an <see
    /// cref="InvalidOperationException"/> is thrown.
    /// </summary>
    public Task<NavigationResult> NavigatePartialAsync<TParentViewModel, TNestedViewModel1, TNestedViewModel2, TNestedViewModel3>(
        ISpecifiedNestedRoute<TParentViewModel, TNestedViewModel1> nestedRoute1,
        ISpecifiedNestedRoute<TNestedViewModel1, TNestedViewModel2> nestedRoute2,
        ISpecifiedNestedRoute<TNestedViewModel2, TNestedViewModel3> nestedRoute3,
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
    /// Registers a task receiver that gets invoked whenever an asynchronous navigation occurs. The task passed into the receiver completes at the end of the
    /// navigation.
    /// </summary>
    /// <remarks>
    /// This method can be used to register a task receiver that can control UI state while an asynchronous navigation is in progress, such as showing a loading
    /// indicator or disabling controls. The task receiver is not invoked if the navigation completes synchronously. Particularly useful for allowing you to run
    /// asynchronous navigations as busy tasks with a TaskRunner from the Singulink.UI.Tasks library.
    /// </remarks>
    public void RegisterAsyncNavigationTaskReceiver(Action<Task> asyncNavigationTaskReceiver);

    /// <summary>
    /// Unregisters a task receiver that was previously registered with <see cref="RegisterAsyncNavigationTaskReceiver"/>.
    /// </summary>
    public void UnregisterAsyncNavigationTaskReceiver(Action<Task> asyncNavigationTaskReceiver);

    /// <summary>
    /// Gets the route parameter from the current route.
    /// </summary>
    public bool TryGetRouteParameter<TParam, TViewModel>(RouteBase<TParam, TViewModel> route, [MaybeNullWhen(false)] out TParam parameter)
        where TParam : notnull
        where TViewModel : class, IRoutedViewModel<TParam>;

    /// <summary>
    /// Returns the last view model that matches the specified view model type from the current route. Can only be used to reliably get parent view models since
    /// child view models may have not been initialized yet.
    /// </summary>
    public bool TryGetRouteViewModel<TViewModel>([MaybeNullWhen(false)] out TViewModel viewModel) where TViewModel : class;
}
