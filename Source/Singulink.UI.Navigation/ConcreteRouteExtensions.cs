namespace Singulink.UI.Navigation;

/// <summary>
/// Provides extension methods for composing concrete route parts into multi-level routes.
/// </summary>
public static class ConcreteRouteExtensions
{
    /// <summary>
    /// Returns a route made up of the specified root route part followed by the specified child route part. Chain further levels with
    /// <see cref="ConcreteRoute{TLeafViewModel}.Then{TChildViewModel}(IConcreteChildRoutePart{TLeafViewModel, TChildViewModel})"/>.
    /// </summary>
    /// <typeparam name="TRootViewModel">The view model type of the root route part.</typeparam>
    /// <typeparam name="TChildViewModel">The view model type of the child route part.</typeparam>
    /// <param name="rootRoutePart">The concrete root route part.</param>
    /// <param name="childRoutePart">A concrete child route part whose parent view model type is the root route part's view model type.</param>
    public static ConcreteRoute<TChildViewModel> Then<TRootViewModel, TChildViewModel>(
        this IConcreteRootRoutePart<TRootViewModel> rootRoutePart,
        IConcreteChildRoutePart<TRootViewModel, TChildViewModel> childRoutePart)
        where TRootViewModel : class
        where TChildViewModel : class
    {
        return new([rootRoutePart, childRoutePart]);
    }

    /// <summary>
    /// Returns a route made up of the specified root route part followed by the specified child route part. The child's view model type is not known
    /// statically, so the returned route cannot be extended further.
    /// </summary>
    /// <typeparam name="TRootViewModel">The view model type of the root route part.</typeparam>
    /// <param name="rootRoutePart">The concrete root route part.</param>
    /// <param name="childRoutePart">A concrete child route part whose parent view model type is the root route part's view model type.</param>
    public static ConcreteRoute Then<TRootViewModel>(
        this IConcreteRootRoutePart<TRootViewModel> rootRoutePart,
        IConcreteChildRoutePart<TRootViewModel> childRoutePart)
        where TRootViewModel : class
    {
        return ConcreteRoute.Create([rootRoutePart, childRoutePart]);
    }

    /// <summary>
    /// Returns a partial route made up of the specified child route part followed by the specified grandchild route part. The grandchild's view model type
    /// is not known statically, so the returned partial route cannot be extended further.
    /// </summary>
    /// <typeparam name="TParentViewModel">The view model type that the first child route part belongs to.</typeparam>
    /// <typeparam name="TFirstChildViewModel">The view model type of the first child route part.</typeparam>
    /// <param name="firstChildRoutePart">The concrete child route part beneath the parent view model.</param>
    /// <param name="childRoutePart">A concrete child route part whose parent view model type is the first child route part's view model type.</param>
    public static ConcretePartialRoute<TParentViewModel> Then<TParentViewModel, TFirstChildViewModel>(
        this IConcreteChildRoutePart<TParentViewModel, TFirstChildViewModel> firstChildRoutePart,
        IConcreteChildRoutePart<TFirstChildViewModel> childRoutePart)
        where TParentViewModel : class
        where TFirstChildViewModel : class
    {
        return ConcretePartialRoute<TParentViewModel>.Create([firstChildRoutePart, childRoutePart]);
    }

    /// <summary>
    /// Returns a partial route made up of the specified child route part followed by the specified grandchild route part, for use with partial
    /// navigation beneath a parent of type <typeparamref name="TParentViewModel"/>. Chain further levels with
    /// <see cref="ConcretePartialRoute{TParentViewModel, TLeafViewModel}.Then{TChildViewModel}(IConcreteChildRoutePart{TLeafViewModel, TChildViewModel})"/>.
    /// </summary>
    /// <typeparam name="TParentViewModel">The view model type that the first child route part belongs to.</typeparam>
    /// <typeparam name="TFirstChildViewModel">The view model type of the first child route part.</typeparam>
    /// <typeparam name="TChildViewModel">The view model type of the appended child route part.</typeparam>
    /// <param name="firstChildRoutePart">The concrete child route part beneath the parent view model.</param>
    /// <param name="childRoutePart">A concrete child route part whose parent view model type is the first child route part's view model type.</param>
    public static ConcretePartialRoute<TParentViewModel, TChildViewModel> Then<TParentViewModel, TFirstChildViewModel, TChildViewModel>(
        this IConcreteChildRoutePart<TParentViewModel, TFirstChildViewModel> firstChildRoutePart,
        IConcreteChildRoutePart<TFirstChildViewModel, TChildViewModel> childRoutePart)
        where TParentViewModel : class
        where TFirstChildViewModel : class
        where TChildViewModel : class
    {
        return new([firstChildRoutePart, childRoutePart]);
    }
}
