using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
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

public partial class EditorRootModel : ObservableObject, IRoutedViewModel<string>, IRoutedViewModelBase
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly IReadOnlyList<IconGroupModel> _iconGroups;
    private readonly FrozenDictionary<string, IconGroupModel> _iconGroupsById;
    private readonly IWindow _window;
    private readonly IFontSubsetter _fontSubsetter;
    private readonly IReadOnlyList<IExporter> _exporters;

    private FileStream? _projectStream;
    private Timer? _nameFilterDebounceTimer;

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

    public EditorRootModel(IWindow window, IconsSource iconsSource, IFontSubsetter fontSubsetter, IEnumerable<IExporter> exporters)
    {
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

    public async Task OnNavigatedAwayAsync()
    {
        if (_projectStream is not null)
        {
            await _projectStream.DisposeAsync();
            _projectStream = null;
        }

        _nameFilterDebounceTimer?.Stop();
        _nameFilterDebounceTimer?.Dispose();
        _nameFilterDebounceTimer = null;
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
            filtered = filtered.Filter(NameFilter, ig => {
                if (string.IsNullOrWhiteSpace(ig.ExportName))
                    return ig.Info.Name;

                return $"{ig.Info.Name} {ig.ExportName}";
            });
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

        try
        {
            _projectStream = ProjectFile.OpenAsyncStream(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            project = await JsonSerializer.DeserializeAsync<Project>(_projectStream, JsonOptions);
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

        bool hasMissingIcons = false;

        // Apply icon exports

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
        else if (hasVersionDowngrade && !hasMissingIcons)
        {
            warnings.Add(
                $"No icons were missing despite the version downgrade. " +
                $"Saving the project will update it to use icons source version {IconsSource.Version}.");
        }

        IsDirty = false;

        if (warnings.Count > 0)
        {
            string message = string.Join("\n", warnings);
            await this.Navigator.ShowMessageDialogAsync(message, "Project Load Warnings");
        }
    }

    private async Task<bool> SaveProjectInternalAsync()
    {
        if (_projectStream is null)
        {
            await this.Navigator.ShowMessageDialogAsync("Project file stream is not available.");
            return false;
        }

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

            _projectStream.SetLength(0);
            _projectStream.Position = 0;

            await JsonSerializer.SerializeAsync(_projectStream, project, JsonOptions);
            await _projectStream.FlushAsync();

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
