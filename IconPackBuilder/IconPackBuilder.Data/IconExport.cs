namespace IconPackBuilder.Data;

public record IconExport(string GroupId, string ExportName, IReadOnlyList<string> Variants);
