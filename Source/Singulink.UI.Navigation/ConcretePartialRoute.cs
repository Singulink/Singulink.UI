namespace Singulink.UI.Navigation;

/// <summary>
/// A concrete partial route made up of two or more child route parts beneath a parent view model of type <typeparamref name="TParentViewModel"/>, for
/// use with partial navigation. Build partial routes by calling
/// <see cref="ConcreteRouteExtensions.Then{TParentViewModel, TFirstChildViewModel, TChildViewModel}(IConcreteChildRoutePart{TParentViewModel, TFirstChildViewModel}, IConcreteChildRoutePart{TFirstChildViewModel, TChildViewModel})"/>
/// on a concrete child route part and chaining <see cref="ConcretePartialRoute{TParentViewModel, TLeafViewModel}.Then{TChildViewModel}(IConcreteChildRoutePart{TLeafViewModel, TChildViewModel})"/>
/// for each further level. Partial routes with a single child route part are represented by the child route part itself.
/// </summary>
/// <typeparam name="TParentViewModel">The view model type that the first child route part belongs to.</typeparam>
public abstract class ConcretePartialRoute<TParentViewModel>
    where TParentViewModel : class
{
    private protected ConcretePartialRoute(IReadOnlyList<IConcreteRoutePart> childRouteParts)
    {
        ChildRouteParts = childRouteParts;
    }

    internal static ConcretePartialRoute<TParentViewModel> Create(IReadOnlyList<IConcreteRoutePart> childRouteParts) => new TerminalRoute(childRouteParts);

    /// <summary>
    /// A partial route ending in a child route part whose view model type is not known statically, so no further parts can be appended.
    /// </summary>
    private sealed class TerminalRoute(IReadOnlyList<IConcreteRoutePart> childRouteParts) : ConcretePartialRoute<TParentViewModel>(childRouteParts);

    /// <summary>
    /// Gets the concrete child route parts that make up the partial route, starting with the child of the parent view model.
    /// </summary>
    public IReadOnlyList<IConcreteRoutePart> ChildRouteParts { get; }
}

/// <summary>
/// A concrete partial route whose last route part maps to the <typeparamref name="TLeafViewModel"/> view model type.
/// </summary>
/// <typeparam name="TParentViewModel">The view model type that the first child route part belongs to.</typeparam>
/// <typeparam name="TLeafViewModel">The view model type of the last route part.</typeparam>
public sealed class ConcretePartialRoute<TParentViewModel, TLeafViewModel> : ConcretePartialRoute<TParentViewModel>
    where TParentViewModel : class
    where TLeafViewModel : class
{
    internal ConcretePartialRoute(IReadOnlyList<IConcreteRoutePart> childRouteParts) : base(childRouteParts)
    {
    }

    /// <summary>
    /// Returns a new partial route with the specified child route part appended to this partial route.
    /// </summary>
    /// <typeparam name="TChildViewModel">The view model type of the child route part.</typeparam>
    /// <param name="childRoutePart">A concrete child route part whose parent view model type is this partial route's leaf view model type.</param>
    public ConcretePartialRoute<TParentViewModel, TChildViewModel> Then<TChildViewModel>(IConcreteChildRoutePart<TLeafViewModel, TChildViewModel> childRoutePart)
        where TChildViewModel : class
    {
        return new([.. ChildRouteParts, childRoutePart]);
    }

    /// <summary>
    /// Returns a new partial route with the specified child route part appended to this partial route. The child's view model type is not known
    /// statically, so the returned partial route cannot be extended further.
    /// </summary>
    /// <param name="childRoutePart">A concrete child route part whose parent view model type is this partial route's leaf view model type.</param>
    public ConcretePartialRoute<TParentViewModel> Then(IConcreteChildRoutePart<TLeafViewModel> childRoutePart)
    {
        return Create([.. ChildRouteParts, childRoutePart]);
    }
}
