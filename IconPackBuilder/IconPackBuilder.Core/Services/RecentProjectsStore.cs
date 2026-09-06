using System.Text.Json;
using IconPackBuilder.Data;

namespace IconPackBuilder.Core.Services;

/// <summary>
/// File-backed <see cref="IRecentProjectsStore"/>. Persistence is best-effort: a missing or unreadable file yields an empty list and write
/// failures are ignored, since losing the list is preferable to blocking the user.
/// </summary>
public sealed class RecentProjectsStore : IRecentProjectsStore
{
    private const int MaxProjects = 10;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _filePath;
    private List<RecentProject>? _projects;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecentProjectsStore"/> class that stores the list in the given file, or in the user's local
    /// application data folder if no file is specified.
    /// </summary>
    public RecentProjectsStore(string? filePath = null)
    {
        _filePath = filePath ??
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Singulink", "IconPackBuilder", "RecentProjects.json");
    }

    public IReadOnlyList<RecentProject> Projects => _projects ??= Load();

    public async Task AddOrUpdateAsync(string path, string name)
    {
        var projects = _projects ??= Load();

        projects.RemoveAll(p => IsSamePath(p.Path, path));
        projects.Insert(0, new RecentProject(path, name, DateTime.UtcNow));

        if (projects.Count > MaxProjects)
            projects.RemoveRange(MaxProjects, projects.Count - MaxProjects);

        await SaveAsync();
    }

    public async Task RemoveAsync(string path)
    {
        var projects = _projects ??= Load();

        if (projects.RemoveAll(p => IsSamePath(p.Path, path)) > 0)
            await SaveAsync();
    }

    public async Task ClearAsync()
    {
        _projects = [];
        await SaveAsync();
    }

    private static bool IsSamePath(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private List<RecentProject> Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return [];

            using var stream = File.OpenRead(_filePath);
            var projects = JsonSerializer.Deserialize<List<RecentProject>>(stream, JsonOptions) ?? [];
            projects.RemoveAll(p => string.IsNullOrWhiteSpace(p.Path));
            return projects;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return [];
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

            string tempPath = _filePath + ".tmp";
            await File.WriteAllBytesAsync(tempPath, JsonSerializer.SerializeToUtf8Bytes(_projects, JsonOptions));
            File.Move(tempPath, _filePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
