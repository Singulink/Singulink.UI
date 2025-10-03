using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IconPackBuilder.Core;
using IconPackBuilder.Core.Services;
using IconPackBuilder.Data;
using Singulink.UI.Navigation;

namespace IconPackBuilder.ViewModels;

public partial class StartRootModel(IconsSource iconsSource, IFileDialogHandler fileDialogHandler) : ObservableObject, IRoutedViewModel
{
    private static readonly string[] ProjectFileFilters = [".ipbproj"];
    private static readonly JsonSerializerOptions ProjectJsonOptions = new() { WriteIndented = true };

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateProjectCommand))]
    public partial string NewProjectName { get; set; } = string.Empty;

    private bool CanCreateProject => IsValidProjectName(NewProjectName);

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
            await JsonSerializer.SerializeAsync(stream, project, ProjectJsonOptions);

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
