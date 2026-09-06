namespace IconPackBuilder.Data;

/// <summary>
/// An entry in the recent projects list.
/// </summary>
public sealed record RecentProject(string Path, string Name, DateTime LastOpenedUtc);
