using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Singulink.UI.Icons.WinUI;

/// <summary>
/// A <see cref="FontIcon"/> that displays an <see cref="IIconGlyph"/>, automatically using its right-to-left glyph when the element's effective
/// <see cref="FrameworkElement.FlowDirection"/> is <see cref="FlowDirection.RightToLeft"/>. The <see cref="Glyph"/> property takes the icon; the
/// underlying glyph string is managed by the element and should not be set through a <see cref="FontIcon"/>-typed reference.
/// </summary>
public partial class AutoDirectionFontIcon : FontIcon
{
    /// <summary>
    /// Identifies the <see cref="Glyph"/> dependency property.
    /// </summary>
    public static new readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
        nameof(Glyph), typeof(IIconGlyph), typeof(AutoDirectionFontIcon), new PropertyMetadata(null, (d, e) => ((AutoDirectionFontIcon)d).UpdateGlyph()));

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoDirectionFontIcon"/> class.
    /// </summary>
    public AutoDirectionFontIcon()
    {
        RegisterPropertyChangedCallback(FlowDirectionProperty, (s, p) => ((AutoDirectionFontIcon)s).UpdateGlyph());
        Loaded += (s, e) => UpdateGlyph();
    }

    /// <summary>
    /// Gets or sets the icon to display.
    /// </summary>
    public new IIconGlyph? Glyph
    {
        get => (IIconGlyph?)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    private void UpdateGlyph()
    {
        SetValue(FontIcon.GlyphProperty, Glyph?.GetGlyph(FlowDirection is FlowDirection.RightToLeft) ?? string.Empty);
    }
}
