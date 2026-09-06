using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Singulink.UI.Icons.Avalonia;

/// <summary>
/// A <see cref="TextBlock"/> that displays an <see cref="IIconGlyph"/> from an icon font, automatically using its right-to-left glyph when the
/// control's effective <see cref="Visual.FlowDirection"/> is <see cref="FlowDirection.RightToLeft"/>. Set <see cref="TextBlock.FontFamily"/> to the
/// icon pack's font (typically through a style). The <see cref="TextBlock.Text"/> property is managed by the control and should not be set.
/// </summary>
public class AutoDirectionFontIcon : TextBlock
{
    /// <summary>
    /// Defines the <see cref="Glyph"/> property.
    /// </summary>
    public static readonly StyledProperty<IIconGlyph?> GlyphProperty = AvaloniaProperty.Register<AutoDirectionFontIcon, IIconGlyph?>(nameof(Glyph));

    /// <summary>
    /// Gets or sets the icon to display.
    /// </summary>
    public IIconGlyph? Glyph
    {
        get => GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == GlyphProperty || change.Property == FlowDirectionProperty)
            Text = Glyph?.GetGlyph(FlowDirection is FlowDirection.RightToLeft) ?? string.Empty;
    }
}
