using System.Text.Json;
using IconPackBuilder.Data;

namespace IconPackBuilder.Core.Services;

/// <summary>
/// Exports a saved project without the editor, for scripts and CI (<c>iconpackbuilder export MyApp.Icons.ipproj</c>).
/// </summary>
public static class HeadlessExport
{
    public const string Usage = "Usage: export <project.ipproj>";

    /// <summary>
    /// Builds the export request for a saved project, resolving its icons against the icon source. Unlike the editor, which loads what it can
    /// and warns about the rest, anything missing is an error so that a scripted export never silently produces an incomplete pack. Throws
    /// <see cref="InvalidOperationException"/> with a user-presentable message for problems.
    /// </summary>
    /// <param name="project">The saved project.</param>
    /// <param name="document">The project document; the export folder is created beside it.</param>
    /// <param name="iconsSource">The icon source to resolve the project's icons against.</param>
    /// <param name="warnings">Receives non-fatal problems, such as the project having been saved with a newer icon source.</param>
    public static ExportRequest CreateRequest(Project project, IProjectDocument document, IconsSource iconsSource, ICollection<string>? warnings = null)
    {
        if (string.IsNullOrWhiteSpace(project.Name))
            throw new InvalidOperationException("The project has no name.");

        if (project.IconsSourceId != iconsSource.Id)
            throw new InvalidOperationException($"The project requires icon source '{project.IconsSourceId}', but this build of Icon Pack Builder has '{iconsSource.Id}'.");

        if (project.IconsSourceVersion > iconsSource.Version)
        {
            warnings?.Add(
                $"The project was saved with icon source version {project.IconsSourceVersion}, but this build of Icon Pack Builder has " +
                $"version {iconsSource.Version}. Icons added in the newer version will be reported as missing.");
        }

        var groupsById = iconsSource.LoadIconGroups().ToDictionary(g => g.Id, StringComparer.Ordinal);
        var icons = new List<ExportIconInfo>();

        foreach (var export in project.IconExports)
        {
            if (!groupsById.TryGetValue(export.GroupId, out var group))
                throw new InvalidOperationException($"Icon '{export.GroupId}' is missing from icon source '{iconsSource.Name}' version {iconsSource.Version}.");

            string exportName = string.IsNullOrWhiteSpace(export.ExportName) ? group.Id : export.ExportName;

            foreach (string variant in export.Variants)
            {
                var icon = group.Icons.FirstOrDefault(i => i.Variant == variant) ??
                    throw new InvalidOperationException($"Icon '{group.Name}' has no '{variant}' variant in icon source '{iconsSource.Name}' version {iconsSource.Version}.");

                icons.Add(new ExportIconInfo(exportName, icon));
            }
        }

        return new ExportRequest(project.Name, document, iconsSource, icons, project.GetExportFormats());
    }

    /// <summary>
    /// Exports the project file given in <paramref name="args"/> and returns the process exit code: 0 on success, 1 if the export failed and 2
    /// for invalid arguments. Progress and problems are written to the given writers.
    /// </summary>
    public static async Task<int> RunAsync(IReadOnlyList<string> args, IconsSource iconsSource, IExportService exportService, TextWriter output, TextWriter error)
    {
        if (args.Count is not 1 || args[0].StartsWith('-'))
        {
            await error.WriteLineAsync(Usage);
            return 2;
        }

        string projectPath = Path.GetFullPath(args[0]);

        if (!File.Exists(projectPath))
        {
            await error.WriteLineAsync($"Project file not found: {projectPath}");
            return 1;
        }

        try
        {
            using var document = new FileProjectDocument(projectPath);
            byte[] bytes = await document.ReadAsync() ?? throw new InvalidOperationException($"Project file not found: {projectPath}");

            var project = JsonSerializer.Deserialize(bytes, ProjectJsonContext.Default.Project) ??
                throw new InvalidOperationException("The project file is empty.");

            var warnings = new List<string>();
            var request = CreateRequest(project, document, iconsSource, warnings);

            foreach (string warning in warnings)
                await error.WriteLineAsync("warning: " + warning);

            string exportDir = await exportService.ExportAsync(request);
            await output.WriteLineAsync($"Exported {request.Icons.Count} icon(s) from '{project.Name}' to: {exportDir}");
            return 0;
        }
        catch (Exception ex) when (ex is InvalidOperationException or JsonException or IOException or UnauthorizedAccessException)
        {
            await error.WriteLineAsync("error: " + ex.Message);
            return 1;
        }
    }
}
