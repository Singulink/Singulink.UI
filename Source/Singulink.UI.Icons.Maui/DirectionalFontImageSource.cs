using Microsoft.Maui.Controls;

namespace Singulink.UI.Icons.Maui;

/// <summary>
/// A <see cref="FontImageSource"/> that displays an <see cref="IIconGlyph"/> using the glyph for a given <see cref="FlowDirection"/>. Image sources
/// are not visual elements and have no effective flow direction of their own, so the direction must be set (typically bound to the flow direction
/// of the page or the hosting element).
/// </summary>
public class DirectionalFontImageSource : FontImageSource
{
    /// <summary>
    /// Identifies the <see cref="Icon"/> bindable property.
    /// </summary>
    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(IIconGlyph), typeof(DirectionalFontImageSource), null, propertyChanged: (b, o, n) => ((DirectionalFontImageSource)b).UpdateGlyph());

    /// <summary>
    /// Identifies the <see cref="FlowDirection"/> bindable property.
    /// </summary>
    public static readonly BindableProperty FlowDirectionProperty = BindableProperty.Create(
        nameof(FlowDirection), typeof(FlowDirection), typeof(DirectionalFontImageSource), FlowDirection.LeftToRight, propertyChanged: (b, o, n) => ((DirectionalFontImageSource)b).UpdateGlyph());

    /// <summary>
    /// Gets or sets the icon to display.
    /// </summary>
    public IIconGlyph? Icon
    {
        get => (IIconGlyph?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Gets or sets the flow direction that determines which glyph of the icon is displayed. <see cref="FlowDirection.MatchParent"/> is treated as
    /// left-to-right since an image source has no parent to match.
    /// </summary>
    public FlowDirection FlowDirection
    {
        get => (FlowDirection)GetValue(FlowDirectionProperty);
        set => SetValue(FlowDirectionProperty, value);
    }

    private void UpdateGlyph()
    {
        Glyph = Icon?.GetGlyph(FlowDirection is FlowDirection.RightToLeft) ?? string.Empty;
    }
}
