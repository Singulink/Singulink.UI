using IconPackBuilder.Core.Services;
using Singulink.IO;
using WpfMedia = System.Windows.Media;

namespace IconPackBuilder.Services;

public class WpfFontSubsetter : IFontSubsetter
{
    public static WpfFontSubsetter Instance { get; } = new();

    public async Task SaveAsync(IAbsoluteFilePath sourcePath, IAbsoluteFilePath destinationPath, IEnumerable<int> codePoints)
    {
        string iconUri = "file:///" + sourcePath.PathDisplay;
        var typeface = new WpfMedia.GlyphTypeface(new Uri(iconUri, UriKind.Absolute));

        var glyphIndices = new HashSet<ushort>();

        foreach (int codePoint in codePoints)
        {
            ushort glyph = typeface.CharacterToGlyphMap[codePoint];
            glyphIndices.Add(glyph);
        }

        byte[] subsetBytes = typeface.ComputeSubset(glyphIndices);

        await using var destinationStream = destinationPath.OpenAsyncStream(FileMode.Create);

        await destinationStream.WriteAsync(subsetBytes);
        await destinationStream.FlushAsync();
    }
}
