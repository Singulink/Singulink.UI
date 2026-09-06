using System.ComponentModel;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a navigator that can navigate a hierarchy of views using routes and display dialogs for an application or window.
/// </summary>
/// <remarks>
/// Navigator instances are not thread-safe and should only be accessed from the UI thread. Attempting to access methods or properties from a non-UI thread will
/// result in an <see cref="InvalidOperationException"/> being thrown. Any exceptions to this rule are documented in method or property summaries.
/// </remarks>
public interface INavigator : IDialogPresenter, INotifyPropertyChanged
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
    /// Gets information about the current route, including the path and options.
    /// </summary>
    public NavigatorRoute CurrentRoute { get; }

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
    /// Clears back and forward navigation history.
    /// </summary>
    public ValueTask ClearHistoryAsync();

    /// <summary>
    /// Returns the routes that are in the back navigation stack, ordered from the most recent to the oldest. Does not include the current route.
    /// </summary>
    public IReadOnlyList<NavigatorRoute> GetBackStack();

    /// <summary>
    /// Returns a list of routes currently in the forward navigation stack. Does not include the current route.
    /// </summary>
    public IReadOnlyList<NavigatorRoute> GetForwardStack();

    /// <summary>
    /// Gets the current route parts up to the specified parent view model type.
    /// </summary>
    IEnumerable<IConcreteRoutePart> GetCurrentRoutePartsToParent(Type parentViewModelType);

    /// <summary>
    /// Navigates back to the previous view.
    /// </summary>
    public Task<NavigationResult> GoBackAsync();

    /// <summary>
    /// Navigates forward to the next view.
    /// </summary>
    public Task<NavigationResult> GoForwardAsync();

    /// <summary>
    /// Determines whether the current route contains a parent view with the specified view model type.
    /// </summary>
    public bool CurrentRouteHasParent<TViewModel>();

    /// <summary>
    /// Determines whether the current route path starts with the same path as the specified root route part.
    /// </summary>
    /// <remarks>
    /// This method does not require the mapped views or view models on the current and specified routes to match, only the route paths.
    /// </remarks>
    public bool CurrentPathStartsWith(IConcreteRootRoutePart rootRoutePart);

    /// <summary>
    /// Determines whether the current route path starts with the same path as the specified route.
    /// </summary>
    /// <remarks>
    /// This method does not require the mapped views or view models on the current and specified routes to match, only the route paths.
    /// </remarks>
    public bool CurrentPathStartsWith(ConcreteRoute route);

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    public Task<NavigationResult> NavigateAsync(string route);

    /// <summary>
    /// Navigates to the specified root route.
    /// </summary>
    public Task<NavigationResult> NavigateAsync(IConcreteRootRoutePart rootRoutePart, string? anchor = null);

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    public Task<NavigationResult> NavigateAsync(ConcreteRoute route, string? anchor = null);

    /// <summary>
    /// Navigates to a partial route that has the same path as the current route but with the specified options.
    /// </summary>
    public Task<NavigationResult> NavigatePartialAsync(string? anchor);

    /// <summary>
    /// Navigates to the specified child route beneath the parent view model type in the current route. The current route must contain a view with the
    /// specified parent view model type otherwise an <see cref="InvalidOperationException"/> is thrown.
    /// </summary>
    public Task<NavigationResult> NavigatePartialAsync<TParentViewModel>(
        IConcreteChildRoutePart<TParentViewModel> childRoutePart,
        string? anchor = null)
        where TParentViewModel : class;

    /// <summary>
    /// Navigates to the specified partial route beneath the parent view model type in the current route. The current route must contain a view with the
    /// specified parent view model type otherwise an <see cref="InvalidOperationException"/> is thrown.
    /// </summary>
    public Task<NavigationResult> NavigatePartialAsync<TParentViewModel>(
        ConcretePartialRoute<TParentViewModel> route,
        string? anchor = null)
        where TParentViewModel : class;

    /// <summary>
    /// Navigates to the parent view in the current route that has the specified view model type.
    /// </summary>
    public Task<NavigationResult> NavigateToParentAsync<TParentViewModel>(string? anchor = null)
        where TParentViewModel : class;

    /// <summary>
    /// Refreshes the current route.
    /// </summary>
    public Task<NavigationResult> RefreshAsync();

    /// <summary>
    /// Updates the current route in-place without triggering any navigation lifecycle events. This is useful for updating the anchor or other options in
    /// response to UI state changes (e.g. selected item in a list) without causing a full navigation.
    /// </summary>
    /// <param name="anchor">The new anchor to set on the current route, or <see langword="null"/> to clear the anchor.</param>
    /// <exception cref="InvalidOperationException">The navigator does not have a current route or a navigation is currently in progress.</exception>
    public void UpdateCurrentRoute(string? anchor);

    /// <summary>
    /// Updates the last route part of the current route in-place without triggering any navigation lifecycle events. The new route part must have a view model
    /// type that matches the last route part of the current route. This is useful when a route changes in response to an action (e.g. a form submission that
    /// creates a new entry and transitions from a "new-entry" to an "entry/{id}" route) without causing a full navigation.
    /// </summary>
    /// <param name="concreteRoutePart">The new concrete route part to replace the last route part of the current route.</param>
    /// <param name="anchor">The new anchor to set on the current route, or <see langword="null"/> to clear the anchor.</param>
    /// <exception cref="InvalidOperationException">The navigator does not have a current route or a navigation is currently in progress.</exception>
    /// <exception cref="ArgumentException">The view model type of the specified route part does not match the last route part of the current route.</exception>
    public void UpdateCurrentRoute(IConcreteRoutePart concreteRoutePart, string? anchor = null);
}
