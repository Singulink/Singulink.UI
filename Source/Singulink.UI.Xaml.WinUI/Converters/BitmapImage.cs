using System.Diagnostics;
using SysBitmapImage = Microsoft.UI.Xaml.Media.Imaging.BitmapImage;

namespace Singulink.UI.Xaml.Converters;

/// <summary>
/// Provides conversion methods to <see cref="SysBitmapImage"/> for use in XAML bindings.
/// </summary>
public static class BitmapImage
{
    /// <summary>
    /// Creates a new <see cref="SysBitmapImage"/> from the specified <see cref="System.Uri"/>.
    /// </summary>
    public static SysBitmapImage? FromUri(System.Uri? uri)
    {
        if (uri is null)
            return null;

        try
        {
            return new SysBitmapImage(uri);
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"[Singulink.UI.Xaml.WinUI] Failed to create BitmapImage from URI '{uri}': " + ex);
            return null;
        }
    }

    /// <summary>
    /// Creates a new <see cref="SysBitmapImage"/> from the specified URI string.
    /// </summary>
    public static SysBitmapImage? FromUriString(string? uriString) => FromUri(Uri.FromString(uriString));
}
