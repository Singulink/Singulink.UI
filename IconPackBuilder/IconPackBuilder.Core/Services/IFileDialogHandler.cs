using Singulink.IO;

namespace IconPackBuilder.Core.Services;

public interface IFileDialogHandler
{
    public Task<IAbsoluteFilePath?> ShowOpenFileDialogAsync(IEnumerable<string> filters);

    public Task<IAbsoluteFilePath?> ShowSaveFileDialogAsync(IEnumerable<string> filters, string defaultFileName);
}
