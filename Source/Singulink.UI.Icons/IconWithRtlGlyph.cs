namespace Singulink.UI.Icons;

/// <summary>
/// Provides glyph information about an icons that have unique versions for left-to-right and right-to-left flow directions.
/// </summary>
public class IconWithRtlGlyph : IIconGlyph
{
    /// <inheritdoc cref="IIconGlyph.CodePoint"/>
    public int CodePoint { get; }

    /// <inheritdoc cref="IIconGlyph.RtlCodePoint"/>
    public int RtlCodePoint { get; }

    /// <inheritdoc cref="IIconGlyph.Glyph"/>
    public string Glyph { get; }

    /// <inheritdoc cref="IIconGlyph.RtlGlyph"/>
    public string RtlGlyph { get; }

    /// <inheritdoc/>
    bool IIconGlyph.HasUniqueRtlGlyph => true;

    /// <summary>
    /// Initializes a new instance of the <see cref="IconWithRtlGlyph"/> class.
    /// </summary>
    public IconWithRtlGlyph(int codePoint, int rtlCodePoint)
    {
        if (codePoint == rtlCodePoint)
            throw new ArgumentException("Code points for LTR and RTL versions must be different.", nameof(rtlCodePoint));

        CodePoint = codePoint;
        RtlCodePoint = rtlCodePoint;
        Glyph = char.ConvertFromUtf32(codePoint);
        RtlGlyph = char.ConvertFromUtf32(rtlCodePoint);
    }

    /// <inheritdoc/>
    public override string ToString() => Glyph;
}
