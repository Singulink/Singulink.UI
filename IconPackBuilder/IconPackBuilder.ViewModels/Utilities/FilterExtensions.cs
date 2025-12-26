namespace IconPackBuilder.ViewModels.Utilities;

public static class FilterExtensions
{
    public static IEnumerable<T> Filter<T>(this IEnumerable<T> source, string filter, Func<T, string> valueSelector)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return source;

        string[] filterParts = filter.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return source.Where(item => MatchesFilter(valueSelector(item), filterParts));
    }

    public static bool MatchesFilter(this string value, string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return true;

        string[] filterParts = filter.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return MatchesFilter(value, filterParts);
    }

    private static bool MatchesFilter(string value, string[] filterParts)
    {
        foreach (string filterPart in filterParts)
        {
            bool found = false;

            foreach (var range in value.AsSpan().Split(' '))
            {
                var valuePart = value.AsSpan()[range];

                if (valuePart.StartsWith(filterPart, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
                return false;
        }

        return true;
    }
}
