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
    /// The navigation was cancelled while navigating away, or a user-initiated back/forward request was cancelled due to a current navigation being in
    /// progress.
    /// </summary>
    Cancelled,

    /// <summary>
    /// The navigation was cancelled and rerouted to a different route while navigating to it.
    /// </summary>
    Rerouted,
}
