namespace Singulink.UI.Navigation;

/// <summary>
/// Represents the result of a navigation operation.
/// </summary>
public enum NavigationResult
{
    /// <summary>
    /// The navigation was successful.
    /// </summary>
    Success,

    /// <summary>
    /// The navigation was cancelled while navigating away from the current route.
    /// </summary>
    Cancelled,
}
