using Singulink.IO;

namespace IconPackBuilder.Core.Services;

public interface IFontSubsetter
{
    public Task SaveAsync(IAbsoluteFilePath sourceFile, IAbsoluteFilePath destinationFile, IEnumerable<int> codePoints);
}
