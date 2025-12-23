namespace Singulink.UI.Icons;

/// <summary>
/// Provides glyph information about icons that use the same glyph for both left-to-right and right-to-left flow directions.
/// </summary>
public class IconGlyph : IIconGlyph
{
    /// <inheritdoc cref="IIconGlyph.CodePoint"/>
    public int CodePoint { get; }

    /// <inheritdoc cref="IIconGlyph.Glyph"/>
    public string Glyph { get; }

    /// <inheritdoc/>
    string IIconGlyph.RtlGlyph => Glyph;

    /// <inheritdoc/>
    int IIconGlyph.RtlCodePoint => CodePoint;

    /// <inheritdoc/>
    bool IIconGlyph.HasUniqueRtlGlyph => false;

    /// <summary>
    /// Initializes a new instance of the <see cref="IconGlyph"/> class.
    /// </summary>
    public IconGlyph(int codePoint)
    {
        CodePoint = codePoint;
        Glyph = char.ConvertFromUtf32(codePoint);
    }

    /// <inheritdoc/>
    public override string ToString() => Glyph;
}
