using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IconPackBuilder.Core;
using IconPackBuilder.Core.Services;
using IconPackBuilder.Data;
using IconPackBuilder.ViewModels.Utilities;
using Singulink.IO;
using Singulink.UI.Navigation;
using Singulink.UI.Tasks;
using Timer = System.Timers.Timer;

namespace IconPackBuilder.ViewModels;

public partial class EditorRootModel : ObservableObject, IRoutedViewModel<string>, IRoutedViewModelBase, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly IReadOnlyList<IconGroupModel> _iconGroups;
    private readonly FrozenDictionary<string, IconGroupModel> _iconGroupsById;
    private readonly IWindow _window;
    private readonly IFontSubsetter _fontSubsetter;
    private readonly IReadOnlyList<IExporter> _exporters;
    private readonly IRecentProjectsStore _recentProjects;

    private readonly Lock _reloadTimerLock = new();

    private Timer? _nameFilterDebounceTimer;
    private Timer? _reloadDebounceTimer;
    private FileSystemWatcher? _projectFileWatcher;
    private byte[]? _projectFileHash;
    private bool _isHandlingExternalChange;

    public bool CanBeCached => false;

    public IAbsoluteFilePath ProjectFile { get; }

    [ObservableProperty]
    public partial string ProjectName { get; private set; } = string.Empty;

    public IconsSource IconsSource { get; }

    [ObservableProperty]
    public partial string NameFilter { get; set; } = string.Empty;

    partial void OnNameFilterChanged(string value)
    {
        // Debounce filter update for 1 second after last key press
        _nameFilterDebounceTimer?.Stop();
        _nameFilterDebounceTimer?.Dispose();

        _nameFilterDebounceTimer = new Timer(500) { AutoReset = false };
        _nameFilterDebounceTimer.Elapsed += (s, e) => this.TaskRunner.Post(() => OnFilterChanged(false));
        _nameFilterDebounceTimer.Start();
    }

    public IReadOnlyList<string> VariantFilters { get; }

    [ObservableProperty]
    [MemberNotNull(nameof(FilteredIconGroups))]
    public partial string VariantFilter { get; set; }

    partial void OnVariantFilterChanged(string value) => OnFilterChanged(true);

    [ObservableProperty]
    public partial bool RtlVersionsOnlyFilter { get; set; }

    partial void OnRtlVersionsOnlyFilterChanged(bool value) => OnFilterChanged(false);

    [ObservableProperty]
    public partial bool IncludedOnlyFilter { get; set; }

    partial void OnIncludedOnlyFilterChanged(bool value) => OnFilterChanged(false);

    [ObservableProperty]
    public partial bool IsDirty { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<IconGroupModel> FilteredIconGroups { get; set; }

    [ObservableProperty]
    public partial IconGroupModel? SelectedIconGroup { get; set; }

    partial void OnSelectedIconGroupChanged(IconGroupModel? oldValue, IconGroupModel? newValue)
    {
        if (oldValue is not null)
            oldValue.IsSelected = false;

        if (newValue is not null)
            newValue.IsSelected = true;
    }

    public EditorRootModel(
        IWindow window, IconsSource iconsSource, IFontSubsetter fontSubsetter, IEnumerable<IExporter> exporters, IRecentProjectsStore recentProjects)
    {
        _recentProjects = recentProjects;
        _window = window;
        IconsSource = iconsSource;
        _fontSubsetter = fontSubsetter;
        _exporters = [.. exporters];

        ProjectFile = FilePath.ParseAbsolute(this.Parameter, PathOptions.None);

        _iconGroups = [.. iconsSource
            .LoadIconGroups()
            .Select(ig => new IconGroupModel(this, ig))
            .OrderBy(ig => ig.Info.Name, StringComparer.InvariantCulture)
        ];

        _iconGroupsById = _iconGroups.ToFrozenDictionary(ig => ig.Info.Id);

        VariantFilters = ["All", .. iconsSource.Variants];
        VariantFilter = VariantFilters[0];
    }

    public async Task OnNavigatedToAsync(NavigationArgs args) => await LoadProjectAsync(args);

    public async Task OnNavigatingAwayAsync(NavigatingArgs args)
    {
        if (IsDirty)
        {
            int result = await this.Navigator.ShowMessageDialogAsync(
                "Do you want to save changes to this project?", "Unsaved Changes", ["Save", "Discard", "Cancel"]);

            if (result is 0)
            {
                bool saved = await SaveProjectInternalAsync();

                if (!saved)
                    args.Cancel = true;
            }
            else if (result is 2)
            {
                args.Cancel = true;
            }
        }
    }

    /// <summary>
    /// Releases the project file watcher and timers. Called by the navigator when the view model is discarded after navigating away.
    /// </summary>
    public void Dispose()
    {
        _projectFileWatcher?.Dispose();
        _projectFileWatcher = null;

        lock (_reloadTimerLock)
        {
            _reloadDebounceTimer?.Dispose();
            _reloadDebounceTimer = null;
        }

        _nameFilterDebounceTimer?.Dispose();
        _nameFilterDebounceTimer = null;

        GC.SuppressFinalize(this);
    }

    [RelayCommand]
    private async Task SaveProjectAsync() => await SaveProjectInternalAsync();

    [RelayCommand]
    private async Task ExportProjectAsync()
    {
        if (IsDirty)
        {
            int result = await this.Navigator.ShowMessageDialogAsync(
                "You have unsaved changes. Do you want to save the project before exporting?",
                "Unsaved Changes",
                ["Save and Export", "Export Without Saving", "Cancel"]);

            if (result is 0)
            {
                if (!await SaveProjectInternalAsync())
                    return;
            }
            else if (result is 2)
            {
                return;
            }
        }

        using var busy = this.TaskRunner.EnterBusyScope();

        var exportDir = ProjectFile.ParentDirectory.CombineDirectory(ProjectName + "_Export", PathOptions.None);
        var fontFile = DirectoryPath.GetAppBase() + IconsSource.FontFile;
        var subsetFontFile = exportDir.CombineFile(ProjectName + IconsSource.FontFile.Extension, PathOptions.None);

        var selectedIcons = _iconGroups
            .SelectMany(ig => ig.Icons.Where(i => i.IsSelected).Select(i => (Group: ig, Icon: i)))
            .ToList();

        var allCodePoints = new List<int>();

        foreach (var (_, icon) in selectedIcons)
        {
            allCodePoints.Add(icon.Info.CodePoint);

            if (icon.Info.RtlCodePoint.HasValue)
                allCodePoints.Add(icon.Info.RtlCodePoint.Value);
        }

        exportDir.Delete(recursive: true);
        exportDir.Create();

        try
        {
            await _fontSubsetter.SaveAsync(fontFile, subsetFontFile, allCodePoints);
        }
        catch (Exception ex)
        {
            await this.Navigator.ShowMessageDialogAsync($"Failed to create subset font file:\n{ex.Message}");
            return;
        }

        var exportIcons = selectedIcons.ConvertAll(si => new ExportIconInfo(si.Group.FinalExportName, si.Icon.Info));

        foreach (var exporter in _exporters)
        {
            try
            {
                await exporter.SaveAsync(ProjectName, exportDir, exportIcons, IconsSource.Variants[0]);
            }
            catch (Exception ex)
            {
                await this.Navigator.ShowMessageDialogAsync($"{exporter.Name} failed during execution:\n{ex.Message}");
                return;
            }
        }

        await this.Navigator.ShowMessageDialogAsync($"Project exported successfully to:\n\n{exportDir.PathDisplay}");
    }

    [RelayCommand]
    private async Task PreviewIconPackAsync()
    {
        var selectedIcons = _iconGroups
            .SelectMany(ig => ig.Icons.Where(i => i.IsSelected).Select(i => (
                Group: ig,
                Icon: i)))
            .Select(i => {
                string variant = i.Icon.Info.Variant == IconsSource.Variants[0] ? null : i.Icon.Info.Variant;

                return new PreviewIconItem(i.Icon.Info.Glyph,
                                i.Icon.Info.RtlGlyph,
                                i.Group.FinalExportName + variant);
            })
            .OrderBy(i => i.Name, StringComparer.InvariantCulture)
            .ToList();

        if (selectedIcons.Count is 0)
        {
            await this.Navigator.ShowMessageDialogAsync("No icons selected for preview.");
            return;
        }

        var model = new PreviewIconPackDialogModel(selectedIcons, IconsSource);
        await this.Navigator.ShowDialogAsync(model);
    }

    [RelayCommand]
    private async Task CloseAsync() => await this.Navigator.NavigateAsync(Routes.StartRoot);

    [RelayCommand]
    private void Exit() => _window.Close();

    [MemberNotNull(nameof(FilteredIconGroups))]
    private void OnFilterChanged(bool variantChanged)
    {
        if (variantChanged)
        {
            string filter = VariantFilter is "All" ? string.Empty : VariantFilter;

            foreach (var iconGroup in _iconGroups)
                iconGroup.SetActiveVariant(filter);
        }

        var filtered = _iconGroups.Where(ig => ig.ActiveIconInfo is not null);

        if (!string.IsNullOrWhiteSpace(NameFilter))
        {
            // Name matches rank above keyword-only matches; OrderBy is stable so groups keep their name order within each rank.

            string[] filterParts = FilterExtensions.SplitFilter(NameFilter);

            filtered = filtered
                .Select(ig => (Group: ig, Rank: ig.GetFilterRank(filterParts)))
                .Where(x => x.Rank > 0)
                .OrderByDescending(x => x.Rank)
                .Select(x => x.Group);
        }

        if (RtlVersionsOnlyFilter)
            filtered = filtered.Where(ig => ig.Info.HasUniqueRtlGlyphs);

        if (IncludedOnlyFilter)
            filtered = filtered.Where(ig => ig.HasSelectedIcons);

        FilteredIconGroups = [.. filtered];

        if (SelectedIconGroup is null || !FilteredIconGroups.Contains(SelectedIconGroup))
            SelectedIconGroup = FilteredIconGroups.FirstOrDefault();
    }

    private async Task LoadProjectAsync(NavigationArgs args)
    {
        if (!ProjectFile.Exists)
        {
            await this.Navigator.ShowMessageDialogAsync($"Project file not found:\n{ProjectFile}");
            await this.Navigator.NavigateAsync(Routes.StartRoot);
            return;
        }

        Project? project;
        byte[] hash;

        try
        {
            (project, hash) = await ReadProjectFileAsync();
        }
        catch (Exception ex)
        {
            await this.Navigator.ShowMessageDialogAsync($"Failed to load project file:\n{ex.Message}");
            args.Redirect = Redirect.Navigate(Routes.StartRoot);
            return;
        }

        if (project is null)
        {
            await this.Navigator.ShowMessageDialogAsync("Project file is invalid.");
            args.Redirect = Redirect.Navigate(Routes.StartRoot);
            return;
        }

        if (project.IconsSourceId != IconsSource.Id)
        {
            await this.Navigator.ShowMessageDialogAsync(
                $"Project requires icon source '{project.IconsSourceId}', but the app has '{IconsSource.Id}'.");
            args.Redirect = Redirect.Navigate(Routes.StartRoot);
            return;
        }

        var warnings = ApplyProject(project);
        _projectFileHash = hash;
        StartWatchingProjectFile();
        await _recentProjects.AddOrUpdateAsync(ProjectFile.PathDisplay, ProjectName);

        if (warnings.Count > 0)
            await this.Navigator.ShowMessageDialogAsync(string.Join("\n", warnings), "Project Load Warnings");
    }

    /// <summary>
    /// Reads and deserializes the project file without holding it open, so other programs (e.g. git) can read and replace it while the editor is
    /// open. Retries briefly if another process is still writing the file.
    /// </summary>
    private async Task<(Project? Project, byte[] Hash)> ReadProjectFileAsync()
    {
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                byte[] bytes = await File.ReadAllBytesAsync(ProjectFile.PathExport);
                var project = JsonSerializer.Deserialize<Project>(new MemoryStream(bytes), JsonOptions);
                return (project, SHA256.HashData(bytes));
            }
            catch (IOException) when (attempt < 5)
            {
                await Task.Delay(100);
            }
        }
    }

    /// <summary>
    /// Resets the editor state and applies the given project to it. Returns warnings to show the user, if any.
    /// </summary>
    private List<string> ApplyProject(Project project)
    {
        var warnings = new List<string>();
        bool hasVersionDowngrade = project.IconsSourceVersion > IconsSource.Version;

        if (hasVersionDowngrade)
        {
            warnings.Add(
                $"Icon Source Downgrade Warning:\n\n" +
                $"Project was built with icon source '{project.IconsSourceId}' version {project.IconsSourceVersion}, " +
                $"but the app only has version {IconsSource.Version}.\n");
        }

        ProjectName = project.Name;

        foreach (var iconGroup in _iconGroups)
        {
            iconGroup.ExportName = string.Empty;

            foreach (var icon in iconGroup.Icons)
                icon.IsSelected = false;
        }

        bool hasMissingIcons = false;

        foreach (var export in project.IconExports)
        {
            if (!_iconGroupsById.TryGetValue(export.GroupId, out var iconGroup))
            {
                warnings.Add($"Icon group '{export.GroupId}' is missing from the current icon source.");
                hasMissingIcons = true;
                continue;
            }

            iconGroup.ExportName = export.ExportName;

            foreach (string variant in export.Variants)
            {
                var icon = iconGroup.Icons.FirstOrDefault(i => i.Info.Variant == variant);

                if (icon is null)
                {
                    warnings.Add($"Icon group '{iconGroup.Info.Name}' is missing variant '{variant}'.");
                    hasMissingIcons = true;
                    continue;
                }

                icon.IsSelected = true;
            }
        }

        if (hasMissingIcons)
        {
            warnings.Add("\nWARNING: Missing icons were not loaded and will be removed if you save the project.");
        }
        else if (hasVersionDowngrade)
        {
            warnings.Add(
                $"No icons were missing despite the version downgrade. " +
                $"Saving the project will update it to use icons source version {IconsSource.Version}.");
        }

        IsDirty = false;
        return warnings;
    }

    private void StartWatchingProjectFile()
    {
        _projectFileWatcher = new FileSystemWatcher(ProjectFile.ParentDirectory.PathExport, Path.GetFileName(ProjectFile.PathExport)) {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName | NotifyFilters.CreationTime,
        };

        // Most programs (including git) write a new file and rename it into place, so renames and creations matter as much as writes.
        _projectFileWatcher.Changed += OnProjectFileChanged;
        _projectFileWatcher.Created += OnProjectFileChanged;
        _projectFileWatcher.Renamed += OnProjectFileChanged;
        _projectFileWatcher.EnableRaisingEvents = true;
    }

    private void OnProjectFileChanged(object sender, FileSystemEventArgs e) => ScheduleExternalChangeCheck(300);

    /// <summary>
    /// Debounces file change notifications (writers typically raise several per save) and moves handling to the UI thread. Called from watcher
    /// and timer threads.
    /// </summary>
    private void ScheduleExternalChangeCheck(double delayMs)
    {
        lock (_reloadTimerLock)
        {
            if (_projectFileWatcher is null)
                return;

            _reloadDebounceTimer?.Dispose();
            _reloadDebounceTimer = new Timer(delayMs) { AutoReset = false };
            _reloadDebounceTimer.Elapsed += (s, e) => this.TaskRunner.Post(() => _ = HandleExternalChangeAsync());
            _reloadDebounceTimer.Start();
        }
    }

    private async Task HandleExternalChangeAsync()
    {
        if (_projectFileWatcher is null)
            return;

        // Only the top dialog can show dialogs, so wait until nothing is showing (including our own reload prompt) before handling the change.
        if (_isHandlingExternalChange || !this.Navigator.CanShowDialog)
        {
            ScheduleExternalChangeCheck(1000);
            return;
        }

        _isHandlingExternalChange = true;

        try
        {
            // Deleted (e.g. by a branch switch): keep the editor state and let the next save recreate the file.
            if (!ProjectFile.Exists)
                return;

            var (project, hash) = await ReadProjectFileAsync();

            if (_projectFileHash is not null && hash.AsSpan().SequenceEqual(_projectFileHash))
                return;

            if (project is null || project.IconsSourceId != IconsSource.Id)
            {
                _projectFileHash = hash;
                await this.Navigator.ShowMessageDialogAsync(
                    "The project file was changed outside the editor but is no longer a valid project for this icon source. " +
                    "The editor state was left unchanged.", "Project Changed On Disk");
                return;
            }

            if (IsDirty)
            {
                int result = await this.Navigator.ShowMessageDialogAsync(
                    "The project file was changed outside the editor. Do you want to reload it and discard your unsaved changes?",
                    "Project Changed On Disk", ["Reload", "Keep My Changes"]);

                if (result is 1)
                {
                    // Remember the on-disk content so the same change is not prompted for again. Saving will overwrite it.
                    _projectFileHash = hash;
                    return;
                }
            }

            var warnings = ApplyProject(project);
            _projectFileHash = hash;

            if (warnings.Count > 0)
                await this.Navigator.ShowMessageDialogAsync(string.Join("\n", warnings), "Project Reload Warnings");
        }
        catch (Exception ex)
        {
            await this.Navigator.ShowMessageDialogAsync($"Failed to reload project file:\n{ex.Message}");
        }
        finally
        {
            _isHandlingExternalChange = false;
        }
    }

    private async Task<bool> SaveProjectInternalAsync()
    {
        try
        {
            var exports = new List<IconExport>();

            foreach (var iconGroup in _iconGroups)
            {
                var selectedVariants = iconGroup.Icons
                    .Where(i => i.IsSelected)
                    .Select(i => i.Info.Variant)
                    .ToList();

                if (selectedVariants.Count > 0)
                    exports.Add(new IconExport(iconGroup.Info.Id, iconGroup.SaveExportName, selectedVariants));
            }

            var project = new Project {
                Name = ProjectName,
                IconsSourceId = IconsSource.Id,
                IconsSourceVersion = IconsSource.Version,
                IconExports = exports,
            };

            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(project, JsonOptions);

            // Write to a temporary file and move it into place so the project file is never observed half-written. The hash is recorded first so
            // the watcher recognizes the resulting change as our own.
            _projectFileHash = SHA256.HashData(bytes);

            string tempPath = ProjectFile.PathExport + ".tmp";
            await File.WriteAllBytesAsync(tempPath, bytes);
            File.Move(tempPath, ProjectFile.PathExport, overwrite: true);

            IsDirty = false;
            return true;
        }
        catch (Exception ex)
        {
            await this.Navigator.ShowMessageDialogAsync($"Failed to save project:\n{ex.Message}");
            return false;
        }
    }
}
