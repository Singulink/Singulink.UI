using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Singulink.UI.Navigation.Utilities;

internal static class RouteValueConverter
{
    public static string Format<T>(T value) where T : notnull, IParsable<T>
    {
        if (typeof(T) == typeof(string))
            return (string)(object)value;

        if (value is IFormattable formattable)
        {
            string? format = value switch {
                double => "G17",
                float => "G9",
                DateTime or DateTimeOffset or DateOnly or TimeOnly => "O",
                _ => null,
            };

            return formattable.ToString(format, CultureInfo.InvariantCulture);
        }

        using (new InvariantCultureContext())
            return value.ToString() ?? string.Empty;
    }

    public static bool TryParse<T>(string s, [MaybeNullWhen(false)] out T value) where T : notnull, IParsable<T>
    {
        if (typeof(T) == typeof(DateTime))
        {
            bool result = DateTime.TryParseExact(s, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTimeValue);
            value = (T)(object)dateTimeValue;
            return result;
        }

        if (typeof(T) == typeof(DateTimeOffset))
        {
            bool result = DateTimeOffset.TryParseExact(s, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTimeOffsetValue);
            value = (T)(object)dateTimeOffsetValue;
            return result;
        }

        if (typeof(T) == typeof(DateOnly))
        {
            bool result = DateOnly.TryParseExact(s, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnlyValue);
            value = (T)(object)dateOnlyValue;
            return result;
        }

        if (typeof(T) == typeof(TimeOnly))
        {
            bool result = TimeOnly.TryParseExact(s, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var timeOnlyValue);
            value = (T)(object)timeOnlyValue;
            return result;
        }

        return T.TryParse(s, CultureInfo.InvariantCulture, out value);
    }
}
