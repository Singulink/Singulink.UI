using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Singulink.UI.Tasks;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a navigator that can navigate a hierarchy of views using routes and display dialogs for an application or window.
/// </summary>
/// <remarks>
/// Navigator instances are not thread-safe and should only be accessed from the UI thread. Attempting to access methods or properties from a non-UI thread will
/// result in an <see cref="InvalidOperationException"/> being thrown. Any exceptions to this rule are documented in method or property summaries.
/// </remarks>
public interface INavigator : IDialogNavigatorBase, INotifyPropertyChanged
{
    /// <summary>
    /// Gets a value indicating whether the navigator can navigate back to the previous view. This property can be used to bind the enabled state of a back
    /// button in the UI and should be checked before calling <see cref="GoBackAsync"/>.
    /// </summary>
    public bool CanGoBack { get; }

    /// <summary>
    /// Gets a value indicating whether the navigator can navigate forward to the next view. This property can be used to bind the enabled state of a forward
    /// button in the UI and should be checked before calling <see cref="GoForwardAsync"/>.
    /// </summary>
    public bool CanGoForward { get; }

    /// <summary>
    /// Gets a value indicating whether the navigator can refresh the current view. This property can be used to bind the enabled state of a refresh button or
    /// "pull to refresh" feature in the UI and should be checked before calling <see cref="RefreshAsync"/>.
    /// </summary>
    public bool CanRefresh { get; }

    /// <summary>
    /// Gets a value indicating whether the navigator has back history.
    /// </summary>
    public bool HasBackHistory { get; }

    /// <summary>
    /// Gets a value indicating whether the navigator has forward history.
    /// </summary>
    public bool HasForwardHistory { get; }

    /// <summary>
    /// Gets a value indicating whether the navigator is currently in the process of navigating to a new view.
    /// </summary>
    public bool IsNavigating { get; }

    /// <summary>
    /// Gets a value indicating whether the navigator is currently showing a dialog.
    /// </summary>
    public bool IsShowingDialog { get; }

    /// <summary>
    /// Gets information about the current route, including the path and options.
    /// </summary>
    public IConcreteRoute? CurrentRoute { get; }

    /// <summary>
    /// Gets the task runner for this navigator. This property can be accessed from any thread.
    /// </summary>
    public ITaskRunner TaskRunner { get; }

    /// <summary>
    /// Clears back and forward navigation history.
    /// </summary>
    public void ClearHistory();

    /// <summary>
    /// Returns the routes that are in the back navigation stack, ordered from the most recent to the oldest. Does not include the current route.
    /// </summary>
    public IReadOnlyList<IConcreteRoute> GetBackStack();

    /// <summary>
    /// Returns a list of routes currently in the forward navigation stack. Does not include the current route.
    /// </summary>
    public IReadOnlyList<IConcreteRoute> GetForwardStack();

    /// <summary>
    /// Navigates back to the previous view.
    /// </summary>
    public Task<NavigationResult> GoBackAsync();

    /// <summary>
    /// Navigates forward to the next view.
    /// </summary>
    public Task<NavigationResult> GoForwardAsync();

    /// <summary>
    /// Handles a system back request, such as when the user presses the back button on a mobile device, a hardware back button on a desktop or the back button
    /// in a web browser. Returns <see langword="true"/> if there is a dialog showing, a light dismiss popup was closed, a navigation is currently in progress
    /// or if a back navigation was initiated; otherwise <see langword="false"/> (meaning there was no back history).
    /// </summary>
    /// <remarks>
    /// If a dialog is showing and it implements <see cref="IDismissableDialogViewModel"/>, the <see cref="IDismissableDialogViewModel.OnDismissRequested"/>
    /// method will be called to allow the dialog to handle the back request. If the dialog is not dismissable or if a navigation is currently in progress then
    /// the back request will still be marked as handled but the request will be ignored and no navigation will occur.
    /// </remarks>
    public bool HandleSystemBackRequest();

    /// <summary>
    /// Handles a system forward request, such as when the user presses the forward button on a mobile device, a hardware forward button on a desktop or the
    /// forward button in a web browser. Returns <see langword="true"/> if there is a dialog showing, a navigation is currently in progress or if a forward
    /// navigation was initiated; otherwise <see langword="false"/> (meaning there was no forward history).
    /// </summary>
    /// <remarks>
    /// If a dialog is showing or if a navigation is currently in progress then the forward request will still be marked as handled but the request will be
    /// ignored and no navigation will occur.
    /// </remarks>
    public bool HandleSystemForwardRequest();

    /// <summary>
    /// Determines whether the specified route is a partial match to the start of the current route.
    /// </summary>
    /// <remarks>
    /// The routes are considered a partial match if the current route starts with the same path as the specified route. It does not require the routes to match
    /// up with the same view or view model.
    /// </remarks>
    public bool CurrentRouteStartsWith<TRootViewModel>(
        IConcreteRootRoutePart<TRootViewModel> rootRoute,
        IConcreteChildRoutePart<TRootViewModel> childRoutePart)
        where TRootViewModel : class;

