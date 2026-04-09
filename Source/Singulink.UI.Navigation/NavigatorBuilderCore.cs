using System.Diagnostics.CodeAnalysis;
using Singulink.UI.Navigation.InternalServices;

namespace Singulink.UI.Navigation;

/// <summary>
/// Provides the shared builder infrastructure used by navigator implementations.
/// </summary>
public abstract class NavigatorBuilderCore : INavigatorBuilder
{
    internal Dictionary<Type, MappingInfo> ViewModelTypeToMappingInfo { get; } = [];

    internal Dictionary<Type, Func<object>> ViewModelTypeToDialogActivator { get; } = [];

    internal List<RoutePart> RouteParts { get; } = [];

    internal HashSet<Type> ViewModelTypesWithChildren { get; } = [];

    /// <summary>
    /// Gets the type that parent views must implement (e.g. <c>IParentView</c> in WinUI).
    /// </summary>
    protected abstract Type RequiredParentViewType { get; }

    /// <inheritdoc cref="INavigatorBuilder.MaxNavigationStacksSize"/>
    public int MaxNavigationStacksSize { get; private set; } = INavigatorBuilder.DefaultNavigationStacksSize;

    /// <inheritdoc cref="INavigatorBuilder.MaxBackStackCachedDepth"/>
    public int MaxBackStackCachedDepth { get; private set; } = INavigatorBuilder.DefaultMaxBackStackCachedDepth;

    /// <inheritdoc cref="INavigatorBuilder.MaxForwardStackCachedDepth"/>
    public int MaxForwardStackCachedDepth { get; private set; } = INavigatorBuilder.DefaultMaxForwardStackCachedDepth;

    /// <inheritdoc cref="INavigatorBuilder.Services"/>
    public IServiceProvider Services { get; set; } = EmptyServiceProvider.Instance;

    /// <inheritdoc cref="INavigatorBuilder.AddRoute"/>
    public void AddRoute(RoutePart routePart)
    {
        foreach (var registrationRoutePart in routePart.GetRegistrationParts())
            AddRoutePart(registrationRoutePart);
    }

    /// <inheritdoc cref="INavigatorBuilder.ConfigureNavigationStacks"/>
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

    /// <summary>
    /// Adds a route part registration to the builder.
    /// </summary>
    protected virtual void AddRoutePart(RoutePart routePart)
    {
        if (RouteParts.Contains(routePart))
            throw new InvalidOperationException("Route has already been added.");

        if (routePart.ParentViewModelType is not null)
        {
            if (!RouteParts.Any(rp => rp.ViewModelType == routePart.ParentViewModelType))
            {
                throw new InvalidOperationException(
                    $"Parent view model type '{routePart.ParentViewModelType}' does not have any routes added yet. Add parent routes before child routes.");
            }

            if (!ViewModelTypeToMappingInfo.TryGetValue(routePart.ParentViewModelType, out var parentMapping))
                throw new InvalidOperationException(
                    $"Parent view model type '{routePart.ParentViewModelType}' must be mapped via MapViewType<>() before child routes.");

            if (!parentMapping.ViewType.IsAssignableTo(RequiredParentViewType))
                throw new InvalidOperationException(
                    $"Parent view type '{parentMapping.ViewType}' must implement '{RequiredParentViewType.FullName}'.");

            ViewModelTypesWithChildren.Add(routePart.ParentViewModelType);
        }

        if (!ViewModelTypeToMappingInfo.ContainsKey(routePart.ViewModelType))
            throw new InvalidOperationException($"View model type '{routePart.ViewModelType}' must be mapped via MapViewType<>() before adding routes.");

        RouteParts.Add(routePart);
    }

    /// <summary>
    /// Maps a dialog view model type to a factory that creates the framework-specific dialog object for it.
    /// </summary>
    /// <param name="viewModelType">The dialog view model type.</param>
    /// <param name="activator">A factory delegate that creates a new framework dialog object instance.</param>
    /// <exception cref="InvalidOperationException">A dialog has already been mapped for the specified view model type.</exception>
    protected void MapDialog(Type viewModelType, Func<object> activator)
    {
        if (!ViewModelTypeToDialogActivator.TryAdd(viewModelType, activator))
            throw new InvalidOperationException($"A dialog has already been mapped for view model type '{viewModelType}'.");
    }

    /// <summary>
    /// Maps a default dialog activator for the specified view model type only if no mapping has been registered for it. Used by framework-specific
    /// builders to register fallback dialog implementations (e.g. a built-in message dialog).
    /// </summary>
    /// <param name="viewModelType">The dialog view model type.</param>
    /// <param name="activator">A factory delegate that creates a new framework dialog object instance.</param>
    protected void TryMapDefaultDialog(Type viewModelType, Func<object> activator)
    {
        ViewModelTypeToDialogActivator.TryAdd(viewModelType, activator);
    }

    /// <summary>
    /// Maps a routed view model type to a view type. Must be called before AddRoute() for that view model.
    /// </summary>
    protected virtual void MapRoutedView(
        [DynamicallyAccessedMembers(DAM.AllCtors)] Type viewModelType,
        [DynamicallyAccessedMembers(DAM.PublicDefaultCtor)] Type viewType)
    {
        if (ViewModelTypeToMappingInfo.ContainsKey(viewModelType))
            throw new InvalidOperationException($"View model type '{viewModelType}' already mapped.");

        var mappingInfo = MappingInfo.Create(viewModelType, viewType);
        ViewModelTypeToMappingInfo.Add(viewModelType, mappingInfo);
    }

    internal void Validate()
    {
        AddDefaultDialogActivators();
        ValidateCore();

        // Validate all mapped view model types have at least one route
        foreach (var viewModelType in ViewModelTypeToMappingInfo.Keys)
        {
            if (!RouteParts.Any(rp => rp.ViewModelType == viewModelType))
                throw new InvalidOperationException(
                    $"View model type '{viewModelType}' is mapped but has no routes. Remove the mapping or add a route.");
        }

        foreach (var routePart in RouteParts.Where(rp => ViewModelTypesWithChildren.Contains(rp.ViewModelType)))
            routePart.ValidateAsParent();
    }

    /// <summary>
    /// Performs framework-specific validation once the shared builder state has been populated.
    /// </summary>
    protected virtual void ValidateCore() { }

    /// <summary>
    /// Allows derived builders to register default dialog activators (typically via <see cref="TryMapDefaultDialog"/>) that should only apply when the
    /// consumer has not provided their own mapping. Invoked once when the navigator is being constructed, after the consumer's build action has run.
    /// </summary>
    protected virtual void AddDefaultDialogActivators() { }
}
