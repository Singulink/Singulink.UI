using System.Diagnostics;

namespace Singulink.UI.Xaml.Converters;

/// <summary>
/// Provides conversion methods for use in XAML bindings.
/// </summary>
public static class Convert
{
    #region Conversions to Boolean

    /// <summary>
    /// Negates the specified boolean value.
    /// </summary>
    public static bool Negate(bool value) => !value;

    /// <summary>
    /// Returns <see langword="true"/> if the specified value is <see langword="null"/>; otherwise <see langword="false"/>.
    /// </summary>
    public static bool NullToTrue(object? value) => value is null;

    /// <summary>
    /// Returns <see langword="true"/> if the specified value is <see langword="null"/>; otherwise <see langword="false"/>.
    /// </summary>
    public static bool NullToTrue<T>(T value) => value is null;

    /// <summary>
    /// Returns <see langword="false"/> if the specified value is <see langword="null"/>; otherwise <see langword="true"/>.
    /// </summary>
    public static bool NullToFalse(object? value) => value is not null;

    /// <summary>
    /// Returns <see langword="false"/> if the specified value is <see langword="null"/>; otherwise <see langword="true"/>.
    /// </summary>
    public static bool NullToFalse<T>(T value) => value is not null;

    /// <summary>
    /// Returns <see langword="true"/> if the specified value is <see langword="null"/> or an empty string; otherwise to <see langword="false"/>.
    /// </summary>
    public static bool NullOrEmptyToTrue(string? value) => string.IsNullOrEmpty(value);

    /// <summary>
    /// Returns <see langword="false"/> if the specified value is <see langword="null"/> or an empty string; otherwise to <see langword="true"/>.
    /// </summary>
    public static bool NullOrEmptyToFalse(string? value) => !string.IsNullOrEmpty(value);

    /// <summary>
    /// Returns <see langword="true"/> if the specified value is <see langword="null"/>, empty, or consists only of white-space characters; otherwise to <see
    /// langword="false"/>.
    /// </summary>
    public static bool NullOrWhiteSpaceToTrue(string? value) => string.IsNullOrWhiteSpace(value);

    /// <summary>
    /// Returns <see langword="false"/> if the specified value is <see langword="null"/>, empty, or consists only of white-space characters; otherwise to <see
    /// langword="true"/>.
    /// </summary>
    public static bool NullOrWhiteSpaceToFalse(string? value) => !string.IsNullOrWhiteSpace(value);

    /// <summary>
    /// Returns <see langword="true"/> if the specified value is <see langword="null"/>; otherwise <see langword="false"/>.
    /// </summary>
    public static bool DefaultToTrue(object? value) => value is null;

    /// <summary>
    /// Returns <see langword="true"/> if the specified value is equal to the default value of <typeparamref name="T"/>; otherwise <see langword="false"/>.
    /// </summary>
    public static bool DefaultToTrue<T>(T value) => EqualityComparer<T>.Default.Equals(value, default);

    /// <summary>
    /// Returns <see langword="false"/> if the specified value is <see langword="null"/>; otherwise <see langword="true"/>.
    /// </summary>
    public static bool DefaultToFalse(object? value) => value is not null;

    /// <summary>
    /// Returns <see langword="false"/> if the specified value is equal to the default value of <typeparamref name="T"/>; otherwise <see langword="true"/>.
    /// </summary>
    public static bool DefaultToFalse<T>(T value) => !EqualityComparer<T>.Default.Equals(value, default);

    #endregion

