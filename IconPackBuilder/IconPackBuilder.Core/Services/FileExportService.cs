using Singulink.IO;

namespace IconPackBuilder.Core.Services;

/// <summary>
/// Exports to the file system: subsets the font with an <see cref="IFontSubsetter"/> and runs the exporters for the project's enabled formats
/// into the export folder beside the project file.
/// </summary>
public sealed class FileExportService(IFontSubsetter fontSubsetter, IEnumerable<IExporter> exporters) : IExportService
{
    private readonly IReadOnlyList<IExporter> _exporters = [.. exporters];

    /// <inheritdoc/>
    public async Task<string> ExportAsync(ExportRequest request)
    {
        var projectFile = FilePath.ParseAbsolute(request.Document.Path, PathOptions.None);
        var exportDir = projectFile.ParentDirectory.CombineDirectory(request.ExportDirectoryName, PathOptions.None);
        var fontFile = DirectoryPath.GetAppBase() + request.IconsSource.FontFile;
        var subsetFontFile = exportDir.CombineFile(request.FontFileName, PathOptions.None);

        exportDir.Delete(recursive: true);
        exportDir.Create();

        try
        {
            await fontSubsetter.SaveAsync(fontFile, subsetFontFile, request.CodePoints).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create subset font file:\n{ex.Message}", ex);
        }

        var context = new ExportContext(request.ProjectName, exportDir, request.Icons, request.IconsSource.Variants[0], request.FontFileName);

        foreach (var exporter in _exporters)
        {
            if (!request.Formats.Contains(exporter.Format))
                continue;

            try
            {
                await exporter.SaveAsync(context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"{exporter.Name} failed during execution:\n{ex.Message}", ex);
            }
        }

        return exportDir.PathDisplay;
    }
}
