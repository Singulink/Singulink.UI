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
using Singulink.UI.Navigation;
using Singulink.UI.Tasks;
using Timer = System.Timers.Timer;

namespace IconPackBuilder.ViewModels;

public partial class EditorRootModel : ObservableObject, IRoutedViewModel<string>, IRoutedViewModelBase, IDisposable
{
    private readonly IReadOnlyList<IconGroupModel> _iconGroups;
    private readonly FrozenDictionary<string, IconGroupModel> _iconGroupsById;
    private readonly IWindow _window;
    private readonly IExportService _exportService;
    private readonly IRecentProjectsStore _recentProjects;
    private readonly IHostInfo _hostInfo;

    private readonly Lock _timerLock = new();

    private Timer? _nameFilterDebounceTimer;
    private Timer? _reloadDebounceTimer;
    private Timer? _hostSaveDebounceTimer;
    private bool _isWatchingDocument;
    private bool _isApplyingProject;
    private byte[]? _documentHash;
    private bool _isHandlingExternalChange;

    public bool CanBeCached => false;

    /// <summary>
    /// Gets the project document being edited.
    /// </summary>
    public IProjectDocument Document { get; }

    /// <summary>
    /// Gets the full path of the project document.
    /// </summary>
    public string ProjectPath => Document.Path;

    /// <summary>
    /// Gets a value indicating whether the Save, Close and Exit commands apply. They do not when the host (e.g. Visual Studio Code) owns the
    /// document's persistence, in which case every change is pushed to the host immediately.
    /// </summary>
    public bool ShowFileCommands => !Document.HostOwnsPersistence;

    /// <summary>
    /// Gets a value indicating whether the Exit command applies: only where the host can actually be exited (not in a browser tab).
    /// </summary>
    public bool ShowExitCommand => ShowFileCommands && _hostInfo.CanExit;

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

    partial void OnIsDirtyChanged(bool value)
    {
        // When the host owns persistence there is no Save command: changes are coalesced briefly and pushed to the host, which tracks dirty
        // state and undo itself. Applying a project (load/reload) mutates state whose setters mark the editor dirty; that must not schedule a
        // save or the file would be rewritten on open and stay perpetually modified.
        if (value && Document.HostOwnsPersistence && !_isApplyingProject)
            ScheduleHostSave();
    }

    /// <summary>
    /// Gets or sets a value indicating whether an export writes the C# class. The enabled formats are saved with the project so that scripted
    /// exports produce the same output as the editor.
    /// </summary>
    [ObservableProperty]
    public partial bool ExportCSharp { get; set; } = true;

    partial void OnExportCSharpChanged(bool value) => IsDirty = true;

    /// <summary>
    /// Gets or sets a value indicating whether an export writes the CSS stylesheet.
    /// </summary>
    [ObservableProperty]
    public partial bool ExportCss { get; set; } = true;

    partial void OnExportCssChanged(bool value) => IsDirty = true;

    /// <summary>
    /// Gets or sets a value indicating whether an export writes the JavaScript module and its TypeScript declarations.
    /// </summary>
    [ObservableProperty]
    public partial bool ExportJavaScript { get; set; } = true;

