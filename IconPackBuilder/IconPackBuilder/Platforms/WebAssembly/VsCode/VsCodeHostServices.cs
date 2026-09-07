using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using IconPackBuilder.Core;
using IconPackBuilder.Core.IconSources;
using IconPackBuilder.Core.Services;
using Singulink.IO;

namespace IconPackBuilder.VsCode;

/// <summary>
/// Implements the builder's host services on top of the VS Code extension bridge: the project document is the text document VS Code has open,
/// and exports are performed by the extension host (which owns the file system and the pyftsubset binary).
/// </summary>
[SupportedOSPlatform("browser")]
public sealed class VsCodeHostServices : IProjectDocumentFactory, IExportService, IFileDialogHandler, IDisposable
{
    private VsCodeProjectDocument? _document;
    private SeagullIconsSource? _iconsSource;

    /// <summary>
    /// Gets the icons source. Available after <see cref="InitializeAsync"/> completes.
    /// </summary>
    public IconsSource IconsSource => _iconsSource ?? throw new InvalidOperationException("Host services have not been initialized.");

    /// <summary>
    /// Gets the path of the document VS Code opened. Available after <see cref="InitializeAsync"/> completes.
    /// </summary>
    public string DocumentPath => _document?.Path ?? throw new InvalidOperationException("Host services have not been initialized.");

    /// <summary>
    /// Waits for the bridge to deliver the document and loads the icon metadata from the app package.
    /// </summary>
    public async Task InitializeAsync()
    {
        await VsCodeHost.WhenReady();

        string dir = VsCodeHost.GetDocumentDir();
        string fileName = VsCodeHost.GetDocumentFileName();
        _document = new VsCodeProjectDocument(JoinPath(dir, fileName));

        _iconsSource = await WasmAssets.LoadIconsSourceAsync();
    }

    /// <inheritdoc/>
    public IProjectDocument Open(string path)
    {
        // The host provides exactly one document; the route parameter is only there so the editor route stays the same on every platform.
        if (_document is null || !string.Equals(path, _document.Path, StringComparison.Ordinal))
            throw new InvalidOperationException($"Only the document opened by VS Code ('{_document?.Path}') can be edited here.");

        return _document;
    }

    /// <inheritdoc/>
    public async Task<string> ExportAsync(ExportRequest request)
    {
        // Exporters write files, so run them into a scratch folder on the in-memory file system and ship the results to the host.
        var scratchDir = DirectoryPath.ParseAbsolute(Path.Combine(Path.GetTempPath(), "export-" + Guid.NewGuid().ToString("N")), PathOptions.None);
        scratchDir.Create();

        try
        {
            var context = new ExportContext(request.ProjectName, scratchDir, request.Icons, request.IconsSource.Variants[0], request.FontFileName);

            foreach (var exporter in Exporters.All)
            {
                if (request.Formats.Contains(exporter.Format))
                    await exporter.SaveAsync(context);
            }

            var files = new List<HostExportFile>();

            foreach (var file in scratchDir.GetChildFiles())
                files.Add(new HostExportFile(file.Name, Convert.ToBase64String(await File.ReadAllBytesAsync(file.PathExport))));

            byte[] fontBytes = await WasmAssets.ReadAllBytesAsync(request.IconsSource.FontFile);

            var hostRequest = new HostExportRequest(
                request.ProjectName,
                request.ExportDirectoryName,
                request.FontFileName,
                Convert.ToBase64String(fontBytes),
                request.CodePoints,
                files);

            string resultJson = await VsCodeHost.ExportProject(JsonSerializer.Serialize(hostRequest, HostJsonContext.Default.HostExportRequest));
            var result = JsonSerializer.Deserialize(resultJson, HostJsonContext.Default.HostExportResult) ??
                throw new InvalidOperationException("The VS Code host returned no export result.");

            if (!result.Ok)
                throw new InvalidOperationException(result.Error ?? "The export failed in the VS Code host.");

            return result.ExportDir;
        }
        finally
        {
            scratchDir.Delete(recursive: true);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _document?.Dispose();
        _document = null;
    }

    // The start screen is never shown in VS Code, so no file dialogs are needed.
    Task<IAbsoluteFilePath?> IFileDialogHandler.ShowOpenFileDialogAsync(IEnumerable<string> filters) => Task.FromResult<IAbsoluteFilePath?>(null);

    Task<IAbsoluteFilePath?> IFileDialogHandler.ShowSaveFileDialogAsync(IEnumerable<string> filters, string defaultFileName) => Task.FromResult<IAbsoluteFilePath?>(null);

    private static string JoinPath(string dir, string fileName)
    {
        if (dir.Length is 0)
            return fileName;

        // The document lives on the host's file system, which may use either separator regardless of the (Unix-like) WebAssembly runtime.
        char separator = dir.Contains('\\') ? '\\' : '/';
        return dir.EndsWith(separator) ? dir + fileName : dir + separator + fileName;
    }
}

/// <summary>
/// The text document VS Code has open. Writes are pushed to the host immediately, which tracks dirty state, saving and undo.
/// </summary>
[SupportedOSPlatform("browser")]
internal sealed class VsCodeProjectDocument : IProjectDocument
{
    private static bool _callbackRegistered;
    private static VsCodeProjectDocument? _current;

    public VsCodeProjectDocument(string path)
    {
        Path = path;
        _current = this;

        if (!_callbackRegistered)
        {
            _callbackRegistered = true;
            VsCodeHost.OnDocumentChanged(_ => _current?.Changed?.Invoke(_current, EventArgs.Empty));
        }
    }

    public string Path { get; }

    public bool HostOwnsPersistence => true;

    public event EventHandler? Changed;

    public Task<byte[]?> ReadAsync() => Task.FromResult<byte[]?>(Encoding.UTF8.GetBytes(VsCodeHost.GetDocumentText()));

    public Task WriteAsync(byte[] content)
    {
        VsCodeHost.SetDocumentText(Encoding.UTF8.GetString(content));
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_current == this)
            _current = null;
    }
}

/// <summary>
/// A file produced by an exporter, shipped to the host for writing.
/// </summary>
internal sealed record HostExportFile(string Name, string ContentBase64);

/// <summary>
/// The export request understood by the extension host (see IconPackBuilder.VSCode/README.md).
/// </summary>
internal sealed record HostExportRequest(
    string ProjectName, string ExportDirName, string FontFileName, string FontBase64, IReadOnlyList<int> CodePoints, IReadOnlyList<HostExportFile> Files);

/// <summary>
/// The export result returned by the extension host.
/// </summary>
internal sealed record HostExportResult(bool Ok, string ExportDir, string? Error);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(HostExportRequest))]
[JsonSerializable(typeof(HostExportResult))]
internal sealed partial class HostJsonContext : JsonSerializerContext;
