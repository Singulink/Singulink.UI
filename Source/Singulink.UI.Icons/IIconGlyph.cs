namespace Singulink.UI.Icons;

/// <summary>
/// Provides glyph information about an icon.
/// </summary>
public interface IIconGlyph
{
    /// <summary>
    /// Gets the Unicode code point of the icon.
    /// </summary>
    public int CodePoint { get; }

    /// <summary>
    /// Gets the Unicode code point of the right-to-left version of the icon.
    /// </summary>
    public int RtlCodePoint { get; }

    /// <summary>
    /// Gets the glyph string representation of the icon.
    /// </summary>
    public string Glyph { get; }

    /// <summary>
    /// Gets the glyph string representation of the right-to-left version of the icon.
    /// </summary>
    public string RtlGlyph { get; }

    /// <summary>
    /// Gets a value indicating whether the icon has a unique version for right-to-left flow directions.
    /// </summary>
    public bool HasUniqueRtlGlyph { get; }

    /// <summary>
    /// Returns the glyph string representation of the icon.
    /// </summary>
    public string ToString();
}
