namespace Singulink.UI.Navigation.WinUI;

#pragma warning disable SA1513 // Closing brace should be followed by blank line

/// <inheritdoc cref="INavigatorBuilder" />
public class NavigatorBuilder : INavigatorBuilder
{
    internal Dictionary<Type, ViewInfo> ViewModelTypeToViewInfo { get; } = [];

    internal Dictionary<Type, Func<ContentDialog>> ViewModelTypeToDialogActivator { get; } = [];

    internal List<RoutePart> RouteParts { get; } = [];

    /// <inheritdoc cref="INavigatorBuilder.MaxNavigationStacksSize"/>
    public int MaxNavigationStacksSize { get; private set; } = INavigatorBuilder.DefaultNavigationStacksSize;

    /// <inheritdoc cref="INavigatorBuilder.MaxBackStackCachedViewDepth"/>
    public int MaxBackStackCachedViewDepth { get; private set; } = INavigatorBuilder.DefaultMaxBackStackCachedViewDepth;

    /// <inheritdoc cref="INavigatorBuilder.MaxForwardStackCachedViewDepth"/>
    public int MaxForwardStackCachedViewDepth { get; private set; } = INavigatorBuilder.DefaultMaxForwardStackCachedViewDepth;

    internal NavigatorBuilder() { }

    /// <inheritdoc cref="INavigatorBuilder.AddRouteTo"/>
    public void AddRouteTo(RoutePart routePart)
    {
        if (RouteParts.Contains(routePart))
            throw new InvalidOperationException("Route has already been added.");

        if (routePart.ParentViewModelType is not null)
        {
            if (!RouteParts.Any(r => r.ViewModelType == routePart.ParentViewModelType))
                throw new InvalidOperationException($"Parent view model type '{routePart.ParentViewModelType}' does not have any routes added yet. Add parent routes before child routes.");

            var parentViewType = ViewModelTypeToViewInfo[routePart.ParentViewModelType].ViewType;

            if (!parentViewType.IsAssignableTo(typeof(IParentView)))
                throw new InvalidOperationException($"Parent view type '{parentViewType}' must implement '{typeof(IParentView)}' in order to support child routes.");
        }

        if (!ViewModelTypeToViewInfo.ContainsKey(routePart.ViewModelType))
            throw new InvalidOperationException($"View model type '{routePart.ViewModelType}' has not been mapped to a view.");

        RouteParts.Add(routePart);
    }

    /// <summary>
    /// Maps a routed view model to a routed view.
    /// </summary>
    public void MapRoutedView<TViewModel, TView>()
        where TViewModel : class, IRoutedViewModelBase
        where TView : UIElement, IRoutedView<TViewModel>, new()
    {
        ViewModelTypeToViewInfo.Add(typeof(TViewModel), new ViewInfo(typeof(TView), () => new TView()));
    }

    /// <summary>
    /// Maps a view model to a dialog.
    /// </summary>
    public void MapDialog<TViewModel, TDialog>()
        where TViewModel : class, IDialogViewModel
        where TDialog : ContentDialog, new()
    {
        ViewModelTypeToDialogActivator.Add(typeof(TViewModel), () => new TDialog());
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
        foreach (var (viewModelType, viewInfo) in ViewModelTypeToViewInfo)
        {
            if (!RouteParts.Any(r => r.ViewModelType == viewModelType))
                throw new InvalidOperationException($"View model '{viewModelType}' (mapped to view '{viewInfo.ViewType}') has no routes to it.");
        }
    }
}
