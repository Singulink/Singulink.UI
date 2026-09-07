using IconPackBuilder.Data;

namespace IconPackBuilder.Core.Services;

/// <summary>
/// Writes one output format (e.g. the C# class) of an export next to the subset font.
/// </summary>
public interface IExporter
{
    /// <summary>
    /// Gets the display name of the exporter, used in error messages.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the format this exporter produces, used to match it against the formats a project has enabled.
    /// </summary>
    public ExportFormat Format { get; }

    public Task SaveAsync(ExportContext context);
}
