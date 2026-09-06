using IconPackBuilder.Data;

namespace IconPackBuilder.Core.Services;

/// <summary>
/// Persists the list of recently opened projects, most recent first.
/// </summary>
public interface IRecentProjectsStore
{
    public IReadOnlyList<RecentProject> Projects { get; }

    /// <summary>
    /// Moves the project to the top of the list, adding it if necessary.
    /// </summary>
    public Task AddOrUpdateAsync(string path, string name);

    public Task RemoveAsync(string path);

    public Task ClearAsync();
}
