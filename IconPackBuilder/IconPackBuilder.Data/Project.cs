namespace IconPackBuilder.Data;

public sealed class Project
{
    public string Name { get; set; } = string.Empty;

    public string IconsSourceId { get; set; } = string.Empty;

    public Version IconsSourceVersion { get; set; } = new(0, 0);

    /// <summary>
    /// Gets or sets the formats written by an export. <see langword="null"/> (the property is absent from the file) means every format.
    /// </summary>
    public List<ExportFormat>? ExportFormats { get; set; }

    public List<IconExport> IconExports { get; set; } = [];

    /// <summary>
    /// Gets the formats an export of this project writes, resolving the "all formats" default.
    /// </summary>
    public IReadOnlyCollection<ExportFormat> GetExportFormats() => ExportFormats ?? (IReadOnlyCollection<ExportFormat>)Enum.GetValues<ExportFormat>();
}
