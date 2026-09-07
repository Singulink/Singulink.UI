using System.Runtime.Versioning;
using IconPackBuilder.Core.IconSources;
using Singulink.IO;
using Windows.Storage;

namespace IconPackBuilder;

/// <summary>
/// Reads the icon source assets from the app package. The WebAssembly head cannot use <see cref="File"/> APIs on the app directory, so the
/// assets are fetched through the package URI scheme instead.
/// </summary>
[SupportedOSPlatform("browser")]
internal static class WasmAssets
{
    public static async Task<SeagullIconsSource> LoadIconsSourceAsync()
    {
        using var stream = await OpenReadAsync(SeagullIconsSource.DataFilePath);
        return SeagullIconsSource.FromStream(stream);
    }

    public static async Task<byte[]> ReadAllBytesAsync(IRelativeFilePath packageFile)
    {
        using var stream = await OpenReadAsync(packageFile);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        return buffer.ToArray();
    }

    private static async Task<Stream> OpenReadAsync(IRelativeFilePath packageFile)
    {
        var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///" + packageFile.PathDisplay));
        return (await file.OpenReadAsync()).AsStreamForRead();
    }
}
