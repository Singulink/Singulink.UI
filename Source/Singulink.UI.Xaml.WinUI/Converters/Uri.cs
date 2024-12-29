using System.Diagnostics;
using SysUri = System.Uri;

namespace Singulink.UI.Xaml.Converters;

/// <summary>
/// Provides conversion methods to <see cref="Uri"/> for use in XAML bindings.
/// </summary>
public static class Uri
{
    /// <summary>
    /// Converts the specified string to a URI.
    /// </summary>
    public static SysUri? FromString(string? uriString)
    {
        if (string.IsNullOrWhiteSpace(uriString))
            return null;

        try
        {
            return new SysUri(uriString.Trim());
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"[Singulink.UI.Xaml.WinUI] Failed to convert string '{uriString}' to URI: " + ex);
            return null;
        }
    }

    /// <summary>
    /// Converts the specified phone number string to a phone URI.
    /// </summary>
    public static SysUri? Phone(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return null;

        try
        {
            return new SysUri("tel:" + phoneNumber.Trim());
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"[Singulink.UI.Xaml.WinUI] Failed to convert phone number string '{phoneNumber}' to URI: " + ex);
            return null;
        }
    }

    /// <summary>
    /// Converts the specified email string to an email URI.
    /// </summary>
    public static SysUri? Email(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        try
        {
            return new SysUri("mailto:" + email.Trim());
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"[Singulink.UI.Xaml.WinUI] Failed to convert email string '{email}' to URI: " + ex);
            return null;
        }
    }

    /// <summary>
    /// Converts the specified website string to a website URI. If the string does not start with "http://" or "https://" then "https://" is prepended.
    /// </summary>
    public static SysUri? Website(string? website)
    {
        if (string.IsNullOrWhiteSpace(website))
            return null;

        website = website.Trim();

        if (!website.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !website.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            website = "https://" + website;

        try
        {
            return new SysUri(website);
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"[Singulink.UI.Xaml.WinUI] Failed to convert website string '{website}' to URI: " + ex);
            return null;
        }
    }
}
