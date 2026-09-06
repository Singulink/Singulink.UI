using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentIcons.Common;
using IconPackBuilder.Core.IconSources;
using WaterTrans.GlyphLoader;

namespace IconPackBuilder.SeagullAssetsGenerator;

/// <summary>
/// Generates the Seagull icon assets for the builder app. See the project file for usage.
/// </summary>
internal static partial class Program
{
    private const string FluentIconsRepository = "davidxuang/FluentIcons";
    private const string UpstreamRepository = "https://github.com/microsoft/fluentui-system-icons.git";
    private const string FontFileName = "SeagullFluentIcons.otf";
    private const string DataFileName = "SeagullFluentIcons.json";
    private const string FontFamilyName = "Seagull Fluent Icons";

    private static readonly HttpClient Http = new() { DefaultRequestHeaders = { { "User-Agent", "IconPackBuilder.SeagullAssetsGenerator" } } };

    private static async Task<int> Main(string[] args)
    {
        try
        {
            await RunAsync(args);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            return 1;
        }
    }

    private static async Task RunAsync(string[] args)
    {
        string commonVersion = GetAssemblyMetadata("FluentIconsCommonVersion");
        string winUIVersion = GetAssemblyMetadata("FluentIconsWinUIVersion");
        string tag = GetAssemblyMetadata("FluentIconsTag");

        string outputDir = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.GetFullPath(Path.Combine(GetSourceDirectory(), "..", "IconPackBuilder", "Assets", "Seagull"));
        Directory.CreateDirectory(outputDir);

        Console.WriteLine($"FluentIcons.Common {commonVersion}, FluentIcons.WinUI {winUIVersion}, tag {tag}");
        Console.WriteLine($"Output: {outputDir}");

        // Font

        Console.WriteLine();
        Console.WriteLine($"Downloading FluentIcons.WinUI {winUIVersion}...");

        byte[] fontBytes = await DownloadFontAsync(winUIVersion);
        await File.WriteAllBytesAsync(Path.Combine(outputDir, FontFileName), fontBytes);
        Console.WriteLine($"Wrote {FontFileName} ({fontBytes.Length:N0} bytes)");

        var typeface = new Typeface(new MemoryStream(fontBytes));

        // Upstream metadata

        Console.WriteLine();
        Console.WriteLine($"Resolving upstream commit for tag {tag}...");

        string upstreamCommit = await GetUpstreamCommitAsync(tag);
        var redirects = ParseRedirects(await Http.GetStringAsync($"https://raw.githubusercontent.com/{FluentIconsRepository}/{tag}/seagull-icons/transform.toml"));
        var reverseRedirects = redirects.GroupBy(r => r.Value).ToDictionary(g => g.Key, g => g.First().Key, StringComparer.Ordinal);
        Console.WriteLine($"Upstream commit {upstreamCommit}, {redirects.Count} symbol redirects");

        string upstreamDir = await FetchUpstreamMetadataAsync(upstreamCommit);
        var upstream = LoadUpstreamMetadata(upstreamDir);
        Console.WriteLine($"Loaded metadata for {upstream.Count} upstream icons");

        // Symbols

        Console.WriteLine();

        var variants = Enum.GetValues<IconVariant>().OrderBy(v => (int)v).ToArray();
        var icons = new List<SeagullIconData>();
        int matched = 0, inherited = 0, unmatched = 0;
        var unmatchedNames = new List<string>();

        foreach (var symbol in Enum.GetValues<Symbol>().OrderBy(s => s.ToString(), StringComparer.Ordinal))
        {
            var glyphs = new SortedDictionary<string, SeagullGlyphData>(StringComparer.Ordinal);

            foreach (var variant in variants)
            {
                int codePoint = GetCodePoint(symbol, variant, false);

                if (!typeface.CharacterToGlyphMap.TryGetValue(codePoint, out ushort glyphIndex))
                    continue;

                int rtlCodePoint = GetCodePoint(symbol, variant, true);
                bool hasUniqueRtlGlyph = typeface.CharacterToGlyphMap.TryGetValue(rtlCodePoint, out ushort rtlGlyphIndex) && rtlGlyphIndex != glyphIndex;

                glyphs[variant.ToString()] = new SeagullGlyphData { CodePoint = codePoint, RtlCodePoint = hasUniqueRtlGlyph ? rtlCodePoint : null };
            }

            if (glyphs.Count is 0)
            {
                Console.WriteLine($"WARNING: symbol '{symbol}' has no glyphs in the font and was skipped.");
                continue;
            }

            string id = symbol.ToString();
            string friendlyName = ToFriendlyName(id);
            string normalized = Normalize(id);

            // Seagull's transform.toml [redirect] table maps a base name to the icon that stands in for it, in both directions of interest here: a
            // symbol that only exists as a Seagull alias of an upstream icon (e.g. "lock" for "lock_closed") takes that icon's metadata, and an
            // upstream icon that Seagull renamed (e.g. "home" to "home_empty") passes its metadata on to the new name. A direct name match always
            // wins because it represents the same concept even when Seagull redraws the icon.

            UpstreamIcon? match = upstream.GetValueOrDefault(normalized);
            bool isDirectMatch = match is not null;

            if (match is null && redirects.TryGetValue(normalized, out string? redirectTarget))
                match = upstream.GetValueOrDefault(redirectTarget);

            if (match is null && reverseRedirects.TryGetValue(normalized, out string? redirectSource))
                match = upstream.GetValueOrDefault(redirectSource);

            IReadOnlyList<string> metaphors = [];

            if (match is not null)
            {
                matched++;
            }
            else
            {
                // Seagull composes many icons that upstream does not ship (e.g. "<base> Sparkle"). Inherit the metaphors of the longest matching
                // leading part of the name so the icon is still found by concept, but do not claim its description.

                string[] words = friendlyName.Split(' ');

                for (int count = words.Length - 1; count > 0 && metaphors.Count is 0; count--)
                {
                    if (upstream.TryGetValue(Normalize(string.Join(string.Empty, words, 0, count)), out var baseIcon))
                        metaphors = baseIcon.Metaphors;
                }

                if (metaphors.Count > 0)
                    inherited++;
                else
                    unmatched++;

                unmatchedNames.Add(id);
            }

            icons.Add(new SeagullIconData {
                Id = id,
                Value = (int)symbol,
                Name = isDirectMatch && !match!.Name.Contains('_') ? match.Name : friendlyName,
                Description = match?.Description,
                Metaphors = match?.Metaphors ?? metaphors,
                DirectionType = match?.DirectionType,
                UpstreamName = match?.Name,
                Variants = glyphs,
            });
        }

        // Data file

        var data = new SeagullIconsData {
            Version = commonVersion,
            FontFamilyName = FontFamilyName,
            Source = new SeagullSourceInfo {
                FluentIconsCommonVersion = commonVersion,
                FluentIconsWinUIVersion = winUIVersion,
                FluentIconsTag = tag,
                UpstreamRepository = UpstreamRepository,
                UpstreamCommit = upstreamCommit,
                GeneratedUtc = DateTime.UtcNow,
            },
            Variants = [.. variants.Select(v => v.ToString())],
            Icons = icons,
        };

        string dataPath = Path.Combine(outputDir, DataFileName);

        await using (var stream = File.Create(dataPath))
            await JsonSerializer.SerializeAsync(stream, data, SeagullIconsJsonContext.Default.SeagullIconsData);

        Console.WriteLine($"Wrote {DataFileName} ({new FileInfo(dataPath).Length:N0} bytes)");
        Console.WriteLine();
        Console.WriteLine($"{icons.Count} symbols: {matched} matched upstream metadata, {inherited} inherited metaphors from a base icon, {unmatched} without metadata.");
        Console.WriteLine();
        Console.WriteLine("Symbols without a direct upstream match:");
        Console.WriteLine(string.Join(", ", unmatchedNames));
    }

