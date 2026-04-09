using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Singulink.UI.Navigation.Utilities;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a three-phase, mutable, insertion-ordered collection of route parameter key-value pairs used to bridge URL generation and matching with
/// <see cref="IRouteParamsModel{TSelf}"/> implementations.
/// </summary>
/// <remarks>
/// <para>
/// The collection progresses through three states:
/// </para>
/// <list type="bullet">
///   <item><b>Building</b> (initial): <see cref="Add{T}(string, T)"/>, <see cref="Reserve(string)"/>, and <see cref="AddQuery(RouteQuery)"/> may be
///   called. Each call to <see cref="Add{T}(string, T)"/> or <see cref="Reserve(string)"/> reserves the key so that a subsequent
///   <see cref="AddQuery(RouteQuery)"/> cannot add a conflicting entry; such a conflict throws. <see cref="AddQuery(RouteQuery)"/> transitions the
///   collection to the Consuming state.</item>
///   <item><b>Consuming</b>: entered via <see cref="AddQuery(RouteQuery)"/> or automatically on the first call to any consume or read member
///   (<see cref="TryConsume{T}"/>, enumerators, etc.). Further <see cref="Add{T}(string, T)"/>, <see cref="Reserve(string)"/>, and
///   <see cref="AddQuery(RouteQuery)"/> calls throw.</item>
///   <item><b>Done</b>: entered when <see cref="ConsumeQuery"/> is called. All remaining entries are captured into a <see cref="RouteQuery"/> and
///   the collection is fully exhausted. <see cref="Count"/>, consume methods, and enumerators all throw in this state.</item>
/// </list>
/// <para>
/// Instances are not thread-safe.
/// </para>
/// </remarks>
public sealed partial class RouteValuesCollection : IEnumerable<(string Key, string Value)>
{
    /// <summary>
    /// The entries in the collection. <see langword="null"/> indicates the collection has transitioned to the Done state.
    /// </summary>
    private List<(string Key, string Value)>? _entries;