    /// <summary>
    /// Determines whether the specified route is a partial match to the start of the current route.
    /// </summary>
    /// <remarks>
    /// The routes are considered a partial match if the current route starts with the same path as the specified route. It does not require the routes to match
    /// up with the same view or view model.
    /// </remarks>
    public bool CurrentRouteStartsWith<TRootViewModel, TChildViewModel1>(
        IConcreteRootRoutePart<TRootViewModel> rootRoute,
        IConcreteChildRoutePart<TRootViewModel, TChildViewModel1> childRoutePart1,
        IConcreteChildRoutePart<TChildViewModel1> childRoutePart2)
        where TRootViewModel : class
        where TChildViewModel1 : class;

    /// <summary>
    /// Determines whether the specified route is a partial match to the start of the current route.
    /// </summary>
    /// <remarks>
    /// The routes are considered a partial match if the current route starts with the same path as the specified route. It does not require the routes to match
    /// up with the same view or view model.
    /// </remarks>
    public bool CurrentRouteStartsWith<TRootViewModel, TChildViewModel1, TChildViewModel2>(
        IConcreteRootRoutePart<TRootViewModel> rootRoute,
        IConcreteChildRoutePart<TRootViewModel, TChildViewModel1> childRoutePart1,
        IConcreteChildRoutePart<TChildViewModel1, TChildViewModel2> childRoutePart2,
        IConcreteChildRoutePart<TChildViewModel2> childRoutePart3)
        where TRootViewModel : class
        where TChildViewModel1 : class
        where TChildViewModel2 : class;

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    public Task<NavigationResult> NavigateAsync(string route);

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    public Task<NavigationResult> NavigateAsync(IConcreteRootRoutePart route, RouteOptions? routeOptions = null);

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    public Task<NavigationResult> NavigateAsync<TRootViewModel>(
        IConcreteRootRoutePart<TRootViewModel> rootRoute,
        IConcreteChildRoutePart<TRootViewModel> childRoute,
        RouteOptions? routeOptions = null)
        where TRootViewModel : class;

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    public Task<NavigationResult> NavigateAsync<TRootViewModel, TChildViewModel1>(
        IConcreteRootRoutePart<TRootViewModel> rootRoute,
        IConcreteChildRoutePart<TRootViewModel, TChildViewModel1> childRoutePart1,
        IConcreteChildRoutePart<TChildViewModel1> childRoutePart2,
        RouteOptions? routeOptions = null)
        where TRootViewModel : class
        where TChildViewModel1 : class;

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    public Task<NavigationResult> NavigateAsync<TRootViewModel, TChildViewModel1, TChildViewModel2>(
        IConcreteRootRoutePart<TRootViewModel> rootRoute,
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
    public Task<NavigationResult> NavigatePartialAsync(RouteOptions routeOptions);

    /// <summary>
    /// Navigates to the specified partial route. The current route must contain a view with the specified parent view model type otherwise an <see
    /// cref="InvalidOperationException"/> is thrown.
    /// </summary>
    public Task<NavigationResult> NavigatePartialAsync<TParentViewModel>(
        IConcreteChildRoutePart<TParentViewModel> childRoute,
        RouteOptions? routeOptions = null)
        where TParentViewModel : class;

    /// <summary>
    /// Navigates to the specified partial route. The current route must contain a view with the specified parent view model type otherwise an <see
    /// cref="InvalidOperationException"/> is thrown.
    /// </summary>
    public Task<NavigationResult> NavigatePartialAsync<TParentViewModel, TChildViewModel1>(
        IConcreteChildRoutePart<TParentViewModel, TChildViewModel1> childRoutePart1,
        IConcreteChildRoutePart<TChildViewModel1> childRoutePart2,
        RouteOptions? routeOptions = null)
        where TParentViewModel : class
        where TChildViewModel1 : class;

    /// <summary>
    /// Navigates to the specified partial route. The current route must contain a view with the specified parent view model type otherwise an <see
    /// cref="InvalidOperationException"/> is thrown.
    /// </summary>
    public Task<NavigationResult> NavigatePartialAsync<TParentViewModel, TChildViewModel1, TChildViewModel2>(
        IConcreteChildRoutePart<TParentViewModel, TChildViewModel1> childRoutePart1,
        IConcreteChildRoutePart<TChildViewModel1, TChildViewModel2> childRoutePart2,
        IConcreteChildRoutePart<TChildViewModel2> childRoutePart3,
        RouteOptions? routeOptions = null)
        where TParentViewModel : class
        where TChildViewModel1 : class
        where TChildViewModel2 : class;

    /// <summary>
    /// Refreshes the current route.
    /// </summary>
    public Task<NavigationResult> RefreshAsync();

    /// <summary>
    /// Gets the route parameter from the current route.
    /// </summary>
    public bool TryGetCurrentRouteParameter<TViewModel, TParam>(RoutePart<TViewModel, TParam> routePart, [MaybeNullWhen(false)] out TParam parameter)
        where TViewModel : class, IRoutedViewModel<TParam>
        where TParam : notnull;

    /// <summary>
    /// Returns the last view model that can be assigned to the specified view model type from the current route.
    /// </summary>
    public bool TryGetCurrentRouteViewModel<TViewModel>([MaybeNullWhen(false)] out TViewModel viewModel) where TViewModel : class;
}
