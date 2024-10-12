namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a navigator that sets the active view for a <see cref="ContentControl"/>.
/// </summary>
public class ContentControlNavigator(ContentControl navControl) : ViewNavigatorBase<ContentControl>(navControl)
{
    /// <inheritdoc/>
    protected override void SetActiveView(UIElement? view) => NavControl.Content = view;
}
