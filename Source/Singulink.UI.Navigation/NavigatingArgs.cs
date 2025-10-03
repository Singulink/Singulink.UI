using Singulink.Enums;

namespace Singulink.UI.Navigation;

/// <summary>
/// Provides information to a view model when its route is navigating away and allows it to be cancelled.
/// </summary>
public class NavigatingArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NavigatingArgs"/> class.
    /// </summary>
    public NavigatingArgs(INavigator navigator, NavigationType navigationType)
    {
        navigationType.ThrowIfNotValid(nameof(navigationType));

        Navigator = navigator;
        NavigationType = navigationType;
    }

    /// <summary>
    /// Gets the navigator that is performing the navigation.
    /// </summary>
    public INavigator Navigator { get; }

    /// <summary>
    /// Gets the type of navigation that is occurring.
    /// </summary>
    public NavigationType NavigationType { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the new navigation should be canceled and the current route should remain active.
    /// </summary>
    public bool Cancel { get; set; }
}
