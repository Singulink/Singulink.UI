using Singulink.IO;

namespace IconPackBuilder.Core.Services;

public interface IExporter
{
    public string Name { get; }

    public Task SaveAsync(string projectName, IAbsoluteDirectoryPath exportDir, IEnumerable<ExportIconInfo> icons, string defaultVariantName);
}
