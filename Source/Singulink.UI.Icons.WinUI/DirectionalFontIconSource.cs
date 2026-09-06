using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Singulink.UI.Icons.WinUI;

/// <summary>
/// A <see cref="FontIconSource"/> that displays an <see cref="IIconGlyph"/> using the glyph for a given <see cref="FlowDirection"/>. Icon sources are
/// not part of the visual tree and the elements that hosts create from them cannot be intercepted, so unlike <see cref="AutoDirectionFontIcon"/> the
/// direction cannot be detected automatically and must be set (typically bound to the flow direction of the hosting element or the root).
/// </summary>
public partial class DirectionalFontIconSource : FontIconSource
{
    /// <summary>
    /// Identifies the <see cref="Icon"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon), typeof(IIconGlyph), typeof(DirectionalFontIconSource), new PropertyMetadata(null, (d, e) => ((DirectionalFontIconSource)d).UpdateGlyph()));

    /// <summary>
    /// Identifies the <see cref="FlowDirection"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty FlowDirectionProperty = DependencyProperty.Register(
        nameof(FlowDirection), typeof(FlowDirection), typeof(DirectionalFontIconSource), new PropertyMetadata(FlowDirection.LeftToRight, (d, e) => ((DirectionalFontIconSource)d).UpdateGlyph()));

    /// <summary>
    /// Identifies the <see cref="Glyph"/> dependency property. Hosts resolve the glyph of a font icon source in two different ways: some copy
    /// <see cref="FontIconSource.Glyph"/> when they create their icon element, while WinUI's <c>IconSourceElement</c> binds by name to a dependency
    /// property registered on the source's own type and finds nothing on a derived type otherwise. A separately registered property with the same name
    /// and value satisfies both.
    /// </summary>
    public static new readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
        nameof(Glyph), typeof(string), typeof(DirectionalFontIconSource), new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets the glyph string currently displayed, computed from <see cref="Icon"/> and <see cref="FlowDirection"/>.
    /// </summary>
    public new string Glyph => (string)GetValue(GlyphProperty);

    /// <summary>
    /// Gets or sets the icon to display.
    /// </summary>
    public IIconGlyph? Icon
    {
        get => (IIconGlyph?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Gets or sets the flow direction that determines which glyph of the icon is displayed.
    /// </summary>
    public FlowDirection FlowDirection
    {
        get => (FlowDirection)GetValue(FlowDirectionProperty);
        set => SetValue(FlowDirectionProperty, value);
    }

    private void UpdateGlyph()
    {
        string glyph = Icon?.GetGlyph(FlowDirection is FlowDirection.RightToLeft) ?? string.Empty;
        SetValue(GlyphProperty, glyph);
        base.Glyph = glyph;
    }
}
