namespace Singulink.UI.Navigation.WinUI;

/// <summary>
/// Represents a navigator that sets the active view for a <see cref="ContentControl"/>.
/// </summary>
public class ContentControlNavigator : ViewNavigator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentControlNavigator"/> class with the specified <see cref="ContentControl"/>.
    /// </summary>
    public ContentControlNavigator(ContentControl navigationContentControl)
    {
        NavigationControl = navigationContentControl;
    }

    /// <inheritdoc/>
    public override ContentControl NavigationControl { get; }

    /// <inheritdoc/>
    protected internal override void SetActiveView(UIElement? view) => NavigationControl.Content = view;
}
