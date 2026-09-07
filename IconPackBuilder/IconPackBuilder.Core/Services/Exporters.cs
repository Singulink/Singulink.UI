namespace IconPackBuilder.Core.Services;

/// <summary>
/// The exporters the builder ships with, one per <see cref="Data.ExportFormat"/>.
/// </summary>
public static class Exporters
{
    public static IReadOnlyList<IExporter> All { get; } = [CSharpExporter.Instance, CssExporter.Instance, JavaScriptExporter.Instance];
}
