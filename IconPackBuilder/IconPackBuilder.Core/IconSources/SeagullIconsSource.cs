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

    private static readonly Lazy<SeagullIconsSource> _instance = new(() => new());

    public static SeagullIconsSource Instance => _instance.Value;

    private readonly SeagullIconsData _data;

    private SeagullIconsSource()
    {
        var dataFile = DirectoryPath.GetAppBase() + DataFile;

        using var stream = dataFile.OpenStream(FileMode.Open, FileAccess.Read, FileShare.Read);
        _data = JsonSerializer.Deserialize(stream, SeagullIconsJsonContext.Default.SeagullIconsData) ??
            throw new InvalidDataException($"Icon data file '{dataFile.PathDisplay}' is empty.");

        Version = Version.Parse(_data.Version);
        Variants = [.. _data.Variants];
    }

    public override string Id => "FluentIcons.Seagull";

    public override string Name => "Fluent Icons (Seagull)";

    public override Version Version { get; }

    public override IRelativeFilePath FontFile { get; } = FilePath.ParseRelative($"{AssetsFolder}/SeagullFluentIcons.otf", PathFormat.Universal);

    /// <summary>
    /// Gets the metadata file path relative to the app directory.
    /// </summary>
    public IRelativeFilePath DataFile { get; } = FilePath.ParseRelative($"{AssetsFolder}/SeagullFluentIcons.json", PathFormat.Universal);

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
