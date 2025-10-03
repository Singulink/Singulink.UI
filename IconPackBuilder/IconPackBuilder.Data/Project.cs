namespace IconPackBuilder.Data;

public sealed class Project
{
    public string Name { get; set; } = string.Empty;

    public string IconsSourceId { get; set; } = string.Empty;

    public Version IconsSourceVersion { get; set; } = new(0, 0);

    public List<IconExport> IconExports { get; set; } = [];
}
