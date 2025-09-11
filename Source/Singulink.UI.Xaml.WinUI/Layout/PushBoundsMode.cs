namespace Singulink.UI.Xaml.Layout;

/// <summary>
/// Specifies which directions to push layout bounds.
/// </summary>
public enum PushBoundsMode
{
    /// <summary>
    /// Do not push the layout bounds.
    /// </summary>
    None,

    /// <summary>
    /// Push the layout bounds horizontally.
    /// </summary>
    Horizontal,

    /// <summary>
    /// Push the layout bounds vertically.
    /// </summary>
    Vertical,

    /// <summary>
    /// Push the layout bounds in both the horizontal and vertical directions.
    /// </summary>
    Both,
}
