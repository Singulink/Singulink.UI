using Microsoft.UI.Xaml.Data;

namespace Singulink.UI.Xaml.Converters;

/// <summary>
/// Converts a boolean value to its negated value.
/// </summary>
public partial class NegateConverter : IValueConverter
{
    /// <summary>
    /// Converts a boolean value to its negated value.
    /// </summary>
    public object? Convert(object? value, Type targetType, object? parameter, string? language) => Convert(value);

    /// <summary>
    /// Converts a boolean value back from its negated value.
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, string? language) => Convert(value);

    private static object? Convert(object? value)
    {
        if (value is bool b)
            return !b;

        if (value is null)
            return null;

        throw new ArgumentException("Value must be null or a boolean.", nameof(value));
    }
}
