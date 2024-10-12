namespace Singulink.UI.Navigation;

// TODO: Properly support frame navigator.

/// <summary>
/// Represents a navigator that sets the active view for a <see cref="Frame"/>.
/// </summary>
public class FrameNavigator(Frame navControl) : ViewNavigatorBase<Frame>(navControl)
{
    /// <inheritdoc/>
    protected override void SetActiveView(UIElement? view) => NavControl.Content = view;
}