    private static async Task<byte[]> DownloadFontAsync(string winUIVersion)
    {
        string url = $"https://api.nuget.org/v3-flatcontainer/fluenticons.winui/{winUIVersion.ToLowerInvariant()}/fluenticons.winui.{winUIVersion.ToLowerInvariant()}.nupkg";
        byte[] package = await Http.GetByteArrayAsync(url);

        using var zip = new ZipArchive(new MemoryStream(package), ZipArchiveMode.Read);
        var entry = zip.Entries.FirstOrDefault(e => e.Name.Equals(FontFileName, StringComparison.OrdinalIgnoreCase)) ??
            throw new InvalidOperationException($"'{FontFileName}' not found in the FluentIcons.WinUI package.");

        await using var entryStream = entry.Open();
        var ms = new MemoryStream();
        await entryStream.CopyToAsync(ms);
        return ms.ToArray();
    }

    private static async Task<string> GetUpstreamCommitAsync(string tag)
    {
        using var doc = JsonDocument.Parse(await Http.GetStringAsync($"https://api.github.com/repos/{FluentIconsRepository}/contents/seagull-icons/upstream?ref={tag}"));
        var root = doc.RootElement;

        if (root.GetProperty("type").GetString() is not "submodule")
            throw new InvalidOperationException("'seagull-icons/upstream' in the FluentIcons repository is not a submodule.");

        return root.GetProperty("sha").GetString()!;
    }

