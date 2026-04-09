using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Singulink.UI.Navigation.Utilities;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents an immutable, insertion-ordered collection of query string parameters with strongly-typed access. Values are stored as strings and
/// converted using the same invariant culture formatting used for route path parameters.
/// </summary>
/// <remarks>
/// <para>
/// Equality is key-order insensitive: two queries with the same key/value pairs compare equal and produce the same hash code regardless of the order
/// in which their entries were added.
/// </para>
/// <para>
/// The <see langword="default"/> value of this type is equivalent to <see cref="Empty"/>.
/// </para>
/// </remarks>
public readonly partial struct RouteQuery : IEquatable<RouteQuery>, IEnumerable<(string Key, string Value)>
{
    /// <summary>
    /// Gets an empty query instance with no parameters.
    /// </summary>
    public static RouteQuery Empty { get; }

    private readonly ImmutableArray<(string Key, string Value)> _entries;

    /// <summary>
    /// A canonical representation of the entries sorted by key (ordinal) using <c>\0</c> as a separator between keys, values, and entries. Used as
    /// the sole basis for <see cref="Equals(RouteQuery)"/> and <see cref="GetHashCode"/> so they are key-order insensitive and cheap.
    /// </summary>
    private readonly string? _sortedKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="RouteQuery"/> struct with the specified pre-formatted string parameters. If duplicate keys are
    /// provided, the last occurrence wins. Use <see cref="RouteQueryBuilder"/> to compose a query from strongly-typed values.
    /// </summary>
    /// <param name="parameters">The query parameters to add.</param>
    /// <exception cref="ArgumentException">A parameter name is null or empty.</exception>
    public RouteQuery(params ReadOnlySpan<(string Name, string Value)> parameters)
    {
        if (parameters.Length is 0)
        {
            _entries = [];
            _sortedKey = string.Empty;
            return;
        }

        List<(string Key, string Value)> entries = new(parameters.Length);

        foreach (var (name, value) in parameters)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Query parameter names cannot be null or empty.", nameof(parameters));

            int existingIndex = FindIndexByKey(CollectionsMarshal.AsSpan(entries), name);

            if (existingIndex >= 0)
                entries[existingIndex] = (name, value);
            else
                entries.Add((name, value));
        }

        _entries = [.. entries];
        _sortedKey = BuildSortedKey(_entries);
    }

    private RouteQuery(ImmutableArray<(string Key, string Value)> entries)
    {
        _entries = entries;
        _sortedKey = entries.Length is 0 ? string.Empty : BuildSortedKey(entries);
    }

    /// <summary>
    /// Determines whether two <see cref="RouteQuery"/> instances are equal.
    /// </summary>
    public static bool operator ==(RouteQuery left, RouteQuery right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="RouteQuery"/> instances are not equal.
    /// </summary>
    public static bool operator !=(RouteQuery left, RouteQuery right) => !left.Equals(right);

    /// <summary>
    /// Gets the number of query parameters.
    /// </summary>
    public int Count => Entries.Length;

    /// <summary>
    /// Gets the normalized entries, treating a default-initialized instance as empty.
    /// </summary>
    private ImmutableArray<(string Key, string Value)> Entries => _entries.IsDefault ? [] : _entries;

    /// <summary>
    /// Attempts to get the value associated with the specified key, parsed as the specified type using invariant culture formatting.
    /// </summary>
    /// <typeparam name="T">The type to parse the value as, which must implement <see cref="IParsable{TSelf}"/>.</typeparam>
    /// <param name="key">The query parameter key.</param>
    /// <param name="value">When this method returns, contains the parsed value if the key was found and parsing succeeded.</param>
    /// <returns><see langword="true"/> if the key was found and the value was successfully parsed; otherwise <see langword="false"/>.</returns>
    /// <exception cref="FormatException">Thrown if the key is found but parsing fails.</exception>
    public bool TryGetValue<T>(string key, [MaybeNullWhen(false)] out T value) where T : IParsable<T>
    {
        var entries = Entries;
        int index = FindIndexByKey(entries.AsSpan(), key);

        if (index < 0)
        {
            value = default;
            return false;
        }

        string stringValue = entries[index].Value;

        if (!RouteValueConverter.TryParse(stringValue, out value))
            throw new FormatException($"Failed to parse query parameter '{key}' with value '{stringValue}' as type '{typeof(T)}'.");

        return true;
    }

    /// <summary>
    /// Determines whether the query contains a parameter with the specified key.
    /// </summary>
    /// <param name="key">The query parameter key.</param>
    public bool ContainsKey(string key) => FindIndexByKey(Entries.AsSpan(), key) >= 0;

    /// <inheritdoc/>
    public bool Equals(RouteQuery other) => (_sortedKey ?? string.Empty) == (other._sortedKey ?? string.Empty);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is RouteQuery other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => (_sortedKey ?? string.Empty).GetHashCode();

    /// <summary>
    /// Returns a query string representation of the parameters (e.g. <c>key1=value1&amp;key2=value2</c>). The result does not include a leading <c>?</c>.
    /// </summary>
    public override string ToString()
    {
        var entries = Entries;

        if (entries.Length is 0)
            return string.Empty;

        var sb = new StringBuilder();

        foreach (var (key, value) in entries)
        {
            if (sb.Length > 0)
                sb.Append('&');

            sb.Append(Uri.EscapeDataString(key));
            sb.Append('=');
            sb.Append(Uri.EscapeDataString(value));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Returns a new <see cref="RouteQueryBuilder"/> seeded with this query's entries.
    /// </summary>
    public RouteQueryBuilder ToBuilder() => new(this);

    internal static RouteQuery FromEntries(IEnumerable<(string Key, string Value)> entries)
    {
        var immutable = entries.ToImmutableArray();
        return immutable.Length is 0 ? Empty : new RouteQuery(immutable);
    }

    internal static RouteQuery Parse(string queryString)
    {
        if (string.IsNullOrEmpty(queryString))
            return Empty;

        List<(string Key, string Value)> entries = [];

        foreach (string segment in queryString.Split('&'))
        {
            if (segment.Length is 0)
                continue;

            int equalsIndex = segment.IndexOf('=');

            if (equalsIndex < 0)
                continue;

            string key = Uri.UnescapeDataString(segment.AsSpan()[..equalsIndex]);
            string value = Uri.UnescapeDataString(segment.AsSpan()[(equalsIndex + 1)..]);

            if (key.Length is 0)
                continue;

            int existingIndex = FindIndexByKey(CollectionsMarshal.AsSpan(entries), key);

            if (existingIndex >= 0)
                entries[existingIndex] = (key, value);
            else
                entries.Add((key, value));
        }

        return entries.Count is 0 ? Empty : new RouteQuery(entries.ToImmutableArray());
    }

    private static int FindIndexByKey(ReadOnlySpan<(string Key, string Value)> entries, string key)
    {
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].Key == key)
                return i;
        }

        return -1;
    }

    private static string BuildSortedKey(ImmutableArray<(string Key, string Value)> entries)
    {
        if (entries.Length is 0)
            return string.Empty;

        var sorted = entries.Sort(static (a, b) => string.CompareOrdinal(a.Key, b.Key));

        int totalLength = 0;
        foreach (var (key, value) in sorted)
            totalLength += key.Length + value.Length + 2; // two '\0' separators per entry

        var sb = new StringBuilder(totalLength);

        foreach (var (key, value) in sorted)
        {
            sb.Append(key);
            sb.Append('\0');
            sb.Append(value);
            sb.Append('\0');
        }

        return sb.ToString();
    }

    /// <summary>
    /// Gets an enumerator over the query parameter key-value pairs in insertion order.
    /// </summary>
    public ImmutableArray<(string Key, string Value)>.Enumerator GetEnumerator() => Entries.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator<(string Key, string Value)> IEnumerable<(string Key, string Value)>.GetEnumerator()
    {
        return ((IEnumerable<(string Key, string Value)>)Entries).GetEnumerator();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)Entries).GetEnumerator();
    }
}
