using PrefixClassName.MsTest;
using Shouldly;

namespace Singulink.UI.Icons.Tests;

[PrefixTestClass]
public class IconGlyphTests
{
    // Seagull code points live in a supplementary plane, so glyph strings are surrogate pairs.
    private const int Save = 0xF03E4;
    private const int ArrowPrevious = 0xF0050;
    private const int ArrowPreviousRtl = 0x100050;

    [TestMethod]
    public void IconGlyph_SameGlyphForBothDirections()
    {
        IIconGlyph icon = new IconGlyph(Save);

        icon.CodePoint.ShouldBe(Save);
        icon.RtlCodePoint.ShouldBe(Save);
        icon.Glyph.ShouldBe(char.ConvertFromUtf32(Save));
        icon.RtlGlyph.ShouldBe(icon.Glyph);
        icon.HasUniqueRtlGlyph.ShouldBeFalse();
        icon.ToString().ShouldBe(icon.Glyph);
        icon.Glyph.Length.ShouldBe(2);
    }

    [TestMethod]
    public void IconWithRtlGlyph_DistinctGlyphs()
    {
        IIconGlyph icon = new IconWithRtlGlyph(ArrowPrevious, ArrowPreviousRtl);

        icon.CodePoint.ShouldBe(ArrowPrevious);
        icon.RtlCodePoint.ShouldBe(ArrowPreviousRtl);
        icon.Glyph.ShouldBe(char.ConvertFromUtf32(ArrowPrevious));
        icon.RtlGlyph.ShouldBe(char.ConvertFromUtf32(ArrowPreviousRtl));
        icon.HasUniqueRtlGlyph.ShouldBeTrue();
        icon.ToString().ShouldBe(icon.Glyph);
    }

    [TestMethod]
    public void IconWithRtlGlyph_SameCodePoints_Throws()
    {
        Should.Throw<ArgumentException>(() => new IconWithRtlGlyph(Save, Save));
    }

    [TestMethod]
    public void GetGlyph_PicksDirection()
    {
        var directional = new IconWithRtlGlyph(ArrowPrevious, ArrowPreviousRtl);
        var plain = new IconGlyph(Save);

        directional.GetGlyph(isRightToLeft: false).ShouldBe(directional.Glyph);
        directional.GetGlyph(isRightToLeft: true).ShouldBe(directional.RtlGlyph);
        directional.GetCodePoint(isRightToLeft: false).ShouldBe(ArrowPrevious);
        directional.GetCodePoint(isRightToLeft: true).ShouldBe(ArrowPreviousRtl);

        plain.GetGlyph(isRightToLeft: true).ShouldBe(plain.Glyph);
        plain.GetCodePoint(isRightToLeft: true).ShouldBe(Save);
    }
}