    /// <summary>
    /// Parses the simple entries of the [redirect] section of Seagull's transform.toml, which map a base icon name to the icon that stands in for
    /// it. Keys and values are returned in normalized form.
    /// </summary>
    private static Dictionary<string, string> ParseRedirects(string toml)
    {
        var redirects = new Dictionary<string, string>(StringComparer.Ordinal);
        bool inSection = false;

        foreach (string rawLine in toml.Split('\n'))
        {
            string line = rawLine.Trim();

            if (line.StartsWith('['))
            {
                inSection = line is "[redirect]";
                continue;
            }

            if (!inSection || line.Length is 0 || line.StartsWith('#'))
                continue;

            var simple = SimpleRedirectRegex().Match(line);

            if (simple.Success)
                redirects[Normalize(simple.Groups[1].Value)] = Normalize(simple.Groups[2].Value);
            else
                Console.WriteLine($"NOTE: redirect entry ignored (only simple name redirects are used): {line}");
        }

        return redirects;
    }

    /// <summary>
    /// Sparse-clones just the metadata files from the upstream repository at the given commit into a cache directory, reusing a previous clone of
    /// the same commit if present.
    /// </summary>
    private static async Task<string> FetchUpstreamMetadataAsync(string commit)
    {
        string dir = Path.Combine(Path.GetTempPath(), "IconPackBuilder.SeagullAssetsGenerator", "fluentui-system-icons");
        string markerFile = Path.Combine(dir, ".metadata-commit");

        if (File.Exists(markerFile) && File.ReadAllText(markerFile).Trim() == commit)
        {
            Console.WriteLine($"Using cached upstream metadata in {dir}");
            return dir;
        }

        Console.WriteLine($"Fetching upstream metadata into {dir} (this can take a minute)...");

        if (Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);

        Directory.CreateDirectory(dir);

        await GitAsync(dir, "init", "-q");
        await GitAsync(dir, "remote", "add", "origin", UpstreamRepository);
        await GitAsync(dir, "sparse-checkout", "init", "--no-cone");
        await GitAsync(dir, "sparse-checkout", "set", "assets/*/metadata.json");
        await GitAsync(dir, "fetch", "-q", "--depth", "1", "--filter=blob:none", "origin", commit);
        await GitAsync(dir, "checkout", "-q", "FETCH_HEAD");

        File.WriteAllText(markerFile, commit);
        return dir;
    }

    private static async Task GitAsync(string workingDir, params string[] arguments)
    {
        var psi = new ProcessStartInfo("git") {
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (string arg in arguments)
            psi.ArgumentList.Add(arg);

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start git.");
        var stdErr = process.StandardError.ReadToEndAsync();
        var stdOut = process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"git {string.Join(' ', arguments)} failed (exit code {process.ExitCode}):\n{await stdErr}{await stdOut}");
    }