    /// <summary>
    /// Tracks reserved keys while in the Building state. <see langword="null"/> indicates the collection has transitioned to the Consuming or Done
    /// state. Kept as a list since the expected number of entries is small.
    /// </summary>
    private List<string>? _reservedKeys = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="RouteValuesCollection"/> class in the Building state.
    /// </summary>
    public RouteValuesCollection()
    {
        _entries = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RouteValuesCollection"/> class in the Building state with the specified initial capacity.
    /// </summary>
    public RouteValuesCollection(int capacity)
    {
        _entries = new(capacity);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RouteValuesCollection"/> class already in the Consuming state, containing the specified path
    /// parameters followed by the non-conflicting entries from the specified query. If a query entry's key matches a path parameter's key, the query
    /// entry is silently dropped so that path values always win.
    /// </summary>
    /// <remarks>
    /// This constructor trusts that <paramref name="pathParams"/> does not contain duplicate keys.
    /// </remarks>
    internal RouteValuesCollection(List<(string Key, string Value)>? pathParams, RouteQuery queryParams)
    {
        _entries = new((pathParams?.Count ?? 0) + queryParams.Count);

        if (pathParams is not null)
            _entries.AddRange(pathParams);

        foreach (var (key, value) in queryParams)
        {
            if (pathParams is null || FindIndexByKey(pathParams, key) < 0)
                _entries.Add((key, value));
        }

        // Collection is used for route matching only; it starts in the Consuming state.
        _reservedKeys = null;
    }

    /// <summary>
    /// Gets the number of entries currently in the collection.
    /// </summary>
    /// <exception cref="InvalidOperationException">The collection is in the Done state.</exception>
    public int Count
    {
        get
        {
            EnsureNotDone();
            return _entries.Count;
        }
    }

    /// <summary>
    /// Adds a parameter entry and reserves the specified key so that a subsequent <see cref="AddQuery(RouteQuery)"/> cannot add a conflicting entry.
    /// </summary>
    /// <exception cref="InvalidOperationException">The collection is not in the Building state.</exception>
    /// <exception cref="ArgumentException">The key has already been reserved.</exception>
    public void Add<T>(string key, T value) where T : notnull, IParsable<T>
    {
        EnsureBuilding();
        ReserveCore(_reservedKeys, key);
        _entries.Add((key, RouteValueConverter.Format(value)));
    }

    /// <summary>
    /// Reserves the specified key without adding an entry, so that a subsequent <see cref="AddQuery(RouteQuery)"/> cannot add an entry with a
    /// matching key. Intended for optional named parameters whose current value is <see langword="null"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The collection is not in the Building state.</exception>
    /// <exception cref="ArgumentException">The key has already been reserved.</exception>
    public void Reserve(string key)
    {
        EnsureBuilding();
        ReserveCore(_reservedKeys, key);
    }

    /// <summary>
    /// Adds all entries from the specified <see cref="RouteQuery"/> and transitions the collection to the Consuming state. Throws if any entry's
    /// key has already been reserved by <see cref="Add{T}"/> or <see cref="Reserve(string)"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The collection is not in the Building state.</exception>
    /// <exception cref="ArgumentException">An entry's key matches a key that has already been reserved.</exception>
    public void AddQuery(RouteQuery query)
    {
        EnsureBuilding();

        foreach (var (key, value) in query)
        {
            if (_reservedKeys.Contains(key))
            {
                throw new ArgumentException(
                    $"Cannot add query entry '{key}': a named parameter with the same key has already been reserved on this collection. " +
                    $"The Rest property must not contain keys that conflict with other named parameters on the route params model.",
                    nameof(query));
            }

            _entries.Add((key, value));
        }

        BeginConsuming();
    }

    /// <summary>
    /// Tries to consume (parse and remove) the entry with the specified key. Returns <see langword="false"/> if the key is not found or parsing
    /// fails. Transitions the collection to the Consuming state if it is not already.
    /// </summary>
    /// <exception cref="InvalidOperationException">The collection is in the Done state.</exception>
    public bool TryConsume<T>(string key, [MaybeNullWhen(false)] out T value)
        where T : notnull, IParsable<T>
    {
        EnsureNotDone();
        BeginConsuming();

        int index = FindIndexByKey(_entries, key);

        if (index >= 0)
        {
            string strValue = _entries[index].Value;
            _entries.RemoveAt(index);

            if (RouteValueConverter.TryParse(strValue, out value))
                return true;

            value = default;
            return false;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Returns all remaining entries as a <see cref="RouteQuery"/> and transitions the collection to the Done state. No further members may be
    /// accessed after this call.
    /// </summary>
    /// <exception cref="InvalidOperationException">The collection is already in the Done state.</exception>
    public RouteQuery ConsumeQuery()
    {
        EnsureNotDone();
        BeginConsuming();

        var entries = _entries;
        _entries = null;

        return entries.Count is 0 ? RouteQuery.Empty : RouteQuery.FromEntries(entries);
    }

    /// <summary>
    /// Tries to get the string value for the specified key without consuming it. Transitions the collection to the Consuming state if it is not
    /// already.
    /// </summary>
    /// <exception cref="InvalidOperationException">The collection is in the Done state.</exception>
    internal bool TryGetValue(string key, [MaybeNullWhen(false)] out string value)
    {
        EnsureNotDone();
        BeginConsuming();

        int index = FindIndexByKey(_entries, key);

        if (index >= 0)
        {
            value = _entries[index].Value;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Tries to consume (remove and return) the raw string entry with the specified key. Transitions the collection to the Consuming state if it is
    /// not already.
    /// </summary>
    /// <exception cref="InvalidOperationException">The collection is in the Done state.</exception>
    internal bool TryConsumeValue(string key, [MaybeNullWhen(false)] out string value)
    {
        EnsureNotDone();
        BeginConsuming();

        int index = FindIndexByKey(_entries, key);

        if (index >= 0)
        {
            value = _entries[index].Value;
            _entries.RemoveAt(index);
            return true;
        }

        value = default;
        return false;
    }

    /// <inheritdoc/>
    public IEnumerator<(string Key, string Value)> GetEnumerator()
    {
        EnsureNotDone();
        BeginConsuming();
        return ((IEnumerable<(string Key, string Value)>)_entries).GetEnumerator();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    [MemberNotNull(nameof(_reservedKeys), nameof(_entries))]
    private void EnsureBuilding()
    {
        if (_entries is null)
            throw new InvalidOperationException("The collection is in the Done state; no further entries can be added.");

        if (_reservedKeys is null)
            throw new InvalidOperationException("The collection is in the Consuming state; no further entries can be added.");
    }

    [MemberNotNull(nameof(_entries))]
    private void EnsureNotDone()
    {
        if (_entries is null)
            throw new InvalidOperationException("The collection is in the Done state; no further members may be accessed.");
    }

    private void BeginConsuming() => _reservedKeys = null;

    private static void ReserveCore(List<string> reserved, string key)
    {
        if (reserved.Contains(key))
            throw new ArgumentException($"A parameter with the key '{key}' has already been reserved on this collection.", nameof(key));

        reserved.Add(key);
    }

    private static int FindIndexByKey(List<(string Key, string Value)> entries, string key)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].Key == key)
                return i;
        }

        return -1;
    }
}
