using Singulink.IO;

namespace IconPackBuilder.Core.Services;

public interface IFontSubsetter
{
    public Task SaveAsync(IAbsoluteFilePath sourcePath, IAbsoluteFilePath destinationPath, IEnumerable<int> codePoints);
}
