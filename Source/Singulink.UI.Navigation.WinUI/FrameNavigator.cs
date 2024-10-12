namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a navigator that sets the active view for a <see cref="Frame"/>.
/// </summary>
public class FrameNavigator : ViewNavigatorBase<Frame>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FrameNavigator"/> class using the specified function to get the target frame.
    /// </summary>
    public FrameNavigator(Func<Frame> getTargetFrameFunc) : base(getTargetFrameFunc) { }

    /// <inheritdoc/>
    protected override void SetActiveView(UIElement view)
    {
        var frame = TargetControl;
        frame.Content = view;
        frame.BackStack.Clear();
    }
}
