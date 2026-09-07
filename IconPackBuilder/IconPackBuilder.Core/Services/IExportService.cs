using IconPackBuilder.Data;

namespace IconPackBuilder.Core.Services;

/// <summary>
/// Exports a project to its export folder.
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Runs the export and returns the display path of the folder it was written to. Throws <see cref="InvalidOperationException"/> with a
    /// user-presentable message if the export fails.
    /// </summary>
    public Task<string> ExportAsync(ExportRequest request);
}

/// <summary>
/// Everything needed to export a project: the subset font is produced from <see cref="CodePoints"/> and the exporters for the enabled
/// <see cref="Formats"/> run over <see cref="Icons"/>.
/// </summary>
/// <param name="ProjectName">The project name in <c>Namespace.Class</c> form; also used to name the export folder and font file.</param>
/// <param name="Document">The project document; exports are written to a <c>&lt;ProjectName&gt;_Export</c> folder beside it.</param>
/// <param name="IconsSource">The icons source providing the font to subset.</param>
/// <param name="Icons">The selected icons with their final export names.</param>
/// <param name="Formats">The output formats to write alongside the subset font.</param>
public sealed record ExportRequest(
    string ProjectName,
    IProjectDocument Document,
    IconsSource IconsSource,
    IReadOnlyList<ExportIconInfo> Icons,
    IReadOnlyCollection<ExportFormat> Formats)
{
    /// <summary>
    /// Gets all code points to keep in the subset font, including right-to-left glyphs.
    /// </summary>
    public IReadOnlyList<int> CodePoints { get; } = GetCodePoints(Icons);

    /// <summary>
    /// Gets the name of the folder the export is written to, beside the project document.
    /// </summary>
    public string ExportDirectoryName => ProjectName + "_Export";

    /// <summary>
    /// Gets the file name of the subset font.
    /// </summary>
    public string FontFileName => ProjectName + IconsSource.FontFile.Extension;

    private static List<int> GetCodePoints(IReadOnlyList<ExportIconInfo> icons)
    {
        var codePoints = new SortedSet<int>();

        foreach (var icon in icons)
        {
            codePoints.Add(icon.Icon.CodePoint);

            if (icon.Icon.RtlCodePoint is int rtlCodePoint)
                codePoints.Add(rtlCodePoint);
        }

        return [.. codePoints];
    }
}
