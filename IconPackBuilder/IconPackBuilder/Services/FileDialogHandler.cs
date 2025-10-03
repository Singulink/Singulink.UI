using IconPackBuilder.Core.Services;
using Singulink.IO;
using Windows.Storage.Pickers;

namespace IconPackBuilder.Services;

public class FileDialogHandler : IFileDialogHandler
{
    private readonly Window _window;

    private nint WindowHandle => WinRT.Interop.WindowNative.GetWindowHandle(_window);

    public FileDialogHandler(Window window)
    {
        _window = window;
    }

    public async Task<IAbsoluteFilePath?> ShowOpenFileDialogAsync(IEnumerable<string> filters)
    {
        var openFilePicker = new FileOpenPicker {
            ViewMode = PickerViewMode.List,
        };

        openFilePicker.FileTypeFilter.AddRange(filters);
        WinRT.Interop.InitializeWithWindow.Initialize(openFilePicker, WindowHandle);

        var result = await openFilePicker.PickSingleFileAsync();
        return result is null ? null : FilePath.ParseAbsolute(result.Path, PathOptions.None);
    }

    public async Task<IAbsoluteFilePath?> ShowSaveFileDialogAsync(IEnumerable<string> filters, string defaultFileName)
    {
        var saveFilePicker = new FileSavePicker {
            SuggestedFileName = defaultFileName,
        };

        saveFilePicker.FileTypeChoices.Add("Supported Files", [.. filters]);

        WinRT.Interop.InitializeWithWindow.Initialize(saveFilePicker, WindowHandle);

        var result = await saveFilePicker.PickSaveFileAsync();
        return result is null ? null : FilePath.ParseAbsolute(result.Path, PathOptions.NoUnfriendlyNames);
    }
}
