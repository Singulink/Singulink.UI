using Microsoft.UI.Xaml.Data;

namespace Singulink.UI.Xaml.Converters;

/// <summary>
/// Converts a value to a boolean or <see cref="Visibility"/> based on a value's type.
/// </summary>
public partial class TypeMatchConverter : IValueConverter
{
    /// <summary>
    /// Gets or sets the type to match.
    /// </summary>
    public Type? MatchType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the match should be exact, or whether subclasses will also match. Default is <see langword="false"/>.
    /// </summary>
    public bool IsExact { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the result should be inverted (i.e. a match returns <see langword="false"/> or <see
    /// cref="Visibility.Collapsed"/>). Default is <see langword="false"/>.
    /// </summary>
    public bool InvertResult { get; set; }

    /// <summary>
    /// Converts the specified value to a <see cref="bool"/> or <see cref="Visibility"/> (depending on the specified target type) and whether the value's type
    /// matches <see cref="MatchType"/>.
    /// </summary>
    public object Convert(object? value, Type targetType, object? parameter, string? language)
    {
        bool result;

        if (MatchType == null || value == null)
            result = false;
        else
            result = IsExact ? MatchType == value as Type : MatchType.IsInstanceOfType(value);

        if (InvertResult)
            result = !result;

        if (targetType == typeof(bool) || targetType == typeof(bool?))
            return result;

        if (targetType == typeof(Visibility) || targetType == typeof(Visibility?))
            return result ? Visibility.Visible : Visibility.Collapsed;

        throw new ArgumentException($"Target type {targetType} is not supported.");
    }

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">Operation is not supported.</exception>
    public object ConvertBack(object? value, Type targetType, object? parameter, string? language) => throw new NotSupportedException();
}
