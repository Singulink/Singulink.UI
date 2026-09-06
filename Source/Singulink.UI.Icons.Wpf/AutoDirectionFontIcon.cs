using System.Windows;
using System.Windows.Controls;

namespace Singulink.UI.Icons.Wpf;

/// <summary>
/// A <see cref="TextBlock"/> that displays an <see cref="IIconGlyph"/> from an icon font, automatically using its right-to-left glyph when the
/// element's effective <see cref="FrameworkElement.FlowDirection"/> is <see cref="FlowDirection.RightToLeft"/>. Set <see cref="Control.FontFamily"/>
/// to the icon pack's font (typically through a style). The <see cref="TextBlock.Text"/> property is managed by the element and should not be set.
/// </summary>
public class AutoDirectionFontIcon : TextBlock
{
    /// <summary>
    /// Identifies the <see cref="Glyph"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
        nameof(Glyph), typeof(IIconGlyph), typeof(AutoDirectionFontIcon), new PropertyMetadata(null, (d, e) => ((AutoDirectionFontIcon)d).UpdateText()));

    /// <summary>
    /// Gets or sets the icon to display.
    /// </summary>
    public IIconGlyph? Glyph
    {
        get => (IIconGlyph?)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    static AutoDirectionFontIcon()
    {
        FlowDirectionProperty.OverrideMetadata(typeof(AutoDirectionFontIcon), new FrameworkPropertyMetadata((d, e) => ((AutoDirectionFontIcon)d).UpdateText()));
    }

    private void UpdateText()
    {
        Text = Glyph?.GetGlyph(FlowDirection is FlowDirection.RightToLeft) ?? string.Empty;
    }
}
