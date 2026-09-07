using System.Text.Json;
using Singulink.IO;

namespace IconPackBuilder.Core.IconSources;

/// <summary>
/// Icons source backed by the Seagull Fluent Icons font and the metadata asset generated from it by the <c>IconPackBuilder.SeagullAssetsGenerator</c>
/// tool. Both assets live in the app's <c>Assets/Seagull</c> folder.
/// </summary>
public sealed class SeagullIconsSource : IconsSource
{
    private const string AssetsFolder = "Assets/Seagull";

    private static readonly Lazy<SeagullIconsSource> _instance = new(LoadFromAppBase);

    public static SeagullIconsSource Instance => _instance.Value;

    private readonly SeagullIconsData _data;

    private SeagullIconsSource(Stream dataStream, string dataDescription)
    {
        _data = JsonSerializer.Deserialize(dataStream, SeagullIconsJsonContext.Default.SeagullIconsData) ??
            throw new InvalidDataException($"Icon data '{dataDescription}' is empty.");

        Version = Version.Parse(_data.Version);
        Variants = [.. _data.Variants];
    }

    private static SeagullIconsSource LoadFromAppBase()
    {
        var dataFile = DirectoryPath.GetAppBase() + DataFilePath;

        using var stream = dataFile.OpenStream(FileMode.Open, FileAccess.Read, FileShare.Read);
        return new SeagullIconsSource(stream, dataFile.PathDisplay);
    }

    /// <summary>
    /// Creates a source from the metadata JSON read from the given stream, for hosts that cannot read the app directory with <see cref="File"/>
    /// APIs (WebAssembly). The font file is still expected at <see cref="FontFile"/> relative to the app package.
    /// </summary>
    public static SeagullIconsSource FromStream(Stream dataStream) => new(dataStream, "stream");

    public override string Id => "FluentIcons.Seagull";

    public override string Name => "Fluent Icons (Seagull)";

    public override Version Version { get; }

    public override IRelativeFilePath FontFile { get; } = FilePath.ParseRelative($"{AssetsFolder}/SeagullFluentIcons.otf", PathFormat.Universal);

    /// <summary>
    /// Gets the metadata file path relative to the app directory.
    /// </summary>
    public static IRelativeFilePath DataFilePath { get; } = FilePath.ParseRelative($"{AssetsFolder}/SeagullFluentIcons.json", PathFormat.Universal);

    public override string FontFamilyName => _data.FontFamilyName;

    public override IReadOnlyList<string> Variants { get; }

    /// <summary>
    /// Gets information about the upstream packages and repository the assets were generated from.
    /// </summary>
    public SeagullSourceInfo Source => _data.Source;

    public override IEnumerable<IconGroupInfo> LoadIconGroups()
    {
        foreach (var icon in _data.Icons)
        {
            var icons = _data.Variants
                .Where(icon.Variants.ContainsKey)
                .Select(v => new IconInfo(v, icon.Variants[v].CodePoint, icon.Variants[v].RtlCodePoint));

            yield return new IconGroupInfo(icon.Id, icon.Name, icons, icon.Description, icon.Metaphors);
        }
    }
}
