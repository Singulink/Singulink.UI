namespace Singulink.UI.Navigation;

/// <summary>
/// Specifies the types of navigations that can occur.
/// </summary>
public enum NavigationType
{
    /// <summary>
    /// Indicates that a new navigation is occurring.
    /// </summary>
    New,

    /// <summary>
    /// Indicates that a forward navigation is occurring.
    /// </summary>
    Forward,

    /// <summary>
    /// Indicates that a back navigation is occurring.
    /// </summary>
    Back,
}
