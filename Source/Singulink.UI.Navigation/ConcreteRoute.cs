namespace Singulink.UI.Navigation;

/// <summary>
/// A concrete route made up of a root route part followed by one or more child route parts. Build routes by calling
/// <see cref="ConcreteRouteExtensions.Then{TRootViewModel, TChildViewModel}(IConcreteRootRoutePart{TRootViewModel}, IConcreteChildRoutePart{TRootViewModel, TChildViewModel})"/>
/// on a concrete root route part and chaining <see cref="ConcreteRoute{TLeafViewModel}.Then{TChildViewModel}(IConcreteChildRoutePart{TLeafViewModel, TChildViewModel})"/>
/// for each further level. Routes with only a root route part are represented by the root route part itself.
/// </summary>
public abstract class ConcreteRoute
{
    private protected ConcreteRoute(IReadOnlyList<IConcreteRoutePart> routeParts)
    {
        RouteParts = routeParts;
    }

    internal static ConcreteRoute Create(IReadOnlyList<IConcreteRoutePart> routeParts) => new TerminalRoute(routeParts);

    /// <summary>
    /// Gets the concrete route parts that make up the route, starting with the root route part.
    /// </summary>
    public IReadOnlyList<IConcreteRoutePart> RouteParts { get; }

    /// <summary>
    /// Gets the route path.
    /// </summary>
    public string Path => Route.GetRoute(RouteParts);

    /// <inheritdoc/>
    public override string ToString() => Path;

    /// <summary>
    /// A route ending in a child route part whose view model type is not known statically, so no further parts can be appended.
    /// </summary>
    private sealed class TerminalRoute(IReadOnlyList<IConcreteRoutePart> routeParts) : ConcreteRoute(routeParts);
}

/// <summary>
/// A concrete route whose last route part maps to the <typeparamref name="TLeafViewModel"/> view model type.
/// </summary>
/// <typeparam name="TLeafViewModel">The view model type of the last route part.</typeparam>
public sealed class ConcreteRoute<TLeafViewModel> : ConcreteRoute
    where TLeafViewModel : class
{
    internal ConcreteRoute(IReadOnlyList<IConcreteRoutePart> routeParts) : base(routeParts)
    {
    }

    /// <summary>
    /// Returns a new route with the specified child route part appended to this route.
    /// </summary>
    /// <typeparam name="TChildViewModel">The view model type of the child route part.</typeparam>
    /// <param name="childRoutePart">A concrete child route part whose parent view model type is this route's leaf view model type.</param>
    public ConcreteRoute<TChildViewModel> Then<TChildViewModel>(IConcreteChildRoutePart<TLeafViewModel, TChildViewModel> childRoutePart)
        where TChildViewModel : class
    {
        return new([.. RouteParts, childRoutePart]);
    }

    /// <summary>
    /// Returns a new route with the specified child route part appended to this route. The child's view model type is not known statically, so the
    /// returned route cannot be extended further.
    /// </summary>
    /// <param name="childRoutePart">A concrete child route part whose parent view model type is this route's leaf view model type.</param>
    public ConcreteRoute Then(IConcreteChildRoutePart<TLeafViewModel> childRoutePart)
    {
        return Create([.. RouteParts, childRoutePart]);
    }
}
