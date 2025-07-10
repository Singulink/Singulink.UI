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
    /// Adds a route to the navigator.
    /// </summary>
    public void AddRoute(RouteBase route);

    /// <summary>
    /// Configures navigation stack options.
    /// </summary>
    /// <param name="maxSize">The maximum depth of the forward and back navigation stack. Must be non-negative.</param>
    /// <param name="maxBackCachedViewDepth">The maximum depth of cached views in the back navigation stack. Defaults to <see
    /// cref="DefaultMaxBackStackCachedViewDepth"/>. Must be non-negative and value is clamped to the value of <paramref name="maxSize"/>. Views that are
    /// deeper than this will be recreated if navigated to again.</param>
    /// <param name="maxForwardCachedViewDepth"> The maximum depth of cached views in the forward navigation stack. Defaults to <see
    /// cref="DefaultMaxForwardStackCachedViewDepth"/>. Must be non-negative and value is clamped to the value of <paramref name="maxSize"/>. Views that are
    /// deeper than this will be recreated if navigated to again.</param>
    void ConfigureNavigationStacks(
        int maxSize = DefaultNavigationStacksSize,
        int maxBackCachedViewDepth = DefaultMaxBackStackCachedViewDepth,
        int maxForwardCachedViewDepth = DefaultMaxForwardStackCachedViewDepth);
}
