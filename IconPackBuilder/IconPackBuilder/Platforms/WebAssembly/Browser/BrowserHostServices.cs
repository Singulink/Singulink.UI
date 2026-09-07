using System.Buffers.Binary;
using System.IO.Compression;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using IconPackBuilder.Core;
using IconPackBuilder.Core.Services;
using IconPackBuilder.Data;
using Singulink.IO;

namespace IconPackBuilder.Browser;

/// <summary>
/// Implements the builder's host services for the version that runs in a plain browser tab (https://iconpackbuilder.singulink.com/).
/// Projects live in the runtime's in-memory file system, mirrored to the browser's local storage so they survive reloads; "Open" uploads a
/// project file, and exports are downloaded as a zip. Font subsetting runs fontTools in Pyodide, fetched on first use.
/// </summary>
[SupportedOSPlatform("browser")]
public sealed class BrowserHostServices : IProjectDocumentFactory, IExportService, IFileDialogHandler, IRecentProjectsStore, IHostInfo
{
    private const string StoredFilePrefix = "ipb:file:";
    private static readonly string RootDir = "/home/web_user/IconPackBuilder";
    private static readonly string ProjectsDir = RootDir + "/Projects";

    private readonly RecentProjectsStore _recentProjects = new(RootDir + "/RecentProjects.json");
    private IconsSource? _iconsSource;

    /// <summary>
    /// Gets the icons source. Available after <see cref="InitializeAsync"/> completes.
    /// </summary>
    public IconsSource IconsSource => _iconsSource ?? throw new InvalidOperationException("Host services have not been initialized.");

    /// <inheritdoc/>
    public string StartPageNote =>
        "This is the browser version of Icon Pack Builder. Projects are kept in this browser's local storage; open a project file from your " +
        "computer with \"Open existing project\". Export downloads a zip with the font, the generated files and the project file. The first " +
        "export also downloads the font subsetter (about 10 MB).";

    /// <inheritdoc/>
    public bool CanExit => false;

