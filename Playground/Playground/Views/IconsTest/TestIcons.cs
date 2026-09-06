using Singulink.UI.Icons;

namespace Playground.Views.IconsTest;

/// <summary>
/// Icons from the Seagull font shipped in Assets/Fonts, shaped like the output of the icon pack builder's C# exporter.
/// </summary>
public static class TestIcons
{
    public static IconWithRtlGlyph ArrowPrevious { get; } = new(0xF0050, 0x100050);

    public static IconGlyph Save { get; } = new(0xF03E4);
}
