namespace Singulink.UI.Navigation.WinUI;

// TODO: Properly support frame navigator.

/// <summary>
/// Represents a navigator that sets the active view for a <see cref="Frame"/>.
/// </summary>
public class FrameNavigator : ViewNavigatorBase<Frame>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FrameNavigator"/> class with the specified <see cref="Frame"/>.
    /// </summary>
    /// <param name="frame">The frame to be used for navigation.</param>
    public FrameNavigator(Frame frame) : base(frame)
    {
    }

    /// <inheritdoc/>
    protected override void SetActiveView(UIElement? view) => NavControl.Content = view;
}
