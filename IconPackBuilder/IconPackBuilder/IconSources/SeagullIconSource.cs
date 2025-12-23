using System.Text;
using FluentIcons.Common;
using IconPackBuilder.Core;
using Singulink.IO;
using WaterTrans.GlyphLoader;
using Symbol = FluentIcons.Common.Symbol;

namespace IconPackBuilder.IconSources;

#pragma warning disable CA1822 // Mark members as static

public sealed class SeagullIconSource : IconsSource
{
    public static SeagullIconSource Instance => field ??= new();

#if HAS_UNO
    private const string Folder = "FluentIcons.Resources.Uno";
#else
    private const string Folder = "FluentIcons.WinUI";
#endif

    public override string Id => "FluentIcons.Seagull";

    public override string Name => "Fluent Icons (Seagull)";

    public override Version Version => typeof(Symbol).Assembly.GetName().Version ?? throw new InvalidOperationException("Unable to determine version.");

    public override IRelativeFilePath FontFile { get; } = FilePath.ParseRelative($"{Folder}/Assets/SeagullFluentIcons.otf", PathFormat.Universal);

    public override string FontFamilyName => "Seagull Fluent Icons";

    public override IReadOnlyList<string> Variants => [
        nameof(IconVariant.Regular),
        nameof(IconVariant.Filled),
        nameof(IconVariant.Color),
        nameof(IconVariant.Light),
    ];

    public override IEnumerable<IconGroupInfo> LoadIconGroups()
    {
        var fontFile = DirectoryPath.GetAppBase() + FontFile;

        Typeface typeface;

        using (var fontStream = fontFile.OpenStream(FileMode.Open, FileAccess.Read))
            typeface = new Typeface(fontStream);

        ImmutableArray<IconVariant> variants = [IconVariant.Regular, IconVariant.Filled, IconVariant.Color, IconVariant.Light];

        foreach (var symbol in Enum.GetValues<Symbol>())
        {
            string groupName = ToFriendlyName(symbol);
            IEnumerable<IconInfo> groupIcons = GetIconsForSymbol(symbol);

            yield return new IconGroupInfo(symbol.ToString(), groupName, groupIcons);
        }

        IEnumerable<IconInfo> GetIconsForSymbol(Symbol symbol)
        {
            foreach (var variant in variants)
            {
                int codePoint = GetCodePoint(symbol, variant, false);

                if (!typeface.CharacterToGlyphMap.TryGetValue(codePoint, out ushort glyphIndex))
                    continue;

                int rtlCodePoint = GetCodePoint(symbol, variant, true);
                bool hasUniqueRtlGlyph = glyphIndex != typeface.CharacterToGlyphMap[rtlCodePoint];

                yield return new IconInfo(variant.ToString(), codePoint, hasUniqueRtlGlyph ? rtlCodePoint : null);
            }
        }
    }

    private static int GetCodePoint(Symbol symbol, IconVariant iconVariant, bool isRtl)
    {
        int cp = 0xf0000 + (4 * (int)symbol) + (int)iconVariant;

        if (isRtl)
            cp += 0x10000;

        return cp;
    }

    private static string ToFriendlyName<T>(T identifier) where T : unmanaged, Enum
    {
        // Split PascalCase into words, e.g:
        // Xbox360Controller => Xbox 360 Controller
        // Cellular4G => Cellular 4G
        // Multiplier_5x => Multiplier .5x
        // Multiplier1_5x => Multiplier 1.5x

        string identifierString = identifier.ToString();
        var sb = new StringBuilder();

        for (int i = 0; i < identifierString.Length; i++)
        {
            char c = identifierString[i];
            char p = i > 0 ? identifierString[i - 1] : '\0';
            char n = i < identifierString.Length - 1 ? identifierString[i + 1] : '\0';

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly

            if ((p is not '\0' && char.IsAsciiLetterUpper(c) && (char.IsAsciiLetterLower(n) || (n is '\0' && char.IsAsciiLetterLower(p)))) ||
                (char.IsDigit(c) && !char.IsDigit(p)) ||
                (c is '_' && char.IsAsciiLetter(p)))
            {
                sb.Append(' ');
            }

#pragma warning restore SA1009

            sb.Append(c is '_' ? '.' : c);
        }

        // Fixups:

        sb.Replace("Usb", "USB");
        sb.Replace("Ios", "IOS");
        sb.Replace("Ui", "UI");
        sb.Replace("Tv", "TV");
        sb.Replace("Qr Code", "QR Code");
        sb.Replace("To Do", "ToDo");
        sb.Replace("Re Order", "ReOrder");
        sb.Replace("In Private", "InPrivate");

        return sb.ToString();
    }
}
