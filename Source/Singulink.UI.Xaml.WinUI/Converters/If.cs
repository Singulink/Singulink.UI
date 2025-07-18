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
    /// <exception cref="ArgumentException">Thrown when the type of <paramref name="value"/> is not a primitive numeric type or <see
    /// cref="decimal"/>.</exception>
    public static bool Zero(object value) => Type.GetTypeCode(value.GetType()) switch {
        TypeCode.Byte => (byte)value == 0,
        TypeCode.SByte => (sbyte)value == 0,
        TypeCode.Int16 => (short)value == 0,
        TypeCode.Int32 => (int)value == 0,
        TypeCode.Int64 => (long)value == 0,
        TypeCode.UInt16 => (ushort)value == 0,
        TypeCode.UInt32 => (uint)value == 0,
        TypeCode.UInt64 => (ulong)value == 0,
        TypeCode.Single => (float)value == 0f,
        TypeCode.Double => (double)value == 0d,
        TypeCode.Decimal => (decimal)value == 0m,
        _ => throw new ArgumentException($"Type {value.GetType()} is not supported. Only primitive numeric types and decimals are supported.", nameof(value)),
    };

    /// <summary>
    /// Returns <see langword="true"/> if the specified value is equal to zero; otherwise <see langword="false"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the type of <paramref name="value"/> is not a primitive numeric type or <see
    /// cref="decimal"/>.</exception>
    public static bool Zero<T>(T value) where T : struct
    {
        if (typeof(T) != typeof(byte) &&
            typeof(T) != typeof(sbyte) &&
            typeof(T) != typeof(short) &&
            typeof(T) != typeof(int) &&
            typeof(T) != typeof(long) &&
            typeof(T) != typeof(ushort) &&
            typeof(T) != typeof(uint) &&
            typeof(T) != typeof(ulong) &&
            typeof(T) != typeof(float) &&
            typeof(T) != typeof(double) &&
            typeof(T) != typeof(decimal))
        {
            throw new ArgumentException($"Type {typeof(T)} is not supported. Only primitive numeric types and decimals are supported.");
        }

        return EqualityComparer<T>.Default.Equals(value, default);
    }

    /// <summary>
    /// Returns <see langword="true"/> if the specified value is not equal to zero; otherwise <see langword="false"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the type of <paramref name="value"/> is not a primitive numeric type or <see
    /// cref="decimal"/>.</exception>
    public static bool NotZero(object value) => !Zero(value);

    /// <summary>
    /// Returns <see langword="true"/> if the specified value is not equal to zero; otherwise <see langword="false"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the type of <paramref name="value"/> is not a primitive numeric type or <see
    /// cref="decimal"/>.</exception>
    public static bool NotZero<T>(T value) where T : unmanaged => !Zero(value);

    /// <summary>
    /// Returns <see langword="true"/> if the specified focus state is any state other than <see cref="FocusState.Unfocused"/>; otherwise <see
    /// langword="false"/>.
    /// </summary>
    public static bool Focused(FocusState focusState) => focusState is not FocusState.Unfocused;

    /// <summary>
    /// Returns <see langword="true"/> if the specified focus state is <see cref="FocusState.Unfocused"/>; otherwise <see langword="false"/>.
    /// </summary>
    public static bool Unfocused(FocusState focusState) => focusState is FocusState.Unfocused;

    /// <summary>
    /// Returns <see langword="true"/> if the specified value is <see langword="null"/> or an empty string; otherwise <see langword="false"/>.
    /// </summary>
    public static bool NullOrEmpty(string? value) => string.IsNullOrEmpty(value);

    /// <summary>
    /// Returns <see langword="true"/> if the specified value is not <see langword="null"/> or an empty string; otherwise <see langword="false"/>.
    /// </summary>
    public static bool NotNullOrEmpty(string? value) => !string.IsNullOrEmpty(value);

    /// <summary>
    /// Returns <see langword="true"/> if the specified value is <see langword="null"/>, empty, or consists only of white-space characters; otherwise <see
    /// langword="false"/>.
    /// </summary>
    public static bool NullOrWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value);

    /// <summary>
    /// Returns <see langword="true"/> if the specified value is not <see langword="null"/>, empty, or consists only of white-space characters; otherwise <see
    /// langword="false"/>.
    /// </summary>
    public static bool NotNullOrWhiteSpace(string? value) => !string.IsNullOrWhiteSpace(value);

    /// <summary>
    /// Returns <see langword="true"/> if the specified value's string representation equals the string provided; otherwise <see langword="false"/>.
    /// </summary>
    public static bool StringEquals(object? value, string match) => value?.ToString() == match;

    /// <summary>
    /// Returns <see langword="true"/> if the specified value's string representation equals the string provided; otherwise <see langword="false"/>.
    /// </summary>
    public static bool StringEquals<T>(T value, string match) => value?.ToString() == match;

    /// <summary>
    /// Returns <see langword="true"/> if the specified value's string representation equals any of the string provided; otherwise <see langword="false"/>.
    /// </summary>
    public static bool StringEqualsAny(object? value, string match1, string match2)
        => value?.ToString() is string s && (s == match1 || s == match2);

    /// <summary>
    /// Returns <see langword="true"/> if the specified value's string representation equals any of the string provided; otherwise <see langword="false"/>.
    /// </summary>
    public static bool StringEqualsAny<T>(T value, string match1, string match2)
        => value?.ToString() is string s && (s == match1 || s == match2);

    /// <summary>
    /// Returns <see langword="true"/> if the specified value's string representation equals any of the string provided; otherwise <see langword="false"/>.
    /// </summary>
    public static bool StringEqualsAny(object? value, string match1, string match2, string match3)
        => value?.ToString() is string s && (s == match1 || s == match2 || s == match3);

    /// <summary>
    /// Returns <see langword="true"/> if the specified value's string representation equals any of the string provided; otherwise <see langword="false"/>.
    /// </summary>
    public static bool StringEqualsAny<T>(T value, string match1, string match2, string match3)
        => value?.ToString() is string s && (s == match1 || s == match2 || s == match3);

    /// <summary>
    /// Returns <see langword="true"/> if the specified value's string representation does not equal the string provided; otherwise <see langword="false"/>.
    /// </summary>
    public static bool StringNotEquals(object? value, string match) => value?.ToString() != match;

    /// <summary>
    /// Returns <see langword="true"/> if the specified value's string representation does not equal the string provided; otherwise <see langword="false"/>.
    /// </summary>
    public static bool StringNotEquals<T>(T value, string match) => value?.ToString() != match;

    /// <summary>
    /// Returns <see langword="true"/> if the specified value's string representation does not equal any of the string provided; otherwise <see
    /// langword="false"/>.
    /// </summary>
    public static bool StringNotEqualsAny(object? value, string match1, string match2)
        => value?.ToString() is var s && s != match1 && s != match2;

    /// <summary>
    /// Returns <see langword="true"/> if the specified value's string representation does not equal any of the string provided; otherwise <see
    /// langword="false"/>.
    /// </summary>
    public static bool StringNotEqualsAny<T>(T value, string match1, string match2)
        => value?.ToString() is var s && s != match1 && s != match2;

    /// <summary>
    /// Returns <see langword="true"/> if the specified value's string representation does not equal any of the string provided; otherwise <see
    /// langword="false"/>.
    /// </summary>
    public static bool StringNotEqualsAny(object? value, string match1, string match2, string match3)
        => value?.ToString() is var s && s != match1 && s != match2 && s != match3;

    /// <summary>
    /// Returns <see langword="true"/> if the specified value's string representation does not equal any of the string provided; otherwise <see
    /// langword="false"/>.
    /// </summary>
    public static bool StringNotEqualsAny<T>(T value, string match1, string match2, string match3)
        => value?.ToString() is var s && s != match1 && s != match2 && s != match3;
}
