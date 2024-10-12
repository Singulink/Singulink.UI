namespace Singulink.UI.Navigation;

/// <summary>
/// Specifies flags that provide additional information for items in a route that is navigating away from.
/// </summary>
/// <remarks>
/// See the property documentation on <see cref="NavigatingArgs"/> for more information on what these flags mean and how they are used.
/// </remarks>
[Flags]
public enum NavigatingFlags
{
    /// <summary>
    /// Default value with no flags specified.
    /// </summary>
    None = 0,

    /// <summary>
    /// Indicates that the view model will be navigated away from if the navigation is not canceled.
    /// </summary>
    WillBeNavigatedFrom = 1,
}
