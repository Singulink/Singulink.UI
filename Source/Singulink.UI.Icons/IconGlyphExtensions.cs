namespace Singulink.UI.Icons;

/// <summary>
/// Provides extension methods for <see cref="IIconGlyph"/>.
/// </summary>
public static class IconGlyphExtensions
{
    /// <summary>
    /// Returns the glyph string for the given flow direction: the right-to-left glyph when <paramref name="isRightToLeft"/> is <see langword="true"/>
    /// and the icon has a unique RTL version, otherwise the regular glyph.
    /// </summary>
    public static string GetGlyph(this IIconGlyph icon, bool isRightToLeft) => isRightToLeft ? icon.RtlGlyph : icon.Glyph;

    /// <summary>
    /// Returns the Unicode code point for the given flow direction: the right-to-left code point when <paramref name="isRightToLeft"/> is
    /// <see langword="true"/> and the icon has a unique RTL version, otherwise the regular code point.
    /// </summary>
    public static int GetCodePoint(this IIconGlyph icon, bool isRightToLeft) => isRightToLeft ? icon.RtlCodePoint : icon.CodePoint;
}
