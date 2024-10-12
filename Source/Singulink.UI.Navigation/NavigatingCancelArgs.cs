namespace Singulink.UI.Navigation;

/// <summary>
/// Allows a navigation away from a view model to be canceled.
/// </summary>
public class NavigatingCancelArgs
{
    /// <summary>
    /// Gets or sets a value indicating whether the navigation should be canceled.
    /// </summary>
    public bool Cancel { get; set; }
}
