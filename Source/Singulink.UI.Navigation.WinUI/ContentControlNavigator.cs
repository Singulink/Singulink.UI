namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a navigator that sets the active view for a <see cref="ContentControl"/>.
/// </summary>
public class ContentControlNavigator : ViewNavigatorBase<ContentControl>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentControlNavigator"/> class using the specified content control.
    /// </summary>
    public ContentControlNavigator(Func<ContentControl> getTargetContentControlFunc) : base(getTargetContentControlFunc) { }

    /// <inheritdoc/>
    protected override void SetActiveView(UIElement view) => TargetControl.Content = view;
}
