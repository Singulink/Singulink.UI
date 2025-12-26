namespace IconPackBuilder.Core.Services;

public record ExportIconInfo(string ExportName, IconInfo Icon)
{
    public string ExportName { get; init; } = string.IsNullOrWhiteSpace(ExportName) ? Icon.Group.Id : ExportName;
}
