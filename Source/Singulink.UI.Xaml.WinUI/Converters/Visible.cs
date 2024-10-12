namespace Singulink.UI.Xaml.Converters;

/// <summary>
/// Provides conversion methods to <see cref="Visibility"/> for use in XAML bindings.
/// </summary>
public static class Visible
{
    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value is <see langword="true"/>; otherwise <see cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility IfTrue(bool? value) => value is true ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value is <see langword="true"/>; otherwise <see cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility IfTrue(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value is <see langword="true"/> or <see langword="null"/>; otherwise <see
    /// cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility IfTrueOrNull(bool? value) => value is not false ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value is <see langword="false"/>; otherwise <see cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility IfFalse(bool? value) => value is false ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value is <see langword="false"/>; otherwise <see cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility IfFalse(bool value) => !value ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value is <see langword="false"/>; otherwise <see cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility IfFalseOrNull(bool? value) => value is not true ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value is <see langword="null"/>; otherwise <see cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility IfNull(object? value) => value is null ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value is <see langword="null"/>; otherwise <see cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility IfNull<T>(T value) => value is null ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value is not <see langword="null"/>; otherwise <see cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility IfNotNull(object? value) => value is not null ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value is not <see langword="null"/>; otherwise <see cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility IfNotNull<T>(T value) => value is not null ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value is <see langword="null"/>; otherwise <see cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility IfDefault(object? value) => value is null ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value is equal to the default value of <typeparamref name="T"/>; otherwise <see
    /// cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility IfDefault<T>(T value) => EqualityComparer<T>.Default.Equals(value, default) ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value is not <see langword="null"/>; otherwise <see cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility IfNotDefault(object? value) => value is not null ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value is not equal to the default value of <typeparamref name="T"/>; otherwise <see
    /// cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility IfNotDefault<T>(T value) => !EqualityComparer<T>.Default.Equals(value, default) ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified focus state is any state other than <see cref="FocusState.Unfocused"/>; otherwise <see
    /// cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility IfFocused(FocusState focusState) => focusState is not FocusState.Unfocused ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified focus state is <see cref="FocusState.Unfocused"/>; otherwise <see cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility IfUnfocused(FocusState focusState) => focusState is FocusState.Unfocused ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value is <see langword="null"/> or an empty string; otherwise <see
    /// cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility IfNullOrEmpty(string? value) => string.IsNullOrEmpty(value) ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value is not <see langword="null"/> or an empty string; otherwise <see
    /// cref="Visibility.Visible"/>.
    /// </summary>
    public static Visibility IfNotNullOrEmpty(string? value) => !string.IsNullOrEmpty(value) ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value is <see langword="null"/>, empty, or consists only of white-space characters; otherwise
    /// <see cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility IfNullOrWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value is not <see langword="null"/>, empty, or consists only of white-space characters;
    /// otherwise <see cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility IfNotNullOrWhiteSpace(string? value) => !string.IsNullOrWhiteSpace(value) ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value's string representation equals the string provided; otherwise <see
    /// cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility IfEqualsString(object? value, string match) => value?.ToString() == match ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value's string representation equals the string provided; otherwise <see
    /// cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility IfEqualsString<T>(T value, string match) => value?.ToString() == match ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value's string representation equals any of the string provided; otherwise <see
    /// cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility IfEqualsAnyString(object? value, string match1, string match2)
        => value?.ToString() is string s && (s == match1 || s == match2) ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value's string representation equals any of the string provided; otherwise <see
    /// cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility IfEqualsAnyString<T>(T value, string match1, string match2)
        => value?.ToString() is string s && (s == match1 || s == match2) ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value's string representation equals any of the string provided; otherwise <see
    /// cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility IfEqualsAnyString(object? value, string match1, string match2, string match3)
        => value?.ToString() is string s && (s == match1 || s == match2 || s == match3) ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value's string representation equals any of the string provided; otherwise <see
    /// cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility IfEqualsAnyString<T>(T value, string match1, string match2, string match3)
        => value?.ToString() is string s && (s == match1 || s == match2 || s == match3) ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value's string representation does not equal the string provided; otherwise <see
    /// cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility IfNotEqualsString(object? value, string match) => value?.ToString() != match ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value's string representation does not equal the string provided; otherwise <see
    /// cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility IfNotEqualsString<T>(T value, string match) => value?.ToString() != match ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value's string representation does not equal any of the string provided; otherwise <see
    /// cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility IfNotEqualsAllStrings(object? value, string match1, string match2)
        => value?.ToString() is var s && s != match1 && s != match2 ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value's string representation does not equal any of the string provided; otherwise <see
    /// cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility IfNotEqualsAllStrings<T>(T value, string match1, string match2)
        => value?.ToString() is var s && s != match1 && s != match2 ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value's string representation does not equal any of the string provided; otherwise <see
    /// cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility IfNotEqualsAllStrings(object? value, string match1, string match2, string match3)
        => value?.ToString() is var s && s != match1 && s != match2 && s != match3 ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if the specified value's string representation does not equal any of the string provided; otherwise <see
    /// cref="Visibility.Collapsed"/>.
    /// </summary>
    public static Visibility IfNotEqualsAllStrings<T>(T value, string match1, string match2, string match3)
        => value?.ToString() is var s && s != match1 && s != match2 && s != match3 ? Visibility.Visible : Visibility.Collapsed;
}