    /// <summary>
    /// Restores stored projects into the file system and loads the icon metadata from the app package.
    /// </summary>
    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(ProjectsDir);
        RestoreStoredFiles();
        _iconsSource = await WasmAssets.LoadIconsSourceAsync();
    }

    /// <inheritdoc/>
    public IProjectDocument Open(string path) => new BrowserProjectDocument(path);

    /// <inheritdoc/>
    public async Task<string> ExportAsync(ExportRequest request)
    {
        var scratchDir = DirectoryPath.ParseAbsolute(Path.Combine(Path.GetTempPath(), "export-" + Guid.NewGuid().ToString("N")), PathOptions.None);
        scratchDir.Create();

        try
        {
            var context = new ExportContext(request.ProjectName, scratchDir, request.Icons, request.IconsSource.Variants[0], request.FontFileName);

            foreach (var exporter in Exporters.All)
            {
                if (request.Formats.Contains(exporter.Format))
                {
                    try
                    {
                        await exporter.SaveAsync(context);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"{exporter.Name} failed during execution:\n{ex.Message}", ex);
                    }
                }
            }

            byte[] fontBytes = await WasmAssets.ReadAllBytesAsync(request.IconsSource.FontFile);

            // A failed package fetch yields the server's error page rather than an exception, so check the sfnt signature before subsetting.
            if (!IsSfntFont(fontBytes))
                throw new InvalidOperationException($"The icon font '{request.IconsSource.FontFile.PathDisplay}' could not be loaded from the app package.");

            byte[] subsetFont;

            try
            {
                string codePointsJson = JsonSerializer.Serialize(request.CodePoints, BrowserJsonContext.Default.IReadOnlyListInt32);
                subsetFont = Convert.FromBase64String(await BrowserHost.SubsetFont(Convert.ToBase64String(fontBytes), codePointsJson));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create subset font file:\n{ex.Message}", ex);
            }

            byte[]? projectBytes = await request.Document.ReadAsync();
            string zipName = request.ExportDirectoryName + ".zip";

            using var zipBuffer = new MemoryStream();

            using (var zip = new ZipArchive(zipBuffer, ZipArchiveMode.Create, leaveOpen: true))
            {
                await AddEntryAsync(zip, request.ExportDirectoryName + "/" + request.FontFileName, subsetFont);

                foreach (var file in scratchDir.GetChildFiles().OrderBy(f => f.Name, StringComparer.Ordinal))
                    await AddEntryAsync(zip, request.ExportDirectoryName + "/" + file.Name, await File.ReadAllBytesAsync(file.PathExport));

                if (projectBytes is not null)
                    await AddEntryAsync(zip, Path.GetFileName(request.Document.Path), projectBytes);
            }

            BrowserHost.DownloadFile(zipName, Convert.ToBase64String(zipBuffer.ToArray()), "application/zip");
            return zipName + " (downloaded)";
        }
        finally
        {
            scratchDir.Delete(recursive: true);
        }
    }

    /// <inheritdoc/>
    async Task<IAbsoluteFilePath?> IFileDialogHandler.ShowOpenFileDialogAsync(IEnumerable<string> filters)
    {
        string? resultJson = await BrowserHost.PickFile(string.Join(',', filters));

        if (resultJson is null)
            return null;

        var picked = JsonSerializer.Deserialize(resultJson, BrowserJsonContext.Default.PickedFile) ??
            throw new InvalidOperationException("The browser returned no file.");

        string path = ProjectsDir + "/" + Path.GetFileName(picked.Name);
        await File.WriteAllBytesAsync(path, Encoding.UTF8.GetBytes(picked.Text));
        PersistFile(path);

        return FilePath.ParseAbsolute(path, PathOptions.None);
    }

    /// <inheritdoc/>
    Task<IAbsoluteFilePath?> IFileDialogHandler.ShowSaveFileDialogAsync(IEnumerable<string> filters, string defaultFileName)
    {
        // There is nowhere to browse to: new projects go straight into the projects folder, replacing any project of the same name.
        return Task.FromResult<IAbsoluteFilePath?>(FilePath.ParseAbsolute(ProjectsDir + "/" + defaultFileName, PathOptions.None));
    }

    /// <inheritdoc/>
    public IReadOnlyList<RecentProject> Projects => _recentProjects.Projects;

    /// <inheritdoc/>
    public async Task AddOrUpdateAsync(string path, string name)
    {
        await _recentProjects.AddOrUpdateAsync(path, name);
        PersistRecentProjects();
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(string path)
    {
        await _recentProjects.RemoveAsync(path);
        PersistRecentProjects();
    }

    /// <inheritdoc/>
    public async Task ClearAsync()
    {
        await _recentProjects.ClearAsync();
        PersistRecentProjects();
    }

    /// <summary>
    /// Mirrors a file in the in-memory file system to local storage so it is restored on the next visit.
    /// </summary>
    internal static void PersistFile(string path)
    {
        if (File.Exists(path))
            BrowserHost.StorageSet(StoredFilePrefix + path, Convert.ToBase64String(File.ReadAllBytes(path)));
        else
            BrowserHost.StorageRemove(StoredFilePrefix + path);
    }

    private static void PersistRecentProjects() => PersistFile(RootDir + "/RecentProjects.json");

    private static void RestoreStoredFiles()
    {
        var keys = JsonSerializer.Deserialize(BrowserHost.StorageKeys(StoredFilePrefix), BrowserJsonContext.Default.StringArray) ?? [];

        foreach (string key in keys)
        {
            string path = key[StoredFilePrefix.Length..];

            try
            {
                if (BrowserHost.StorageGet(key) is string content)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    File.WriteAllBytes(path, Convert.FromBase64String(content));
                }
            }
            catch (Exception ex) when (ex is IOException or FormatException)
            {
                Console.Error.WriteLine($"[IconPackBuilder] Could not restore '{path}' from local storage: {ex.Message}");
            }
        }
    }

    private static bool IsSfntFont(byte[] bytes)
    {
        if (bytes.Length < 4)
            return false;

        ReadOnlySpan<byte> tag = bytes.AsSpan(0, 4);
        return tag.SequenceEqual("OTTO"u8) || tag.SequenceEqual("true"u8) || BinaryPrimitives.ReadUInt32BigEndian(tag) == 0x00010000;
    }

    private static async Task AddEntryAsync(ZipArchive zip, string entryName, byte[] content)
    {
        var entry = zip.CreateEntry(entryName, CompressionLevel.Optimal);
        using var stream = entry.Open();
        await stream.WriteAsync(content);
    }
}

/// <summary>
/// A project in the in-memory file system, mirrored to local storage on every write (and on first read, so projects created by the start
/// screen are persisted before they are first saved).
/// </summary>
[SupportedOSPlatform("browser")]
internal sealed class BrowserProjectDocument(string path) : IProjectDocument
{
    public string Path { get; } = path;

    public bool HostOwnsPersistence => false;

    // Nothing else can modify the file while the page is open.
    public event EventHandler? Changed { add { } remove { } }

    public async Task<byte[]?> ReadAsync()
    {
        if (!File.Exists(Path))
            return null;

        byte[] bytes = await File.ReadAllBytesAsync(Path);
        BrowserHostServices.PersistFile(Path);
        return bytes;
    }

    public async Task WriteAsync(byte[] content)
    {
        await File.WriteAllBytesAsync(Path, content);
        BrowserHostServices.PersistFile(Path);
    }

    public void Dispose()
    {
    }
}

internal sealed record PickedFile(string Name, string Text);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(PickedFile))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(IReadOnlyList<int>))]
internal sealed partial class BrowserJsonContext : JsonSerializerContext;