    partial void OnExportJavaScriptChanged(bool value) => IsDirty = true;

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
        IWindow window, IconsSource iconsSource, IExportService exportService, IRecentProjectsStore recentProjects, IProjectDocumentFactory documents,
        IHostInfo hostInfo)
    {
        _recentProjects = recentProjects;
        _hostInfo = hostInfo;
        _window = window;
        _exportService = exportService;
        IconsSource = iconsSource;

        Document = documents.Open(this.Parameter);

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
        if (IsDirty && !Document.HostOwnsPersistence)
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
    /// Releases the project document (and its change watcher) and timers. Called by the navigator when the view model is discarded after
    /// navigating away.
    /// </summary>
    public void Dispose()
    {
        lock (_timerLock)
        {
            _isWatchingDocument = false;
            _reloadDebounceTimer?.Dispose();
            _reloadDebounceTimer = null;
            _hostSaveDebounceTimer?.Dispose();
            _hostSaveDebounceTimer = null;
        }

        Document.Changed -= OnDocumentChanged;
        Document.Dispose();

        _nameFilterDebounceTimer?.Dispose();
        _nameFilterDebounceTimer = null;

        GC.SuppressFinalize(this);
    }

    [RelayCommand]
    private async Task SaveProjectAsync() => await SaveProjectInternalAsync();

    [RelayCommand]
    private async Task ExportProjectAsync()
    {
        if (IsDirty && !Document.HostOwnsPersistence)
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

        var selectedIcons = _iconGroups
            .SelectMany(ig => ig.Icons.Where(i => i.IsSelected).Select(i => (Group: ig, Icon: i)))
            .ToList();

        var exportIcons = selectedIcons.ConvertAll(si => new ExportIconInfo(si.Group.FinalExportName, si.Icon.Info));
        string exportDir;

        try
        {
            exportDir = await _exportService.ExportAsync(new ExportRequest(ProjectName, Document, IconsSource, exportIcons, GetExportFormats()));
        }
        catch (Exception ex)
        {
            await this.Navigator.ShowMessageDialogAsync(ex.Message, "Export Failed");
            return;
        }

        await this.Navigator.ShowMessageDialogAsync($"Project exported successfully to:\n\n{exportDir}");
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
        Project? project;
        byte[] hash;

        try
        {
            byte[]? bytes = await Document.ReadAsync();

            if (bytes is null)
            {
                await this.Navigator.ShowMessageDialogAsync($"Project file not found:\n{ProjectPath}");
                await this.Navigator.NavigateAsync(Routes.StartRoot);
                return;
            }

            (project, hash) = ParseProject(bytes);
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
        _documentHash = hash;
        StartWatchingDocument();

        if (!Document.HostOwnsPersistence)
            await _recentProjects.AddOrUpdateAsync(ProjectPath, ProjectName);

        if (warnings.Count > 0)
            await this.Navigator.ShowMessageDialogAsync(string.Join("\n", warnings), "Project Load Warnings");
    }

    private List<ExportFormat> GetExportFormats()
    {
        var formats = new List<ExportFormat>();

        if (ExportCSharp)
            formats.Add(ExportFormat.CSharp);

        if (ExportCss)
            formats.Add(ExportFormat.Css);

        if (ExportJavaScript)
            formats.Add(ExportFormat.JavaScript);

        return formats;
    }

    private static (Project? Project, byte[] Hash) ParseProject(byte[] bytes)
    {
        var project = JsonSerializer.Deserialize(bytes, ProjectJsonContext.Default.Project);
        return (project, SHA256.HashData(bytes));
    }

    /// <summary>
    /// Resets the editor state and applies the given project to it. Returns warnings to show the user, if any.
    /// </summary>
    private List<string> ApplyProject(Project project)
    {
        // Suppress the dirty-driven host save while applying, and drop any pending one, so opening a project never writes it back.
        lock (_timerLock)
        {
            _hostSaveDebounceTimer?.Dispose();
            _hostSaveDebounceTimer = null;
        }

        _isApplyingProject = true;

        try
        {
            return ApplyProjectCore(project);
        }
        finally
        {
            _isApplyingProject = false;
        }
    }

    private List<string> ApplyProjectCore(Project project)
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

        var formats = project.GetExportFormats();
        ExportCSharp = formats.Contains(ExportFormat.CSharp);
        ExportCss = formats.Contains(ExportFormat.Css);
        ExportJavaScript = formats.Contains(ExportFormat.JavaScript);

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

    private void StartWatchingDocument()
    {
        lock (_timerLock)
            _isWatchingDocument = true;

        Document.Changed += OnDocumentChanged;
    }

    /// <summary>
    /// Called by the document (on an arbitrary thread) when its content changed outside the editor.
    /// </summary>
    private void OnDocumentChanged(object? sender, EventArgs e) => this.TaskRunner.Post(() => _ = HandleExternalChangeAsync());

    /// <summary>
    /// Re-checks the document for external changes after a delay. Called from UI and timer threads.
    /// </summary>
    private void ScheduleExternalChangeCheck(double delayMs)
    {
        lock (_timerLock)
        {
            if (!_isWatchingDocument)
                return;

            _reloadDebounceTimer?.Dispose();
            _reloadDebounceTimer = new Timer(delayMs) { AutoReset = false };
            _reloadDebounceTimer.Elapsed += (s, e) => this.TaskRunner.Post(() => _ = HandleExternalChangeAsync());
            _reloadDebounceTimer.Start();
        }
    }

    private void ScheduleHostSave()
    {
        lock (_timerLock)
        {
            _hostSaveDebounceTimer?.Dispose();
            _hostSaveDebounceTimer = new Timer(250) { AutoReset = false };
            _hostSaveDebounceTimer.Elapsed += (s, e) => this.TaskRunner.Post(() => _ = SaveProjectInternalAsync());
            _hostSaveDebounceTimer.Start();
        }
    }

    private async Task HandleExternalChangeAsync()
    {
        if (!_isWatchingDocument)
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
            byte[]? bytes = await Document.ReadAsync();

            // Deleted (e.g. by a branch switch): keep the editor state and let the next save recreate the file.
            if (bytes is null)
                return;

            var (project, hash) = ParseProject(bytes);

            if (_documentHash is not null && hash.AsSpan().SequenceEqual(_documentHash))
                return;

            if (project is null || project.IconsSourceId != IconsSource.Id)
            {
                _documentHash = hash;
                await this.Navigator.ShowMessageDialogAsync(
                    "The project file was changed outside the editor but is no longer a valid project for this icon source. " +
                    "The editor state was left unchanged.", "Project Changed On Disk");
                return;
            }

            // A host that owns persistence has already reconciled the change (undo, text edit, reload), so it is applied without asking.
            if (IsDirty && !Document.HostOwnsPersistence)
            {
                int result = await this.Navigator.ShowMessageDialogAsync(
                    "The project file was changed outside the editor. Do you want to reload it and discard your unsaved changes?",
                    "Project Changed On Disk", ["Reload", "Keep My Changes"]);

                if (result is 1)
                {
                    // Remember the on-disk content so the same change is not prompted for again. Saving will overwrite it.
                    _documentHash = hash;
                    return;
                }
            }

            var warnings = ApplyProject(project);
            _documentHash = hash;

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
                ExportFormats = [.. GetExportFormats()],
                IconExports = exports,
            };

            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(project, ProjectJsonContext.Default.Project);

            // The hash is recorded before writing so the document's change notification for our own write is recognized and ignored.
            _documentHash = SHA256.HashData(bytes);
            await Document.WriteAsync(bytes);

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
