using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Singulink.UI.Navigation.Utilities;

namespace Singulink.UI.Navigation;

/// <summary>
/// A mutable fluent builder for composing <see cref="RouteQuery"/> instances. Obtain one via <see cref="RouteQuery.ToBuilder"/> or by constructing a new
/// instance, and finalize the result with <see cref="ToQuery"/>.
/// </summary>
public sealed partial class RouteQueryBuilder : IEnumerable<(string Key, string Value)>
{
    private readonly List<(string Key, string Value)> _entries;

    /// <summary>
    /// Initializes a new instance of the <see cref="RouteQueryBuilder"/> class that is empty.
    /// </summary>
    public RouteQueryBuilder()
    {
        _entries = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RouteQueryBuilder"/> class that is empty and has the specified initial capacity.
    /// </summary>
    public RouteQueryBuilder(int capacity)
    {
        _entries = new(capacity);
    }

    internal RouteQueryBuilder(RouteQuery source)
    {
        _entries = [.. source];
    }

    /// <summary>
    /// Gets the number of entries in the builder.
    /// </summary>
    public int Count => _entries.Count;

    /// <summary>
    /// Adds a new entry with the specified key and strongly-typed value. Throws if an entry with the same key already exists.
    /// </summary>
    /// <exception cref="ArgumentException">An entry with the same key already exists.</exception>
    public RouteQueryBuilder Add<T>(string key, T value) where T : IParsable<T>
    {
        if (IndexOfKey(key) >= 0)
            throw new ArgumentException($"An entry with the key '{key}' already exists.", nameof(key));

        _entries.Add((key, RouteValueConverter.Format(value)));
        return this;
    }

    /// <summary>
    /// Sets the value for the specified key, adding a new entry if one does not already exist or replacing the existing value if it does.
    /// </summary>
    public RouteQueryBuilder Set<T>(string key, T value) where T : IParsable<T>
    {
        string strValue = RouteValueConverter.Format(value);
        int index = IndexOfKey(key);

        if (index >= 0)
            _entries[index] = (key, strValue);
        else
            _entries.Add((key, strValue));

        return this;
    }

    /// <summary>
    /// Removes the entry with the specified key, if it exists.
    /// </summary>
    public RouteQueryBuilder Remove(string key)
    {
        int index = IndexOfKey(key);

        if (index >= 0)
            _entries.RemoveAt(index);

        return this;
    }

    /// <summary>
    /// Removes the entries with the specified keys, if they exist.
    /// </summary>
    public RouteQueryBuilder Remove(params ReadOnlySpan<string> keys)
    {
        foreach (string key in keys)
            Remove(key);

        return this;
    }

    /// <summary>
    /// Removes the entries with the specified keys, if they exist.
    /// </summary>
    public RouteQueryBuilder Remove(IEnumerable<string> keys)
    {
        foreach (string key in keys)
            Remove(key);

        return this;
    }

    /// <summary>
    /// Determines whether the builder contains an entry with the specified key.
    /// </summary>
    public bool ContainsKey(string key) => IndexOfKey(key) >= 0;

    /// <summary>
    /// Attempts to get the value associated with the specified key, parsed as the specified type using invariant culture formatting.
    /// </summary>
    /// <exception cref="FormatException">The key was found but parsing failed.</exception>
    public bool TryGetValue<T>(string key, [MaybeNullWhen(false)] out T value) where T : IParsable<T>
    {
        int index = IndexOfKey(key);

        if (index < 0)
        {
            value = default;
            return false;
        }

        string strValue = _entries[index].Value;

        if (!RouteValueConverter.TryParse(strValue, out value))
            throw new FormatException($"Failed to parse query parameter '{key}' with value '{strValue}' as type '{typeof(T)}'.");

        return true;
    }

    /// <summary>
    /// Finalizes the builder into an immutable <see cref="RouteQuery"/>.
    /// </summary>
    public RouteQuery ToQuery() => _entries.Count is 0 ? RouteQuery.Empty : RouteQuery.FromEntries(_entries);

    /// <inheritdoc/>
    public IEnumerator<(string Key, string Value)> GetEnumerator() => ((IEnumerable<(string Key, string Value)>)_entries).GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_entries).GetEnumerator();

    private int IndexOfKey(string key)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].Key == key)
                return i;
        }

        return -1;
    }
}
