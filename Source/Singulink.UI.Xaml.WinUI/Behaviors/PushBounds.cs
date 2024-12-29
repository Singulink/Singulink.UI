using Windows.Foundation;

namespace Singulink.UI.Xaml.Layout;

/// <summary>
/// Invisible control that sizes itself to take up all the available space in its layout container.
/// </summary>
/// <remarks>
/// Typical usage is to place this control as the first child element in a Grid (often times with MaxWidth or MaxHeight set on the grid) to force the grid to
/// stretch to the space available to it, regardless of its contents. This can be particularly useful inside content dialogs to prevent the dialog from
/// shrinking to fit its contents or changing size as the dialog's content changes.
/// </remarks>
public sealed partial class PushBounds : FrameworkElement
{
    /// <summary>
    /// Identifies the <see cref="Mode"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(
        nameof(Mode),
        typeof(PushBoundsMode),
        typeof(PushBounds),
        new PropertyMetadata(PushBoundsMode.Both, OnModeChanged));

    /// <summary>
    /// Gets or sets the mode that specifies which directions to push the layout bounds.
    /// </summary>
    public PushBoundsMode Mode
    {
        get => (PushBoundsMode)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PushBounds"/> class.
    /// </summary>
    public PushBounds()
    {
        IsHitTestVisible = false;
        IsTabStop = false;
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var mode = Mode;
        double desiredWidth = 0;
        double desiredHeight = 0;

        if (mode is PushBoundsMode.Both or PushBoundsMode.Horizontal && double.IsFinite(availableSize.Width))
            desiredWidth = availableSize.Width;

        if (mode is PushBoundsMode.Both or PushBoundsMode.Vertical && double.IsFinite(availableSize.Height))
            desiredHeight = availableSize.Height;

        return new Size(desiredWidth, desiredHeight);
    }

    private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var newValue = (PushBoundsMode)e.NewValue;
        var pb = (PushBounds)d;

        if ((uint)newValue > (uint)PushBoundsMode.Both)
        {
            pb.Mode = (PushBoundsMode)e.OldValue;
            throw new ArgumentException("Invalid mode.");
        }

        pb.UpdateLayout();
    }
}

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
