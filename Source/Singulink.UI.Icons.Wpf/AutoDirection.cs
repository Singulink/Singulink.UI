using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Singulink.UI.Icons.Wpf;

/// <summary>
/// Attached properties that make an existing <see cref="TextBlock"/> display an <see cref="IIconGlyph"/> with automatic right-to-left glyph
/// selection. Prefer <see cref="AutoDirectionFontIcon"/> where the element is under your control; this is intended for text blocks inside templates
/// that cannot be replaced.
/// </summary>
public static class AutoDirection
{
    /// <summary>
    /// Identifies the <c>AutoDirection.Glyph</c> attached property.
    /// </summary>
    public static readonly DependencyProperty GlyphProperty = DependencyProperty.RegisterAttached(
        "Glyph", typeof(IIconGlyph), typeof(AutoDirection), new PropertyMetadata(null, OnIconChanged));

    // Mirrors the element's effective flow direction through a binding so inherited changes are observed without leaking descriptors.
    private static readonly DependencyProperty FlowDirectionMirrorProperty = DependencyProperty.RegisterAttached(
        "FlowDirectionMirror", typeof(FlowDirection), typeof(AutoDirection), new PropertyMetadata(FlowDirection.LeftToRight, (d, e) => UpdateText((TextBlock)d)));

    /// <summary>
    /// Gets the icon displayed by the text block.
    /// </summary>
    public static IIconGlyph? GetGlyph(TextBlock textBlock) => (IIconGlyph?)textBlock.GetValue(GlyphProperty);

    /// <summary>
    /// Sets the icon displayed by the text block.
    /// </summary>
    public static void SetGlyph(TextBlock textBlock, IIconGlyph? value) => textBlock.SetValue(GlyphProperty, value);

    private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock textBlock)
            throw new InvalidOperationException($"{nameof(AutoDirection)}.Icon can only be set on a {nameof(TextBlock)}.");

        if (BindingOperations.GetBinding(textBlock, FlowDirectionMirrorProperty) is null)
        {
            BindingOperations.SetBinding(textBlock, FlowDirectionMirrorProperty, new Binding {
                Source = textBlock,
                Path = new PropertyPath(FrameworkElement.FlowDirectionProperty),
                Mode = BindingMode.OneWay,
            });
        }

        UpdateText(textBlock);
    }

    private static void UpdateText(TextBlock textBlock)
    {
        textBlock.Text = GetGlyph(textBlock)?.GetGlyph(textBlock.FlowDirection is FlowDirection.RightToLeft) ?? string.Empty;
    }
}
