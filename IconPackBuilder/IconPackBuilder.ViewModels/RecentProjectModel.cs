using IconPackBuilder.Data;

namespace IconPackBuilder.ViewModels;

public sealed class RecentProjectModel(RecentProject project)
{
    public string Name => project.Name;

    public string Path => project.Path;

    public string Folder { get; } = System.IO.Path.GetDirectoryName(project.Path) ?? project.Path;

    /// <summary>
    /// Gets a value indicating whether the project file was missing when the list was last refreshed.
    /// </summary>
    public bool IsMissing { get; } = !File.Exists(project.Path);

    public double Opacity => IsMissing ? 0.5 : 1;
}
