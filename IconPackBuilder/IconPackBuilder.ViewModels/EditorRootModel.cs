using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IconPackBuilder.Core;
using IconPackBuilder.Core.Services;
using IconPackBuilder.Data;
using IconPackBuilder.ViewModels.Utilities;
using Singulink.IO;
using Singulink.UI.Navigation;

namespace IconPackBuilder.ViewModels;

public partial class EditorRootModel : ObservableObject, IRoutedViewModel<string>, IRoutedViewModelBase
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly IReadOnlyList<IconGroupModel> _iconGroups;
    private readonly IFontSubsetter _fontSubsetter;
    private readonly IReadOnlyList<IExporter> _exporters;
    private FileStream? _projectStream;

    public IAbsoluteFilePath ProjectFile { get; }

    [ObservableProperty]
    public partial string ProjectName { get; private set; } = string.Empty;

    public IconsSource IconsSource { get; }

    [ObservableProperty]
    public partial string NameFilter { get; set; } = string.Empty;

    partial void OnNameFilterChanged(string value) => OnFilterChanged(false);

    [ObservableProperty]
    [MemberNotNull(nameof(FilteredIconGroups))]
    public partial string VariantFilter { get; set; }

    partial void OnVariantFilterChanged(string value) => OnFilterChanged(true);

    [ObservableProperty]
    public partial bool WithRtlVersionsOnlyFilter { get; set; }

    partial void OnWithRtlVersionsOnlyFilterChanged(bool value) => OnFilterChanged(false);

    [ObservableProperty]
    public partial bool IsDirty { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<IconGroupModel> FilteredIconGroups { get; set; }

    [ObservableProperty]
    public partial IconGroupModel? SelectedIconGroup { get; set; }

    public EditorRootModel(IconsSource iconsSource, IFontSubsetter fontSubsetter, IEnumerable<IExporter> exporters)
    {
        IconsSource = iconsSource;
        _fontSubsetter = fontSubsetter;
        _exporters = [.. exporters];

        ProjectFile = FilePath.ParseAbsolute(this.Parameter, PathOptions.None);

        _iconGroups = [.. iconsSource
            .LoadIconGroups()
            .Select(ig => new IconGroupModel(this, ig))
            .OrderBy(ig => ig.Info.Name, StringComparer.InvariantCulture)
        ];

        VariantFilter = iconsSource.Variants[0];
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
    }

    [RelayCommand]
    public async Task SaveProjectAsync()
    {
        await SaveProjectInternalAsync();
    }

    [RelayCommand]
    public async Task ExportProjectAsync()
    {
        var exportDir = ProjectFile.ParentDirectory.CombineDirectory(ProjectName + "_Export", PathOptions.None);
        var fontFile = DirectoryPath.GetAppBase() + IconsSource.FontFile;
        var subsetFontFile = exportDir.CombineFile(ProjectName + IconsSource.FontFile.Extension, PathOptions.None);

        var selectedIcons = _iconGroups
            .SelectMany(ig => ig.Icons.Where(i => i.IsSelected).Select(i => (Group: ig, Icon: i)))
            .ToList();

        var allCodePoints = new List<int>();

        foreach (var icon in selectedIcons)
        {
            allCodePoints.Add(icon.Icon.Info.CodePoint);

            if (icon.Icon.Info.RtlCodePoint.HasValue)
                allCodePoints.Add(icon.Icon.Info.RtlCodePoint.Value);
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

        var exportIcons = selectedIcons.ConvertAll(si => new ExportIconInfo(si.Group.ExportName, si.Icon.Info));

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
    }

    [RelayCommand]
    public async Task CloseAsync() => await this.Navigator.NavigateAsync(Routes.StartRoot);

    [MemberNotNull(nameof(FilteredIconGroups))]
    private void OnFilterChanged(bool variantChanged)
    {
        if (variantChanged)
        {
            foreach (var iconGroup in _iconGroups)
                iconGroup.SetActiveVariant(VariantFilter);
        }

        var filtered = _iconGroups.Where(ig => ig.ActiveIconInfo is not null);

        if (!string.IsNullOrWhiteSpace(NameFilter))
            filtered = filtered.Filter(NameFilter, ig => ig.Info.Name);

        if (WithRtlVersionsOnlyFilter)
            filtered = filtered.Where(ig => ig.Info.HasUniqueRtlGlyphs);

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

        if (project.IconsSourceVersion > IconsSource.Version)
        {
            await this.Navigator.ShowMessageDialogAsync(
                $"Project requires icon source version {project.IconsSourceVersion}, but the app has {IconsSource.Version}.");
            args.Redirect = Redirect.Navigate(Routes.StartRoot);
            return;
        }

        ProjectName = project.Name;

        // Apply icon exports

        foreach (var iconGroup in _iconGroups)
        {
            var export = project.IconExports.FirstOrDefault(e => e.GroupId == iconGroup.Info.Id);
            iconGroup.ExportName = export?.ExportName ?? iconGroup.Info.Id;

            foreach (var icon in iconGroup.Icons)
                icon.IsSelected = false;

            if (export is not null)
            {
                foreach (string variant in export.Variants)
                {
                    var icon = iconGroup.Icons.FirstOrDefault(i => i.Info.Variant == variant);
                    icon?.IsSelected = true;
                }
            }
        }

        IsDirty = false;
        _projectStream.Position = 0;
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
                    exports.Add(new IconExport(iconGroup.Info.Id, iconGroup.ExportName, selectedVariants));
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
