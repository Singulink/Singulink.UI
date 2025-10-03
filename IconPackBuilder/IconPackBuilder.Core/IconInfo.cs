namespace IconPackBuilder.Core;

public sealed class IconInfo
{
    public IconGroupInfo Group {
        get => field ?? throw new InvalidOperationException("Icon not assigned to a group.");
        internal set {
            if (field is not null)
                throw new InvalidOperationException("Icon already assigned to a group.");

            field = value;
        }
    }

    public string Variant { get; }

    public int CodePoint { get; }

    public int? RtlCodePoint { get; }

    public string Glyph { get; }

    public string? RtlGlyph { get; }

    public IconInfo(string variant, int codepoint, int? rtlCodePoint)
    {
        Variant = variant;
        CodePoint = codepoint;
        RtlCodePoint = codepoint == rtlCodePoint ? null : rtlCodePoint;
        Glyph = char.ConvertFromUtf32(codepoint);

        if (RtlCodePoint is not null)
            RtlGlyph = char.ConvertFromUtf32(RtlCodePoint.Value);
    }
}
