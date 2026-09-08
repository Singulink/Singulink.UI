using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Singulink.UI.Icons.WinUI;

/// <summary>
/// Attached properties that give existing elements automatic right-to-left glyph selection: <c>AutoDirection.Glyph</c> makes a <see cref="FontIcon"/>
/// display an <see cref="IIconGlyph"/>, and <c>AutoDirection.IconSource</c> gives a control with an <c>IconSource</c> property (<see cref="TabViewItem"/>,
/// <see cref="InfoBar"/> or <see cref="IconSourceElement"/>) a <see cref="DirectionalFontIconSource"/> whose direction follows the control. Prefer
/// <see cref="AutoDirectionFontIcon"/> where the element is under your control; <c>Glyph</c> is intended for font icons inside templates that cannot be
/// replaced.
/// </summary>
public static class AutoDirection
{
    /// <summary>
    /// Identifies the <c>AutoDirection.Glyph</c> attached property.
    /// </summary>
    public static readonly DependencyProperty GlyphProperty = DependencyProperty.RegisterAttached(
        "Glyph", typeof(IIconGlyph), typeof(AutoDirection), new PropertyMetadata(null, OnIconChanged));

    /// <summary>
    /// Identifies the <c>AutoDirection.IconSource</c> attached property.
    /// </summary>
    public static readonly DependencyProperty IconSourceProperty = DependencyProperty.RegisterAttached(
        "IconSource", typeof(DirectionalFontIconSource), typeof(AutoDirection), new PropertyMetadata(null, OnIconSourceChanged));

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

    /// <summary>
    /// Gets the icon source displayed by the control. Setting the property on anything other than a <see cref="TabViewItem"/>, <see cref="InfoBar"/> or
    /// <see cref="IconSourceElement"/> throws.
    /// </summary>
    public static DirectionalFontIconSource? GetIconSource(DependencyObject element) => (DirectionalFontIconSource?)element.GetValue(IconSourceProperty);

    /// <summary>
    /// Sets the icon source displayed by the control. The source's <see cref="DirectionalFontIconSource.FlowDirection"/> is kept in sync with the
    /// control's effective flow direction, and the control is given the source again after a change so that hosts which copy the glyph out of the
    /// source once (<see cref="TabViewItem"/> and <see cref="InfoBar"/>) rebuild their icon. Use a separate source instance for each control.
    /// </summary>
    public static void SetIconSource(DependencyObject element, DirectionalFontIconSource? value) => element.SetValue(IconSourceProperty, value);

    private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FontIcon fontIcon)
            throw new InvalidOperationException($"{nameof(AutoDirection)}.{nameof(GlyphProperty)} can only be set on a {nameof(FontIcon)}.");

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

    private static void OnIconSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var host = (FrameworkElement)d;
        var hostProperty = GetHostIconSourceProperty(host);

        if (!(bool)host.GetValue(IsHookedProperty))
        {
            host.SetValue(IsHookedProperty, true);
            host.RegisterPropertyChangedCallback(FrameworkElement.FlowDirectionProperty, (s, p) => UpdateIconSource((FrameworkElement)s, hostProperty));
            host.Loaded += (s, e) => UpdateIconSource((FrameworkElement)s, hostProperty);
        }

        if (e.NewValue is null)
        {
            // Only clear what this property put there.
            if (ReferenceEquals(host.GetValue(hostProperty), e.OldValue))
                host.ClearValue(hostProperty);

            return;
        }

        UpdateIconSource(host, hostProperty);
    }

    private static void UpdateIconSource(FrameworkElement host, DependencyProperty hostProperty)
    {
        if (GetIconSource(host) is not DirectionalFontIconSource source)
            return;

        bool directionChanged = source.FlowDirection != host.FlowDirection;
        source.FlowDirection = host.FlowDirection;

        if (!ReferenceEquals(host.GetValue(hostProperty), source))
        {
            host.SetValue(hostProperty, source);
        }
        else if (directionChanged)
        {
            // Hosts that copy the glyph out of the source rebuild their icon only when the property is assigned, so hand them the source again.
            host.ClearValue(hostProperty);
            host.SetValue(hostProperty, source);
        }
    }

    private static DependencyProperty GetHostIconSourceProperty(DependencyObject host) => host switch {
        TabViewItem => TabViewItem.IconSourceProperty,
        InfoBar => InfoBar.IconSourceProperty,
        IconSourceElement => IconSourceElement.IconSourceProperty,
        _ => throw new InvalidOperationException(
            $"{nameof(AutoDirection)}.{nameof(IconSourceProperty)} can only be set on a {nameof(TabViewItem)}, {nameof(InfoBar)} or {nameof(IconSourceElement)}."),
    };
}
