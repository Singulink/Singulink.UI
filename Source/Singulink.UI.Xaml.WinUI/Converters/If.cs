using System.Runtime.CompilerServices;

namespace Singulink.UI.Xaml.Converters;

/// <summary>
/// Provides conversion methods to <see cref="bool"/> for use in XAML bindings.
/// </summary>
public static class If
{
    /// <summary>
    /// Returns <see langword="true"/> if the specified value is <see langword="true"/>; otherwise <see langword="false"/>.
    /// </summary>
    public static bool True(bool? value) => value is true;

    /// <summary>
    /// Returns <see langword="true"/> if the specified value is <see langword="true"/> or <see langword="null"/>; otherwise <see langword="false"/>.
    /// </summary>
    public static bool TrueOrNull(bool? value) => value is not false;

    /// <summary>
    /// Returns <see langword="true"/> if the specified value is <see langword="false"/>; otherwise <see langword="false"/>.
    /// </summary>
    public static bool False(bool? value) => value is false;

    /// <summary>
    /// Negates the specified boolean value.
    /// </summary>
    public static bool False(bool value) => !value;

    /// <summary>
    /// Returns <see langword="true"/> if the specified value is <see langword="false"/> or <see langword="null"/>; otherwise <see langword="false"/>.
    /// </summary>
    public static bool FalseOrNull(bool? value) => value is not true;

    /// <summary>
    /// Returns <see langword="true"/> if the specified value is <see langword="null"/>; otherwise <see langword="false"/>.
    /// </summary>
    public static bool Null(object? value) => value is null;

    /// <summary>
    /// Returns <see langword="true"/> if the specified value is <see langword="null"/>; otherwise <see langword="false"/>.
    /// </summary>
    public static bool Null<T>(T value) => value is null;

    /// <summary>
    /// Returns <see langword="true"/> if the specified value is not <see langword="null"/>; otherwise <see langword="false"/>.
    /// </summary>
    public static bool NotNull(object? value) => value is not null;

    /// <summary>
    /// Returns <see langword="true"/> if the specified value is not <see langword="null"/>; otherwise <see langword="false"/>.
    /// </summary>
    public static bool NotNull<T>(T value) => value is not null;

    /// <summary>
    /// Returns <see langword="true"/> if the specified value is <see langword="null"/>; otherwise <see langword="false"/>.
    /// </summary>
    public static bool Default(object? value) => value is null;

    /// <summary>
    /// Returns <see langword="true"/> if the specified value is equal to the default value of <typeparamref name="T"/>; otherwise <see langword="false"/>.
    /// </summary>
    public static bool Default<T>(T value) => EqualityComparer<T>.Default.Equals(value, default);

    /// <summary>
    /// Returns <see langword="true"/> if the specified value is not <see langword="null"/>; otherwise <see langword="false"/>.
    /// </summary>
    public static bool NotDefault(object? value) => value is not null;

    /// <summary>
    /// Returns <see langword="true"/> if the specified value is not equal to the default value of <typeparamref name="T"/>; otherwise <see langword="false"/>.
    /// </summary>
    public static bool NotDefault<T>(T value) => !EqualityComparer<T>.Default.Equals(value, default);

    /// <summary>
    /// Returns <see langword="true"/> if the specified value is equal to zero; otherwise <see langword="false"/>.
    /// </summary>
    public static bool Zero(object? value)
    {
        if (value is null)
            return false;

        var type = value.GetType();

        if (!type.IsPrimitive && !type.IsEnum && type != typeof(decimal))
            throw new NotSupportedException($"Type {type} is not supported by this method. Only primitives, enums and decimals are supported. ");

        object defaultValue = Activator.CreateInstance(type);
        return value.Equals(defaultValue);
    }

    /// <summary>
    /// Returns <see langword="true"/> if the specified value is equal to zero; otherwise <see langword="false"/>.
    /// </summary>
    public static bool Zero<T>(T value) where T : unmanaged => EqualityComparer<T>.Default.Equals(value, default);

    /// <summary>
    /// Returns <see langword="true"/> if the specified value is not equal to zero; otherwise <see langword="false"/>.
    /// </summary>
    public static bool NotZero(object? value) => value is null ? false : !Zero(value);

    /// <summary>
    /// Returns <see langword="true"/> if the specified value is not equal to zero; otherwise <see langword="false"/>.
    /// </summary>
    public static bool NotZero<T>(T value) where T : unmanaged => !Zero(value);

