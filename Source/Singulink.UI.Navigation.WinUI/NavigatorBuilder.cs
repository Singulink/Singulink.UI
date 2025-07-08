namespace Singulink.UI.Navigation.WinUI;

/// <summary>
/// Represents a builder for mapping routed views to view models.
/// </summary>
public class NavigatorBuilder : INavigatorBuilder
{
    internal Dictionary<Type, ViewInfo> VmTypeToViewInfo { get; } = [];

    internal Dictionary<Type, Func<ContentDialog>> VmTypeToDialogCtor { get; } = [];

    internal List<RouteBase> RouteList { get; } = [];

    /// <inheritdoc cref="INavigatorBuilder.MaxBackStackDepth"/>
    public int MaxBackStackDepth { get; set; } = 15;

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
        where TDialog : ContentDialog, new()
    {
        VmTypeToDialogCtor.Add(typeof(TViewModel), () => new TDialog());
    }

    internal void Validate()
    {
        foreach (var (viewModelType, viewInfo) in VmTypeToViewInfo)
            if (!RouteList.Any(r => r.ViewModelType == viewModelType))
                throw new InvalidOperationException($"View model '{viewModelType}' (mapped to view '{viewInfo.ViewType}') has no routes to it.");
    }
}
