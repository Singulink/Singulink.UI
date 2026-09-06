namespace Singulink.UI.Navigation;

/// <summary>
/// Identifies a routed view model lifecycle method invoked by the navigator during navigation.
/// </summary>
public enum ViewModelLifecycleStage
{
    /// <summary>
    /// <see cref="IRoutedViewModelBase.OnNavigatedToAsync"/>.
    /// </summary>
    NavigatedTo,

    /// <summary>
    /// <see cref="IRoutedViewModelBase.OnRouteNavigatedAsync"/>.
    /// </summary>
    RouteNavigated,

    /// <summary>
    /// <see cref="IRoutedViewModelBase.OnNavigatingAwayAsync"/>.
    /// </summary>
    NavigatingAway,

    /// <summary>
    /// <see cref="IRoutedViewModelBase.OnRouteNavigatingAsync"/>.
    /// </summary>
    RouteNavigating,

    /// <summary>
    /// <see cref="IRoutedViewModelBase.OnNavigatedAwayAsync"/>.
    /// </summary>
    NavigatedAway,
}