    #region Conversions to Visibility

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value is <see langword="true"/>; otherwise <see cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility TrueToVisible(bool? value) => value is true ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value is <see langword="true"/>; otherwise <see cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility TrueToVisible(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value is <see langword="false"/>; otherwise <see cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility FalseToVisible(bool? value) => value is false ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value is <see langword="false"/>; otherwise <see cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility FalseToVisible(bool value) => !value ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value is <see langword="null"/>; otherwise <see cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility NullToVisible(object? value) => value is null ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value is <see langword="null"/>; otherwise <see cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility NullToVisible<T>(T value) => value is null ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Collapsed"/> if the specified value is <see langword="null"/>; otherwise <see cref="Visibility.Visible"/>.
    /// </summary>
    public static Visibility NullToCollapsed(object? value) => value is null ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>
    /// Returns <see cref="Visibility.Collapsed"/> if the specified value is <see langword="null"/>; otherwise <see cref="Visibility.Visible"/>.
    /// </summary>
    public static Visibility NullToCollapsed<T>(T value) => value is null ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value is <see langword="null"/> or an empty string; otherwise <see
    /// cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility NullOrEmptyToVisible(string? value) => string.IsNullOrEmpty(value) ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Collapsed"/> if the specified value is <see langword="null"/> or an empty string; otherwise <see
    /// cref="Visibility.Visible"/>.
    /// </summary>
    public static Visibility NullOrEmptyToCollapsed(string? value) => string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value is <see langword="null"/>, empty, or consists only of white-space characters; otherwise
    /// <see cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility NullOrWhiteSpaceToVisible(string? value) => string.IsNullOrWhiteSpace(value) ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Collapsed"/> if the specified value is <see langword="null"/>, empty, or consists only of white-space characters; otherwise
    /// <see cref="Visibility.Visible"/>.
    /// </summary>
    public static Visibility NullOrWhiteSpaceToCollapsed(string? value) => string.IsNullOrWhiteSpace(value) ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value is <see langword="null"/>; otherwise <see cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility DefaultToVisible(object? value) => value is null ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value is equal to the default value of <typeparamref name="T"/>; otherwise <see
    /// cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility DefaultToVisible<T>(T value) => EqualityComparer<T>.Default.Equals(value, default) ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Collapsed"/> if the specified value is <see langword="null"/>; otherwise <see cref="Visibility.Visible"/>.
    /// </summary>
    public static Visibility DefaultToCollapsed(object? value) => value is null ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>
    /// Returns <see cref="Visibility.Collapsed"/> if the specified value is equal to the default value of <typeparamref name="T"/>; otherwise <see
    /// cref="Visibility.Visible"/>.
    /// </summary>
    public static Visibility DefaultToCollapsed<T>(T value) => EqualityComparer<T>.Default.Equals(value, default) ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified focus state is <see cref="FocusState.Unfocused"/>; otherwise <see cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility UnfocusedToVisible(FocusState focusState) => focusState is FocusState.Unfocused ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Collapsed"/> if the specified focus state is <see cref="FocusState.Unfocused"/>; otherwise <see cref="Visibility.Visible"/>.
    /// </summary>
    public static Visibility UnfocusedToCollapsed(FocusState focusState) => focusState is FocusState.Unfocused ? Visibility.Collapsed : Visibility.Visible;

    #endregion

    #region Conversions to Opacity (Double)

    /// <summary>
    /// Returns <c>1</c> if the specified value is <see langword="true"/>; otherwise <c>0</c>.
    /// </summary>
    public static double TrueToOpaque(bool? value) => value is true ? 1 : 0;

    /// <summary>
    /// Returns <c>1</c> if the specified value is <see langword="true"/>; otherwise <c>0</c>.
    /// </summary>
    public static double TrueToOpaque(bool value) => value ? 1 : 0;

    /// <summary>
    /// Returns <c>1</c> if the specified value is <see langword="false"/>; otherwise <c>0</c>.
    /// </summary>
    public static double FalseToOpaque(bool? value) => value is false ? 1 : 0;

    /// <summary>
    /// Returns <c>1</c> if the specified value is <see langword="false"/>; otherwise <c>0</c>.
    /// </summary>
    public static double FalseToOpaque(bool value) => !value ? 1 : 0;

    /// <summary>
    /// Returns <c>1</c> if the specified focus state is <see cref="FocusState.Unfocused"/>; otherwise <c>0</c>.
    /// </summary>
    public static double UnfocusedToOpaque(FocusState focusState) => focusState is FocusState.Unfocused ? 1 : 0;

    /// <summary>
    /// Returns <c>0</c> if the specified focus state is <see cref="FocusState.Unfocused"/>; otherwise <c>1</c>.
    /// </summary>
    public static double UnfocusedToTransparent(FocusState focusState) => focusState is FocusState.Unfocused ? 0 : 1;

    #endregion

    #region Conversions to Uri

    /// <summary>
    /// Converts the specified phone number string to a phone URI.
    /// </summary>
    public static Uri? StringToPhoneUri(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return null;

        try
        {
            return new Uri("tel:" + phoneNumber.Trim());
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("[Singulink.UI.Xaml.WinUI3] Failed to convert phone number string to URI: " + ex);
            return null;
        }
    }

    /// <summary>
    /// Converts the specified email string to an email URI.
    /// </summary>
    public static Uri? StringToEmailUri(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        try
        {
            return new Uri("mailto:" + email.Trim());
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("[Singulink.UI.Xaml.WinUI3] Failed to convert email string to URI: " + ex);
            return null;
        }
    }

    /// <summary>
    /// Converts the specified website string to a website URI. If the string does not start with "http://" or "https://" then "https://" is prepended.
    /// </summary>
    public static Uri? StringToWebsiteUri(string? website)
    {
        if (string.IsNullOrWhiteSpace(website))
            return null;

        website = website.Trim();

        if (!website.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !website.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            website = "https://" + website;

        try
        {
            return new Uri(website);
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("[Singulink.UI.Xaml.WinUI3] Failed to convert website string to URI: " + ex);
            return null;
        }
    }

    #endregion
}
