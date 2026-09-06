using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Singulink.UI.Icons.Avalonia;

/// <summary>
/// Attached properties that make an existing <see cref="TextBlock"/> display an <see cref="IIconGlyph"/> with automatic right-to-left glyph
/// selection. Prefer <see cref="AutoDirectionFontIcon"/> where the control is under your control; this is intended for text blocks inside templates
/// that cannot be replaced.
/// </summary>
public static class AutoDirection
{
    /// <summary>
    /// Defines the <c>AutoDirection.Glyph</c> attached property.
    /// </summary>
    public static readonly AttachedProperty<IIconGlyph?> GlyphProperty = AvaloniaProperty.RegisterAttached<TextBlock, IIconGlyph?>("Glyph", typeof(AutoDirection));

    private static readonly AttachedProperty<bool> IsHookedProperty = AvaloniaProperty.RegisterAttached<TextBlock, bool>("IsHooked", typeof(AutoDirection));

    static AutoDirection()
    {
        GlyphProperty.Changed.AddClassHandler<TextBlock>(OnIconChanged);
    }

    /// <summary>
    /// Gets the icon displayed by the text block.
    /// </summary>
    public static IIconGlyph? GetGlyph(TextBlock textBlock) => textBlock.GetValue(GlyphProperty);

    /// <summary>
    /// Sets the icon displayed by the text block.
    /// </summary>
    public static void SetGlyph(TextBlock textBlock, IIconGlyph? value) => textBlock.SetValue(GlyphProperty, value);

    private static void OnIconChanged(TextBlock textBlock, AvaloniaPropertyChangedEventArgs e)
    {
        if (!textBlock.GetValue(IsHookedProperty))
        {
            textBlock.SetValue(IsHookedProperty, true);
            textBlock.PropertyChanged += OnTextBlockPropertyChanged;
        }

        UpdateText(textBlock);
    }

    private static void OnTextBlockPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Visual.FlowDirectionProperty)
            UpdateText((TextBlock)sender!);
    }

    private static void UpdateText(TextBlock textBlock)
    {
        textBlock.Text = GetGlyph(textBlock)?.GetGlyph(textBlock.FlowDirection is FlowDirection.RightToLeft) ?? string.Empty;
    }
}
