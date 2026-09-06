using System.Text.Json;
using IconPackBuilder.Core;
using IconPackBuilder.Core.Services;
using IconPackBuilder.Data;
using Singulink.IO;

namespace IconPackBuilder.Tests;

/// <summary>
/// A three-icon source with predictable names and keywords, used by the view model tests.
/// </summary>
public sealed class FakeIconsSource : IconsSource
{
    public const string SourceId = "Fake";

    public static FakeIconsSource Instance { get; } = new();

    public override string Id => SourceId;

    public override string Name => "Fake";

    public override Version Version => new(1, 0);

    public override IRelativeFilePath FontFile => FilePath.ParseRelative("Assets/fake.otf", PathFormat.Universal);

    public override string FontFamilyName => "Fake";

    public override IReadOnlyList<string> Variants => ["Regular", "Filled"];

    public override IEnumerable<IconGroupInfo> LoadIconGroups()
    {
        int cp = 0xF0000;

        yield return Group("Add", "Add", "Adds an item.", ["plus", "create", "new"], cp++, cp++, rtl: false);
        yield return Group("Alert", "Alert", "Notifies the user.", ["bell", "notification"], cp++, cp++, rtl: false);
        yield return Group("ArrowLeft", "Arrow Left", "Points left.", ["direction", "back", "previous"], cp++, cp++, rtl: true);
        yield return Group("Save", "Save", null, ["floppy disk", "store"], cp++, cp++, rtl: false);
    }

    private static IconGroupInfo Group(string id, string name, string? description, string[] keywords, int regular, int filled, bool rtl)
        => new(id, name, [new IconInfo("Regular", regular, rtl ? regular + 0x10000 : null), new IconInfo("Filled", filled, rtl ? filled + 0x10000 : null)], description, keywords);
}

public sealed class FakeWindow : IWindow
{
    public bool IsClosed { get; private set; }

    public void Close() => IsClosed = true;
}

public sealed class FakeFontSubsetter : IFontSubsetter
{
    public List<(IAbsoluteFilePath Source, IAbsoluteFilePath Destination, int[] CodePoints)> Calls { get; } = [];

    public Task SaveAsync(IAbsoluteFilePath sourceFile, IAbsoluteFilePath destinationFile, IEnumerable<int> codePoints)
    {
        Calls.Add((sourceFile, destinationFile, [.. codePoints]));
        return Task.CompletedTask;
    }
}

public sealed class FakeFileDialogHandler : IFileDialogHandler
{
    public IAbsoluteFilePath? NextOpenResult { get; set; }

    public IAbsoluteFilePath? NextSaveResult { get; set; }

    public Task<IAbsoluteFilePath?> ShowOpenFileDialogAsync(IEnumerable<string> filters) => Task.FromResult(NextOpenResult);

    public Task<IAbsoluteFilePath?> ShowSaveFileDialogAsync(IEnumerable<string> filters, string defaultFileName) => Task.FromResult(NextSaveResult);
}

/// <summary>
/// Minimal service provider for the builder view models.
/// </summary>
public sealed class TestServices : IServiceProvider
{
    public IconsSource IconsSource { get; init; } = FakeIconsSource.Instance;

    public FakeWindow Window { get; } = new();

    public FakeFontSubsetter FontSubsetter { get; } = new();

    public FakeFileDialogHandler FileDialogs { get; } = new();

    public List<IExporter> Exporters { get; } = [];

    public IRecentProjectsStore RecentProjects { get; init; } = new RecentProjectsStore(TestFiles.NewTempPath("recent", ".json"));

    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(IWindow)) return Window;
        if (serviceType == typeof(IconsSource)) return IconsSource;
        if (serviceType == typeof(IFontSubsetter)) return FontSubsetter;
        if (serviceType == typeof(IEnumerable<IExporter>)) return Exporters;
        if (serviceType == typeof(IFileDialogHandler)) return FileDialogs;
        if (serviceType == typeof(IRecentProjectsStore)) return RecentProjects;
        return null;
    }
}

public static class TestFiles
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string NewTempDirectory()
    {
        string dir = Path.Combine(Path.GetTempPath(), "IconPackBuilder.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static string NewTempPath(string name, string extension) => Path.Combine(NewTempDirectory(), name + extension);

    /// <summary>
    /// Writes a project file the way other programs (including git) do: to a new file that is then moved into place.
    /// </summary>
    public static void WriteProject(string path, Version? sourceVersion = null, params (string GroupId, string ExportName, string[] Variants)[] exports)
    {
        var project = new Project {
            Name = "Test.Icons",
            IconsSourceId = FakeIconsSource.SourceId,
            IconsSourceVersion = sourceVersion ?? FakeIconsSource.Instance.Version,
            IconExports = [.. exports.Select(e => new IconExport(e.GroupId, e.ExportName, e.Variants))],
        };

        string tempPath = path + ".new";
        File.WriteAllBytes(tempPath, JsonSerializer.SerializeToUtf8Bytes(project, JsonOptions));
        File.Move(tempPath, path, overwrite: true);
    }

    public static Project ReadProject(string path) => JsonSerializer.Deserialize<Project>(File.ReadAllBytes(path))!;
}
