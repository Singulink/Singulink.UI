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
    /// The navigation was cancelled while navigating away.
    /// </summary>
    Cancelled,

    /// <summary>
    /// The navigation was cancelled and rerouted to another route while navigating to it.
    /// </summary>
    Rerouted,
}
