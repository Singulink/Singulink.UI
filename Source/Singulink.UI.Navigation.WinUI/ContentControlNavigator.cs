namespace Singulink.UI.Navigation.WinUI;

/// <summary>
/// Represents a navigator that sets the active view for a <see cref="ContentControl"/>.
/// </summary>
public class ContentControlNavigator : ViewNavigatorBase<ContentControl>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentControlNavigator"/> class with the specified <see cref="ContentControl"/>.
    /// </summary>
    public ContentControlNavigator(ContentControl contentControl) : base(contentControl)
    {
    }

    /// <inheritdoc/>
    protected override void SetActiveView(UIElement? view) => NavControl.Content = view;
}
