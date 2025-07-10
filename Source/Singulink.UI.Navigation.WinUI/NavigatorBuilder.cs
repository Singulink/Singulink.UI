namespace Singulink.UI.Navigation.WinUI;

#pragma warning disable SA1513 // Closing brace should be followed by blank line

/// <summary>
/// Represents a builder for mapping routed views to view models.
/// </summary>
public class NavigatorBuilder : INavigatorBuilder
{
    internal Dictionary<Type, ViewInfo> VmTypeToViewInfo { get; } = [];

    internal Dictionary<Type, Func<ContentDialog>> VmTypeToDialogActivator { get; } = [];

    internal List<RouteBase> RouteList { get; } = [];

    /// <inheritdoc cref="INavigatorBuilder.MaxNavigationStacksSize"/>
    public int MaxNavigationStacksSize { get; private set; } = INavigatorBuilder.DefaultNavigationStacksSize;

    /// <inheritdoc cref="INavigatorBuilder.MaxBackStackCachedViewDepth"/>
    public int MaxBackStackCachedViewDepth { get; private set; } = INavigatorBuilder.DefaultMaxBackStackCachedViewDepth;

    /// <inheritdoc cref="INavigatorBuilder.MaxForwardStackCachedViewDepth"/>
    public int MaxForwardStackCachedViewDepth { get; private set; } = INavigatorBuilder.DefaultMaxForwardStackCachedViewDepth;

    internal NavigatorBuilder() { }

    /// <inheritdoc cref="INavigatorBuilder.AddRoute"/>
    public void AddRoute(RouteBase route)
    {
        if (RouteList.Contains(route))
            throw new InvalidOperationException("Route has already been added.");

        if (route.ParentViewModelType is not null)
        {
            if (!RouteList.Any(r => r.ViewModelType == route.ParentViewModelType))
                throw new InvalidOperationException($"Route's parent view model type '{route.ParentViewModelType}' does not have any routes added yet. Add parent routes before child routes.");

            var parentViewType = VmTypeToViewInfo[route.ParentViewModelType].ViewType;

            if (!parentViewType.IsAssignableTo(typeof(IParentView)))
                throw new InvalidOperationException($"Route's parent view type '{parentViewType}' must implement '{typeof(IParentView)}' in order to support nested routes.");
        }

        if (!VmTypeToViewInfo.ContainsKey(route.ViewModelType))
            throw new InvalidOperationException($"Route's view model type '{route.ViewModelType}' has not been mapped to a view.");

        RouteList.Add(route);
    }

    /// <summary>
    /// Maps a routed view model to a routed view.
    /// </summary>
    public void MapRoutedView<TViewModel, TView>()
        where TViewModel : class, IRoutedViewModelBase
        where TView : UIElement, IRoutedView<TViewModel>, new()
    {
        VmTypeToViewInfo.Add(typeof(TViewModel), new ViewInfo(typeof(TView), () => new TView()));
    }

    /// <summary>
    /// Maps a view model to a dialog.
    /// </summary>
    public void MapDialog<TViewModel, TDialog>()
        where TViewModel : class, IDialogViewModel
        where TDialog : ContentDialog, new()
    {
        VmTypeToDialogActivator.Add(typeof(TViewModel), () => new TDialog());
    }

    /// <inheritdoc cref="INavigatorBuilder.ConfigureNavigationStacks"/>"
    public void ConfigureNavigationStacks(
        int maxSize = INavigatorBuilder.DefaultNavigationStacksSize,
        int maxBackCachedViewDepth = INavigatorBuilder.DefaultMaxBackStackCachedViewDepth,
        int maxForwardCachedViewDepth = INavigatorBuilder.DefaultMaxForwardStackCachedViewDepth)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxSize, 0, nameof(maxSize));
        ArgumentOutOfRangeException.ThrowIfLessThan(maxBackCachedViewDepth, 0, nameof(maxBackCachedViewDepth));
        ArgumentOutOfRangeException.ThrowIfLessThan(maxForwardCachedViewDepth, 0, nameof(maxForwardCachedViewDepth));

        MaxNavigationStacksSize = maxSize;
        MaxBackStackCachedViewDepth = Math.Min(maxBackCachedViewDepth, maxSize);
        MaxForwardStackCachedViewDepth = Math.Min(maxForwardCachedViewDepth, maxSize);
    }

    internal void Validate()
    {
        foreach (var (viewModelType, viewInfo) in VmTypeToViewInfo)
        {
            if (!RouteList.Any(r => r.ViewModelType == viewModelType))
                throw new InvalidOperationException($"View model '{viewModelType}' (mapped to view '{viewInfo.ViewType}') has no routes to it.");
        }
    }
}