    private static Dictionary<string, UpstreamIcon> LoadUpstreamMetadata(string upstreamDir)
    {
        var icons = new Dictionary<string, UpstreamIcon>(StringComparer.Ordinal);
        string assetsDir = Path.Combine(upstreamDir, "assets");

        foreach (string metadataPath in Directory.EnumerateFiles(assetsDir, "metadata.json", SearchOption.AllDirectories))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(metadataPath));
            var root = doc.RootElement;

            string folderName = Path.GetFileName(Path.GetDirectoryName(metadataPath)!);
            string name = root.TryGetProperty("name", out var nameProp) && nameProp.ValueKind is JsonValueKind.String ? nameProp.GetString()! : folderName;

            IReadOnlyList<string> metaphors = [];

            if (root.TryGetProperty("metaphor", out var metaphorProp) && metaphorProp.ValueKind is JsonValueKind.Array)
            {
                metaphors = [.. metaphorProp.EnumerateArray()
                    .Where(m => m.ValueKind is JsonValueKind.String)
                    .Select(m => m.GetString()!.Trim())
                    .Where(m => m.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)];
            }

            var icon = new UpstreamIcon(name, GetOptionalString(root, "description"), metaphors, GetOptionalString(root, "directionType"));

            icons.TryAdd(Normalize(folderName), icon);
            icons.TryAdd(Normalize(name), icon);
        }

        return icons;

        static string? GetOptionalString(JsonElement element, string property)
        {
            if (!element.TryGetProperty(property, out var prop) || prop.ValueKind is not JsonValueKind.String)
                return null;

            string value = prop.GetString()!.Trim();
            return value.Length > 0 ? value : null;
        }
    }

    private static int GetCodePoint(Symbol symbol, IconVariant iconVariant, bool isRtl)
    {
        int cp = 0xf0000 + (4 * (int)symbol) + (int)iconVariant;

        if (isRtl)
            cp += 0x10000;

        return cp;
    }

    /// <summary>
    /// Lower-cases and strips everything except ASCII letters and digits so that "Arrow Repeat All", "arrow_repeat_all" and "ArrowRepeatAll"
    /// compare equal.
    /// </summary>
    private static string Normalize(string value)
    {
        var sb = new StringBuilder(value.Length);

        foreach (char c in value)
        {
            if (char.IsAsciiLetterOrDigit(c))
                sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }

    private static string ToFriendlyName(string identifier)
    {
        // Split PascalCase into words, e.g:
        // Xbox360Controller => Xbox 360 Controller
        // Cellular4G => Cellular 4G
        // Multiplier_5x => Multiplier .5x
        // Multiplier1_5x => Multiplier 1.5x

        var sb = new StringBuilder();

        for (int i = 0; i < identifier.Length; i++)
        {
            char c = identifier[i];
            char p = i > 0 ? identifier[i - 1] : '\0';
            char n = i < identifier.Length - 1 ? identifier[i + 1] : '\0';

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
        sb.Replace("Ios", "iOS");
        sb.Replace("Ui", "UI");
        sb.Replace("Tv", "TV");
        sb.Replace("Qr Code", "QR Code");
        sb.Replace("To Do", "ToDo");
        sb.Replace("Re Order", "ReOrder");
        sb.Replace("In Private", "InPrivate");

        return sb.ToString();
    }

    private static string GetAssemblyMetadata(string key)
    {
        return typeof(Program).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == key)?.Value ??
            throw new InvalidOperationException($"Assembly metadata '{key}' not found. Ensure it is set in the project file.");
    }

    private static string GetSourceDirectory([CallerFilePath] string sourcePath = "") => Path.GetDirectoryName(sourcePath)!;

    [GeneratedRegex("""^([a-z0-9_]+)\s*=\s*"([a-z0-9_]+)"$""")]
    private static partial Regex SimpleRedirectRegex();

    private sealed record UpstreamIcon(string Name, string? Description, IReadOnlyList<string> Metaphors, string? DirectionType);
}
