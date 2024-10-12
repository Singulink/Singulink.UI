using System.ComponentModel;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a navigator that can navigate a hierarchy of views using routes and display dialogs for an application or window.
/// </summary>
/// <remarks>
/// Instances of this interface are not thread-safe and should only be accessed from the UI thread.
/// </remarks>
public interface INavigator : IDialogNavigatorBase, INotifyPropertyChanged
{
    /// <summary>
    /// Gets a value indicating whether the navigator is currently showing a dialog.
    /// </summary>
    public bool IsShowingDialog { get; }

    /// <summary>
    /// Gets a value indicating whether the navigator can navigate back to the previous view.
    /// </summary>
    public bool CanGoBack { get; }

    /// <summary>
    /// Gets a value indicating whether the navigator can navigate forward to the next view.
    /// </summary>
    public bool CanGoForward { get; }

    /// <summary>
    /// Gets the route options for the current route.
    /// </summary>
    public RouteOptions? GetRouteOptions();

    /// <summary>
    /// Gets the route parameter for the specified route from the current route. The current route must contain the specified route otherwise an <see
    /// cref="InvalidOperationException"/> is thrown.
    /// </summary>
    public TParam GetRouteParameter<TParam, TViewModel>(RouteBase<TParam, TViewModel> route)
        where TParam : notnull
        where TViewModel : IRoutedViewModel<TParam>;

    /// <summary>
    /// Returns the last view model that matches the specified view model type from the current route.
    /// </summary>
    public TViewModel GetRouteViewModel<TViewModel>() where TViewModel : IRoutedViewModel;

    /// <summary>
    /// Navigates back to the previous view.
    /// </summary>
    public Task GoBackAsync();

    /// <summary>
    /// Navigates forward to the next view.
    /// </summary>
    public Task GoForwardAsync();

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    public Task NavigateAsync(string route);

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    public Task NavigateAsync<TViewModel>(ISpecifiedRoute<TViewModel> route, RouteOptions? routeOptions = null) where TViewModel : IRoutedViewModel;

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    public Task NavigateAsync<TParentViewModel, TNestedViewModel>(
        ISpecifiedRoute<TParentViewModel> parentRoute,
        ISpecifiedNestedRoute<TParentViewModel, TNestedViewModel> nestedRoute,
        RouteOptions? routeOptions = null)
        where TParentViewModel : IRoutedViewModel
        where TNestedViewModel : IRoutedViewModel;

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    public Task NavigateAsync<TParentViewModel, TNestedViewModel1, TNestedViewModel2>(
        ISpecifiedRoute<TParentViewModel> parentRoute,
        ISpecifiedNestedRoute<TParentViewModel, TNestedViewModel1> nestedRoute1,
        ISpecifiedNestedRoute<TNestedViewModel1, TNestedViewModel2> nestedRoute2,
        RouteOptions? routeOptions = null)
        where TParentViewModel : IRoutedViewModel
        where TNestedViewModel1 : IRoutedViewModel
        where TNestedViewModel2 : IRoutedViewModel;

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    public Task NavigateAsync<TParentViewModel, TNestedViewModel1, TNestedViewModel2, TNestedViewModel3>(
        ISpecifiedRoute<TParentViewModel> parentRoute,
        ISpecifiedNestedRoute<TParentViewModel, TNestedViewModel1> nestedRoute1,
        ISpecifiedNestedRoute<TNestedViewModel1, TNestedViewModel2> nestedRoute2,
        ISpecifiedNestedRoute<TNestedViewModel2, TNestedViewModel3> nestedRoute3,
        RouteOptions? routeOptions = null)
        where TParentViewModel : IRoutedViewModel
        where TNestedViewModel1 : IRoutedViewModel
        where TNestedViewModel2 : IRoutedViewModel
        where TNestedViewModel3 : IRoutedViewModel;

    /// <summary>
    /// Navigates to the specified partial route. The current route must contain a view with the specified parent view model type otherwise an <see
    /// cref="InvalidOperationException"/> is thrown.
    /// </summary>
    public Task NavigatePartialAsync<TParentViewModel, TNestedViewModel>(
        ISpecifiedNestedRoute<TParentViewModel, TNestedViewModel> nestedRoute,
        RouteOptions? routeOptions = null)
        where TParentViewModel : IRoutedViewModel
        where TNestedViewModel : IRoutedViewModel;

    /// <summary>
    /// Navigates to the specified partial route. The current route must contain a view with the specified parent view model type otherwise an <see
    /// cref="InvalidOperationException"/> is thrown.
    /// </summary>
    public Task NavigatePartialAsync<TParentViewModel, TNestedViewModel1, TNestedViewModel2>(
        ISpecifiedNestedRoute<TParentViewModel, TNestedViewModel1> nestedRoute1,
        ISpecifiedNestedRoute<TNestedViewModel1, TNestedViewModel2> nestedRoute2,
        RouteOptions? routeOptions = null)
        where TParentViewModel : IRoutedViewModel
        where TNestedViewModel1 : IRoutedViewModel
        where TNestedViewModel2 : IRoutedViewModel;

    /// <summary>
    /// Navigates to the specified partial route. The current route must contain a view with the specified parent view model type otherwise an <see
    /// cref="InvalidOperationException"/> is thrown.
    /// </summary>
    public Task NavigatePartialAsync<TParentViewModel, TNestedViewModel1, TNestedViewModel2, TNestedViewModel3>(
        ISpecifiedNestedRoute<TParentViewModel, TNestedViewModel1> nestedRoute1,
        ISpecifiedNestedRoute<TNestedViewModel1, TNestedViewModel2> nestedRoute2,
        ISpecifiedNestedRoute<TNestedViewModel2, TNestedViewModel3> nestedRoute3,
        RouteOptions? routeOptions = null)
        where TParentViewModel : IRoutedViewModel
        where TNestedViewModel1 : IRoutedViewModel
        where TNestedViewModel2 : IRoutedViewModel
        where TNestedViewModel3 : IRoutedViewModel;
}
