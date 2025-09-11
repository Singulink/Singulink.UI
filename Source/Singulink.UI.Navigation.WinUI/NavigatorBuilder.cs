using System.Diagnostics.CodeAnalysis;
using Singulink.UI.Navigation.InternalServices;

namespace Singulink.UI.Navigation.WinUI;

#pragma warning disable SA1513 // Closing brace should be followed by blank line

/// <inheritdoc cref="INavigatorBuilder" />
public class NavigatorBuilder : INavigatorBuilder
{
    internal Dictionary<Type, MappingInfo> ViewModelTypeToMappingInfo { get; } = [];

    internal Dictionary<Type, Func<ContentDialog>> ViewModelTypeToDialogActivator { get; } = [];

    internal List<RoutePart> RouteParts { get; } = [];

    /// <inheritdoc cref="INavigatorBuilder.MaxNavigationStacksSize"/>
    public int MaxNavigationStacksSize { get; private set; } = INavigatorBuilder.DefaultNavigationStacksSize;

    /// <inheritdoc cref="INavigatorBuilder.MaxBackStackCachedDepth"/>
    public int MaxBackStackCachedDepth { get; private set; } = INavigatorBuilder.DefaultMaxBackStackCachedDepth;

    /// <inheritdoc cref="INavigatorBuilder.MaxForwardStackCachedDepth"/>
    public int MaxForwardStackCachedDepth { get; private set; } = INavigatorBuilder.DefaultMaxForwardStackCachedDepth;

    /// <inheritdoc cref="INavigatorBuilder.Services"/>
    public IServiceProvider Services { get; set; } = EmptyServiceProvider.Instance;

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

            var parentViewType = ViewModelTypeToMappingInfo[routePart.ParentViewModelType].ViewType;

            if (!parentViewType.IsAssignableTo(typeof(IParentView)))
                throw new InvalidOperationException($"Parent view type '{parentViewType}' must implement '{typeof(IParentView)}' in order to support child routes.");
        }

        if (!ViewModelTypeToMappingInfo.ContainsKey(routePart.ViewModelType))
            throw new InvalidOperationException($"View model type '{routePart.ViewModelType}' has not been mapped.");

        RouteParts.Add(routePart);
    }

    /// <summary>
    /// Maps a routed view model to a routed view.
    /// </summary>
    public void MapRoutedView<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TViewModel,
        TView>()
        where TViewModel : class, IRoutedViewModelBase
        where TView : FrameworkElement, new()
    {
        ViewModelTypeToMappingInfo.Add(typeof(TViewModel), MappingInfo.Create(typeof(TViewModel), typeof(TView)));
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
        int maxBackCachedDepth = INavigatorBuilder.DefaultMaxBackStackCachedDepth,
        int maxForwardCachedDepth = INavigatorBuilder.DefaultMaxForwardStackCachedDepth)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxSize, 0, nameof(maxSize));
        ArgumentOutOfRangeException.ThrowIfLessThan(maxBackCachedDepth, 0, nameof(maxBackCachedDepth));
        ArgumentOutOfRangeException.ThrowIfLessThan(maxForwardCachedDepth, 0, nameof(maxForwardCachedDepth));

        MaxNavigationStacksSize = maxSize;
        MaxBackStackCachedDepth = Math.Min(maxBackCachedDepth, maxSize);
        MaxForwardStackCachedDepth = Math.Min(maxForwardCachedDepth, maxSize);
    }

    internal void Validate()
    {
        foreach (var (viewModelType, viewInfo) in ViewModelTypeToMappingInfo)
        {
            if (!RouteParts.Any(r => r.ViewModelType == viewModelType))
                throw new InvalidOperationException($"View model '{viewModelType}' (mapped to view '{viewInfo.ViewType}') has no routes to it.");
        }
    }
}
