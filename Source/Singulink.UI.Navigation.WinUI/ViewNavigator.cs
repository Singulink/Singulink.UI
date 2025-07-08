namespace Singulink.UI.Navigation.WinUI;

/// <summary>
/// Provides methods to create navigators for different types of controls.
/// </summary>
public static class ViewNavigator
{
    /// <summary>
    /// Creates a <see cref="ContentControlNavigator"/> for the specified <see cref="ContentControl"/>.
    /// </summary>
    /// <param name="contentControl">The content control to be used for navigation.</param>
    public static ContentControlNavigator Create(ContentControl contentControl) => new(contentControl);

    /// <summary>
    /// Creates a <see cref="FrameNavigator"/> for the specified <see cref="Frame"/>.
    /// </summary>
    /// <param name="frame">The frame to be used for navigation.</param>
    public static FrameNavigator Create(Frame frame) => new(frame);

    /// <summary>
    /// Creates a <see cref="PanelNavigator"/> for the specified <see cref="Panel"/>.
    /// </summary>
    /// <param name="panel">The panel to be used for navigation.</param>
    /// <param name="maxCachedViews">Number of views that should be cached in the panel before they are removed from the visual tree.</param>
    public static PanelNavigator Create(Panel panel, int maxCachedViews = 5) => new(panel, maxCachedViews);
}