    /// <summary>
    /// Returns <see langword="true"/> if the specified value is <see langword="null"/> or an empty string; otherwise to <see langword="false"/>.
    /// </summary>
    public static bool NullOrEmpty(string? value) => string.IsNullOrEmpty(value);

    /// <summary>
    /// Returns <see langword="true"/> if the specified value is not <see langword="null"/> or an empty string; otherwise to <see langword="false"/>.
    /// </summary>
    public static bool NotNullOrEmpty(string? value) => !string.IsNullOrEmpty(value);

    /// <summary>
    /// Returns <see langword="true"/> if the specified value is <see langword="null"/>, empty, or consists only of white-space characters; otherwise to <see
    /// langword="false"/>.
    /// </summary>
    public static bool NullOrWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value);

    /// <summary>
    /// Returns <see langword="true"/> if the specified value is not <see langword="null"/>, empty, or consists only of white-space characters; otherwise to
    /// <see langword="false"/>.
    /// </summary>
    public static bool NotNullOrWhiteSpace(string? value) => !string.IsNullOrWhiteSpace(value);

    /// <summary>
    /// Returns <see langword="true"/> if the specified value's string representation equals the string provided; otherwise <see langword="false"/>.
    /// </summary>
    public static bool EqualsString(object? value, string match) => value?.ToString() == match;

    /// <summary>
    /// Returns <see langword="true"/> if the specified value's string representation equals the string provided; otherwise <see langword="false"/>.
    /// </summary>
    public static bool EqualsString<T>(T value, string match) => value?.ToString() == match;

    /// <summary>
    /// Returns <see langword="true"/> if the specified value's string representation equals any of the string provided; otherwise <see langword="false"/>.
    /// </summary>
    public static bool EqualsAnyString(object? value, string match1, string match2)
        => value?.ToString() is string s && (s == match1 || s == match2);

    /// <summary>
    /// Returns <see langword="true"/> if the specified value's string representation equals any of the string provided; otherwise <see langword="false"/>.
    /// </summary>
    public static bool EqualsAnyString<T>(T value, string match1, string match2)
        => value?.ToString() is string s && (s == match1 || s == match2);

    /// <summary>
    /// Returns <see langword="true"/> if the specified value's string representation equals any of the string provided; otherwise <see langword="false"/>.
    /// </summary>
    public static bool EqualsAnyString(object? value, string match1, string match2, string match3)
        => value?.ToString() is string s && (s == match1 || s == match2 || s == match3);

    /// <summary>
    /// Returns <see langword="true"/> if the specified value's string representation equals any of the string provided; otherwise <see langword="false"/>.
    /// </summary>
    public static bool EqualsAnyString<T>(T value, string match1, string match2, string match3)
        => value?.ToString() is string s && (s == match1 || s == match2 || s == match3);

    /// <summary>
    /// Returns <see langword="true"/> if the specified value's string representation does not equal the string provided; otherwise <see langword="false"/>.
    /// </summary>
    public static bool NotEqualsString(object? value, string match) => value?.ToString() != match;

    /// <summary>
    /// Returns <see langword="true"/> if the specified value's string representation does not equal the string provided; otherwise <see langword="false"/>.
    /// </summary>
    public static bool NotEqualsString<T>(T value, string match) => value?.ToString() != match;

    /// <summary>
    /// Returns <see langword="true"/> if the specified value's string representation does not equal any of the string provided; otherwise <see
    /// langword="false"/>.
    /// </summary>
    public static bool NotEqualsAllStrings(object? value, string match1, string match2)
        => value?.ToString() is var s && s != match1 && s != match2;

    /// <summary>
    /// Returns <see langword="true"/> if the specified value's string representation does not equal any of the string provided; otherwise <see
    /// langword="false"/>.
    /// </summary>
    public static bool NotEqualsAllStrings<T>(T value, string match1, string match2)
        => value?.ToString() is var s && s != match1 && s != match2;

    /// <summary>
    /// Returns <see langword="true"/> if the specified value's string representation does not equal any of the string provided; otherwise <see
    /// langword="false"/>.
    /// </summary>
    public static bool NotEqualsAllStrings(object? value, string match1, string match2, string match3)
        => value?.ToString() is var s && s != match1 && s != match2 && s != match3;

    /// <summary>
    /// Returns <see langword="true"/> if the specified value's string representation does not equal any of the string provided; otherwise <see
    /// langword="false"/>.
    /// </summary>
    public static bool NotEqualsAllStrings<T>(T value, string match1, string match2, string match3)
        => value?.ToString() is var s && s != match1 && s != match2 && s != match3;
}
