namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a navigator builder that can be used to configure a navigator.
/// </summary>
public interface INavigatorBuilder
{
    /// <summary>
    /// The default maximum size of the forward and back navigation stacks.
    /// </summary>
    public const int DefaultNavigationStacksSize = 20;

    /// <summary>
    /// The default maximum depth of cached views in the back navigation stack.
    /// </summary>
    public const int DefaultMaxBackStackCachedViewDepth = 5;

    /// <summary>
    /// The default maximum depth of cached views in the back in the forward navigation stack.
    /// </summary>
    public const int DefaultMaxForwardStackCachedViewDepth = 5;

    /// <summary>
    /// Gets the maximum size of the forward and back navigation stack. Defaults to <see cref="DefaultNavigationStacksSize"/>.
    /// </summary>
    public int MaxNavigationStacksSize { get; }

    /// <summary>
    /// Gets the maximum depth of cached views in the back navigation stack. Defaults to <see cref="DefaultMaxBackStackCachedViewDepth"/>. Views that are deeper
    /// than this will be recreated if navigated to again.
    /// </summary>
    public int MaxBackStackCachedViewDepth { get; }

    /// <summary>
    /// Gets the maximum depth of cached views in the forward navigation stack. Defaults to <see cref="DefaultMaxForwardStackCachedViewDepth"/>. Views that are
    /// deeper than this will be recreated if navigated to again.
    /// </summary>
    public int MaxForwardStackCachedViewDepth { get; }

    /// <summary>
    /// Adds a route to the specified route part. Parent parts must be added before child parts.
    /// </summary>
    public void AddRouteTo(RoutePart routePart);

    /// <summary>
    /// Configures navigation stack options.
    /// </summary>
    /// <param name="maxSize">The maximum depth of the forward and back navigation stack. Must be non-negative. Defaults to <see
    /// cref="DefaultNavigationStacksSize"/>.</param>
    /// <param name="maxBackCachedViewDepth">The maximum depth of cached views in the back navigation stack. Views that are deeper than this become eligible for
    /// garbage collection and will be recreated if navigated to again. Must be non-negative and value is clamped to the value of <paramref name="maxSize"/>.
    /// Defaults to <see cref="DefaultMaxBackStackCachedViewDepth"/>.</param>
    /// <param name="maxForwardCachedViewDepth"> The maximum depth of cached views in the forward navigation stack. Views that are deeper than this become
    /// eligible for garbage collection and will be recreated if navigated to again. Must be non-negative and value is clamped to the value of <paramref
    /// name="maxSize"/>. Defaults to <see cref="DefaultMaxForwardStackCachedViewDepth"/>.</param>
    void ConfigureNavigationStacks(
        int maxSize = DefaultNavigationStacksSize,
        int maxBackCachedViewDepth = DefaultMaxBackStackCachedViewDepth,
        int maxForwardCachedViewDepth = DefaultMaxForwardStackCachedViewDepth);
}
