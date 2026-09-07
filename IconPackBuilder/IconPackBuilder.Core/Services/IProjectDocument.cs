namespace IconPackBuilder.Core.Services;

/// <summary>
/// The persisted form of an open project. On the desktop this is a file on disk; inside an editor host such as Visual Studio Code it is a
/// document owned by the host.
/// </summary>
public interface IProjectDocument : IDisposable
{
    /// <summary>
    /// Gets the full path of the document, used for display, the recent projects list and as the location exports are written next to.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets a value indicating whether the host owns persistence of the document. When <see langword="true"/>, the host tracks dirty state,
    /// saving and undo, so the editor pushes every change through <see cref="WriteAsync"/> immediately instead of offering Save/Close commands
    /// and prompting about unsaved changes.
    /// </summary>
    public bool HostOwnsPersistence { get; }

    /// <summary>
    /// Raised (on an arbitrary thread) when the document content was changed by something other than <see cref="WriteAsync"/>. Notifications
    /// are debounced by the implementation; the editor compares content hashes to filter out notifications for its own writes.
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Reads the current document content, or returns <see langword="null"/> if the document no longer exists.
    /// </summary>
    public Task<byte[]?> ReadAsync();

    /// <summary>
    /// Replaces the document content.
    /// </summary>
    public Task WriteAsync(byte[] content);
}

/// <summary>
/// Opens project documents by path.
/// </summary>
public interface IProjectDocumentFactory
{
    public IProjectDocument Open(string path);
}
