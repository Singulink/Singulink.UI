namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a navigator builder that can be used to configure a navigator.
/// </summary>
public interface INavigatorBuilder
{
    /// <summary>
    /// The default value of <see cref="MaxNavigationStacksSize"/>.
    /// </summary>
    public const int DefaultNavigationStacksSize = 20;

    /// <summary>
    /// The default value of <see cref="MaxBackStackCachedDepth"/>.
    /// </summary>
    public const int DefaultMaxBackStackCachedDepth = 5;

    /// <summary>
    /// The default value of <see cref="MaxForwardStackCachedDepth"/>.
    /// </summary>
    public const int DefaultMaxForwardStackCachedDepth = 5;

    /// <summary>
    /// Gets the maximum number of entries in the forward and back navigation stack. Defaults to <see cref="DefaultNavigationStacksSize"/>.
    /// </summary>
    public int MaxNavigationStacksSize { get; }

    /// <summary>
    /// Gets the maximum depth of cached views and view models in the back navigation stack. Views and view models that are deeper than this are disposed and
    /// will be recreated if navigated to again. Defaults to <see cref="DefaultMaxBackStackCachedDepth"/>.
    /// </summary>
    public int MaxBackStackCachedDepth { get; }

    /// <summary>
    /// Gets the maximum depth of cached views and view models in the forward navigation stack. Views and view models that are deeper than this will be
    /// recreated if navigated to again. Defaults to <see cref="DefaultMaxForwardStackCachedDepth"/>.
    /// </summary>
    public int MaxForwardStackCachedDepth { get; }

    /// <summary>
    /// Gets or sets the service provider that will be used to resolve root services for view models. Defaults to an empty service provider.
    /// </summary>
    public IServiceProvider Services { get; set; }

    /// <summary>
    /// Adds a route to the specified route part. Parent parts must be added before child parts.
    /// </summary>
    public void AddRouteTo(RoutePart routePart);

    /// <summary>
    /// Configures navigation stack options.
    /// </summary>
    /// <param name="maxSize">The maximum depth of the forward and back navigation stack. Must be non-negative. Defaults to <see
    /// cref="DefaultNavigationStacksSize"/>.</param>
    /// <param name="maxBackCachedDepth">The maximum depth of cached views in the back navigation stack. Views that are deeper than this become eligible for
    /// garbage collection and will be recreated if navigated to again. Must be non-negative and value is clamped to the value of <paramref name="maxSize"/>.
    /// Defaults to <see cref="DefaultMaxBackStackCachedDepth"/>.</param>
    /// <param name="maxForwardCachedDepth"> The maximum depth of cached views in the forward navigation stack. Views that are deeper than this become
    /// eligible for garbage collection and will be recreated if navigated to again. Must be non-negative and value is clamped to the value of <paramref
    /// name="maxSize"/>. Defaults to <see cref="DefaultMaxForwardStackCachedDepth"/>.</param>
    void ConfigureNavigationStacks(
        int maxSize = DefaultNavigationStacksSize,
        int maxBackCachedDepth = DefaultMaxBackStackCachedDepth,
        int maxForwardCachedDepth = DefaultMaxForwardStackCachedDepth);
}
