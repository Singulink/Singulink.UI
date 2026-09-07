using Timer = System.Timers.Timer;

namespace IconPackBuilder.Core.Services;

/// <summary>
/// A project document stored as a file on disk. The file is never held open so other programs (including git) can read and replace it while the
/// editor is open; changes are detected with a <see cref="FileSystemWatcher"/> and writes are atomic (temporary file moved into place).
/// </summary>
public sealed class FileProjectDocument : IProjectDocument
{
    private const int ChangeDebounceMs = 300;

    private readonly Lock _lock = new();
    private FileSystemWatcher? _watcher;
    private Timer? _debounceTimer;
    private EventHandler? _changedHandlers;

    public FileProjectDocument(string path)
    {
        Path = System.IO.Path.GetFullPath(path);
    }

    /// <inheritdoc/>
    public string Path { get; }

    /// <inheritdoc/>
    public bool HostOwnsPersistence => false;

    /// <inheritdoc/>
    public event EventHandler? Changed {
        add {
            lock (_lock)
            {
                _changedHandlers += value;
                EnsureWatching();
            }
        }
        remove {
            lock (_lock)
                _changedHandlers -= value;
        }
    }

    /// <inheritdoc/>
    public async Task<byte[]?> ReadAsync()
    {
        if (!File.Exists(Path))
            return null;

        // Retry briefly if another process is still writing the file.
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                return await File.ReadAllBytesAsync(Path).ConfigureAwait(false);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (IOException) when (attempt < 5)
            {
                await Task.Delay(100).ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc/>
    public async Task WriteAsync(byte[] content)
    {
        // Write to a temporary file and move it into place so the file is never observed half-written.
        string tempPath = Path + ".tmp";
        await File.WriteAllBytesAsync(tempPath, content).ConfigureAwait(false);
        File.Move(tempPath, Path, overwrite: true);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_lock)
        {
            _watcher?.Dispose();
            _watcher = null;
            _debounceTimer?.Dispose();
            _debounceTimer = null;
        }
    }

    private void EnsureWatching()
    {
        if (_watcher is not null)
            return;

        _watcher = new FileSystemWatcher(System.IO.Path.GetDirectoryName(Path)!, System.IO.Path.GetFileName(Path)) {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName | NotifyFilters.CreationTime,
        };

        // Most programs (including git) write a new file and rename it into place, so renames and creations matter as much as writes.
        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileChanged;
        _watcher.Renamed += OnFileChanged;
        _watcher.EnableRaisingEvents = true;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // Writers typically raise several notifications per save, so coalesce them.
        lock (_lock)
        {
            if (_watcher is null)
                return;

            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(ChangeDebounceMs) { AutoReset = false };
            _debounceTimer.Elapsed += (s, e) => _changedHandlers?.Invoke(this, EventArgs.Empty);
            _debounceTimer.Start();
        }
    }
}

/// <summary>
/// Opens <see cref="FileProjectDocument"/> instances.
/// </summary>
public sealed class FileProjectDocumentFactory : IProjectDocumentFactory
{
    public IProjectDocument Open(string path) => new FileProjectDocument(path);
}
