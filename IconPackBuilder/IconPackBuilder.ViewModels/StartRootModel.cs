using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IconPackBuilder.Core;
using IconPackBuilder.Core.Services;
using IconPackBuilder.Data;
using Singulink.UI.Navigation;

namespace IconPackBuilder.ViewModels;

public partial class StartRootModel(IconsSource iconsSource, IFileDialogHandler fileDialogHandler, IRecentProjectsStore recentProjects, IHostInfo hostInfo)
    : ObservableObject, IRoutedViewModel
{
    private static readonly string[] ProjectFileFilters = [".ipproj"];

    /// <summary>
    /// Gets the host's note for the start screen (how projects and exports work here), or <see langword="null"/> if there is none.
    /// </summary>
    public string? HostNote => hostInfo.StartPageNote;

    public bool HasHostNote => hostInfo.StartPageNote is not null;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateProjectCommand))]
    public partial string NewProjectName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<RecentProjectModel> RecentProjects { get; private set; } = [];

    [ObservableProperty]
    public partial bool HasRecentProjects { get; private set; }

    private bool CanCreateProject => IsValidProjectName(NewProjectName);

    public Task OnNavigatedToAsync(NavigationArgs args)
    {
        RefreshRecentProjects();
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(CanCreateProject))]
    private async Task CreateProjectAsync()
    {
        string trimmedName = NewProjectName.Trim();
        string defaultFileName = trimmedName + ProjectFileFilters[0];
        var filePath = await fileDialogHandler.ShowSaveFileDialogAsync(ProjectFileFilters, defaultFileName);

        if (filePath is null)
            return;

        var project = new Project {
            Name = trimmedName,
            IconsSourceId = iconsSource.Id,
            IconsSourceVersion = iconsSource.Version,
        };

        await using (var stream = filePath.OpenAsyncStream(FileMode.Create, FileAccess.Write, FileShare.None))
            await JsonSerializer.SerializeAsync(stream, project, ProjectJsonContext.Default.Project);

        await this.Navigator.NavigateAsync(Routes.EditorRoot.ToConcrete(filePath.PathDisplay));
    }

    [RelayCommand]
    private async Task OpenProjectAsync()
    {
        var filePath = await fileDialogHandler.ShowOpenFileDialogAsync(ProjectFileFilters);

        if (filePath is null)
            return;

        await this.Navigator.NavigateAsync(Routes.EditorRoot.ToConcrete(filePath.PathDisplay));
    }

    [RelayCommand]
    private async Task OpenRecentProjectAsync(RecentProjectModel project)
    {
        if (!File.Exists(project.Path))
        {
            int result = await this.Navigator.ShowMessageDialogAsync(
                $"The project file no longer exists:\n{project.Path}\n\nDo you want to remove it from the recent projects list?",
                "Project Not Found",
                ["Remove", "Keep"]);

            if (result is 0)
                await RemoveRecentProjectAsync(project);
            else
                RefreshRecentProjects();

            return;
        }

        await this.Navigator.NavigateAsync(Routes.EditorRoot.ToConcrete(project.Path));
    }

    [RelayCommand]
    private async Task RemoveRecentProjectAsync(RecentProjectModel project)
    {
        await recentProjects.RemoveAsync(project.Path);
        RefreshRecentProjects();
    }

    [RelayCommand]
    private async Task ClearRecentProjectsAsync()
    {
        int result = await this.Navigator.ShowMessageDialogAsync(
            "Do you want to clear the recent projects list?", "Clear Recent Projects", ["Clear", "Cancel"]);

        if (result is 0)
        {
            await recentProjects.ClearAsync();
            RefreshRecentProjects();
        }
    }

    private void RefreshRecentProjects()
    {
        RecentProjects = [.. recentProjects.Projects.Select(p => new RecentProjectModel(p))];
        HasRecentProjects = RecentProjects.Count > 0;
    }

    private static bool IsValidProjectName(string? projectName)
    {
        if (string.IsNullOrWhiteSpace(projectName))
            return false;

        string trimmedName = projectName.Trim();
        string[] segments = trimmedName.Split('.');

        if (segments.Length < 2)
            return false;

        return segments.All(IsValidIdentifier);
    }

    private static bool IsValidIdentifier(string identifier)
    {
        if (identifier.Length is 0)
            return false;

        if (!char.IsAsciiLetter(identifier[0]))
            return false;

        for (int i = 1; i < identifier.Length; i++)
        {
            char c = identifier[i];

            if (!char.IsAsciiLetterOrDigit(c) && c != '_')
                return false;
        }

        return true;
    }
}
