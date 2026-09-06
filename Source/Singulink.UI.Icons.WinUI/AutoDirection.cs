using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Singulink.UI.Icons.WinUI;

/// <summary>
/// Attached properties that make an existing <see cref="FontIcon"/> display an <see cref="IIconGlyph"/> with automatic right-to-left glyph
/// selection. Prefer <see cref="AutoDirectionFontIcon"/> where the element is under your control; this is intended for font icons inside
/// templates that cannot be replaced.
/// </summary>
public static class AutoDirection
{
    /// <summary>
    /// Identifies the <c>AutoDirection.Glyph</c> attached property.
    /// </summary>
    public static readonly DependencyProperty GlyphProperty = DependencyProperty.RegisterAttached(
        "Glyph", typeof(IIconGlyph), typeof(AutoDirection), new PropertyMetadata(null, OnIconChanged));

    private static readonly DependencyProperty IsHookedProperty = DependencyProperty.RegisterAttached(
        "IsHooked", typeof(bool), typeof(AutoDirection), new PropertyMetadata(false));

    /// <summary>
    /// Gets the icon displayed by the font icon. The accessors take a <see cref="DependencyObject"/> because the WinUI x:Bind code generator passes
    /// attached property targets as <see cref="DependencyObject"/>, so a narrower parameter type fails to compile for x:Bind usage (plain attribute
    /// values would work). Setting the property on anything other than a <see cref="FontIcon"/> throws.
    /// </summary>
    public static IIconGlyph? GetGlyph(DependencyObject element) => (IIconGlyph?)element.GetValue(GlyphProperty);

    /// <summary>
    /// Sets the icon displayed by the font icon.
    /// </summary>
    public static void SetGlyph(DependencyObject element, IIconGlyph? value) => element.SetValue(GlyphProperty, value);

    private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FontIcon fontIcon)
            throw new InvalidOperationException($"{nameof(AutoDirection)}.Icon can only be set on a {nameof(FontIcon)}.");

        if (!(bool)fontIcon.GetValue(IsHookedProperty))
        {
            fontIcon.SetValue(IsHookedProperty, true);
            fontIcon.RegisterPropertyChangedCallback(FrameworkElement.FlowDirectionProperty, (s, p) => UpdateGlyph((FontIcon)s));
            fontIcon.Loaded += (s, e) => UpdateGlyph((FontIcon)s);
        }

        UpdateGlyph(fontIcon);
    }

    private static void UpdateGlyph(FontIcon fontIcon)
    {
        var icon = GetGlyph(fontIcon);
        fontIcon.Glyph = icon is null ? string.Empty : fontIcon.FlowDirection is FlowDirection.RightToLeft ? icon.RtlGlyph : icon.Glyph;
    }
}
