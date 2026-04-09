using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;
using Singulink.UI.Navigation.Utilities;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents an immutable list of parsable values that can be used as a route path parameter, query string parameter, or as a property in an
/// <see cref="IRouteParamsModel{TSelf}"/>. Implements <see cref="IParsable{TSelf}"/> and <see cref="IEquatable{T}"/>.
/// </summary>
/// <typeparam name="T">The element type. Must implement <see cref="IParsable{TSelf}"/> and <see cref="IEquatable{T}"/>.</typeparam>
/// <remarks>
/// <para>
/// The string representation uses one of two URI-safe encodings, chosen so that structural delimiters do not require URI escaping:
/// </para>
/// <list type="bullet">
///   <item>
///     <b>Tilde-separated:</b> a leading tilde (<c>~</c>) followed by values separated by tildes, e.g. <c>~1~2~3</c>. Used when no value contains
///     a tilde.
///   </item>
///   <item>
///     <b>Length-prefixed:</b> each value is written as <c>{length}~{value}</c>, e.g. <c>5~hello5~world</c>. Used when any value contains a tilde,
///     and as the safe fallback for non-whitelisted element types.
///   </item>
/// </list>
/// <para>
/// An empty string parses to an empty list. The first character of the input selects the decoder.
/// </para>
/// </remarks>
public readonly struct ValueList<T> : IParsable<ValueList<T>>, IEquatable<ValueList<T>>, IReadOnlyList<T>
    where T : notnull, IParsable<T>, IEquatable<T>
{
    /// <summary>
    /// Element types whose formatted representation is statically known to never contain a tilde, allowing us to skip the dynamic scan and always
    /// emit tilde-separated.
    /// </summary>
    private static readonly bool IsTildeSafe = DetermineTildeSafe();

    private readonly ImmutableArray<T> _data;
    private readonly string? _toString;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueList{T}"/> struct by copying the specified span.
    /// </summary>
    /// <param name="items">The items to copy.</param>
    public ValueList(params ReadOnlySpan<T> items)
    {
        _data = [.. items];
        _toString = FormatItems(items);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueList{T}"/> struct with the specified immutable array.
    /// </summary>
    /// <param name="items">The immutable array of items.</param>
    public ValueList(ImmutableArray<T> items)
    {
        _data = items;
        _toString = FormatItems(Value.AsSpan());
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueList{T}"/> struct with the items from the specified sequence.
    /// </summary>
    /// <param name="items">The items.</param>
    public ValueList(IEnumerable<T> items)
    {
        _data = [.. items];
        _toString = FormatItems(_data.AsSpan());
    }

    /// <summary>
    /// Gets the immutable items.
    /// </summary>
    public ImmutableArray<T> Value => _data.IsDefault ? [] : _data;

    /// <inheritdoc/>
    public int Count => Value.Length;

    /// <inheritdoc/>
    public T this[int index] => Value[index];

    /// <summary>
    /// Converts an <see cref="ImmutableArray{T}"/> to a <see cref="ValueList{T}"/> without copying.
    /// </summary>
    public static implicit operator ValueList<T>(ImmutableArray<T> items) => new(items);

    /// <summary>
    /// Converts a <see cref="ValueList{T}"/> to an <see cref="ImmutableArray{T}"/> without copying.
    /// </summary>
    public static implicit operator ImmutableArray<T>(ValueList<T> list) => list.Value;

    /// <summary>
    /// Converts a <see cref="ValueList{T}"/> to a <see cref="ReadOnlySpan{T}"/> without copying.
    /// </summary>
    public static implicit operator ReadOnlySpan<T>(ValueList<T> list) => list.AsSpan();

    /// <summary>
    /// Converts a <see cref="ValueList{T}"/> to a <see cref="ReadOnlyMemory{T}"/> without copying.
    /// </summary>
    public static implicit operator ReadOnlyMemory<T>(ValueList<T> list) => list.AsMemory();

    /// <summary>
    /// Returns the items as a <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    public ReadOnlySpan<T> AsSpan() => Value.AsSpan();

    /// <summary>
    /// Returns the items as a <see cref="ReadOnlyMemory{T}"/>.
    /// </summary>
    public ReadOnlyMemory<T> AsMemory() => Value.AsMemory();

    /// <summary>
    /// Returns a copy of the items as a new array.
    /// </summary>
    public T[] ToArray() => [.. Value];

    /// <summary>
    /// Parses a string into a <see cref="ValueList{T}"/>.
    /// </summary>
    /// <exception cref="FormatException">The input string is malformed or contains a field that cannot be parsed as <typeparamref name="T"/>.</exception>
    public static ValueList<T> Parse(string s)
    {
        if (!TryParse(s, out var result))
            throw new FormatException($"Cannot parse the specified string as a {typeof(ValueList<T>)} value.");

        return result;
    }

    /// <inheritdoc/>
    static ValueList<T> IParsable<ValueList<T>>.Parse(string s, IFormatProvider? provider) => Parse(s);

    /// <summary>
    /// Tries to parse a string into a <see cref="ValueList{T}"/>.
    /// </summary>
    public static bool TryParse([NotNullWhen(true)] string? s, out ValueList<T> result)
    {
        if (s is null)
        {
            result = default;
            return false;
        }

        if (s.Length is 0)
        {
            result = new ValueList<T>(ImmutableArray<T>.Empty);
            return true;
        }

        return s[0] is '~' ? TryParseTildeSeparated(s, out result) : TryParseLengthPrefixed(s, out result);
    }

    /// <inheritdoc/>
    static bool IParsable<ValueList<T>>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out ValueList<T> result) => TryParse(s, out result);

    /// <summary>
    /// Returns the encoded string representation of the items.
    /// </summary>
    public override string ToString() => _toString ?? string.Empty;

    /// <inheritdoc/>
    public bool Equals(ValueList<T> other) => AsSpan().SequenceEqual(other.AsSpan());

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ValueList<T> other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        HashCode hash = default;
        foreach (var item in Value)
            hash.Add(item);

        return hash.ToHashCode();
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    public IEnumerator<T> GetEnumerator()
    {
        foreach (var item in Value)
            yield return item;
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Determines whether two <see cref="ValueList{T}"/> instances are equal.
    /// </summary>
    public static bool operator ==(ValueList<T> left, ValueList<T> right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="ValueList{T}"/> instances are not equal.
    /// </summary>
    public static bool operator !=(ValueList<T> left, ValueList<T> right) => !left.Equals(right);

    #region Formatting

    private static string FormatItems(ReadOnlySpan<T> items)
    {
        if (items.IsEmpty)
            return string.Empty;

        // Format each item once. If T is whitelisted as tilde-safe we use tilde-separated directly; otherwise we scan the formatted values
        // and fall back to length-prefixed if any value is empty or contains a tilde.
        string[] formatted = new string[items.Length];
        bool useTildeSeparated = true;

        for (int i = 0; i < items.Length; i++)
        {
            string f = RouteValueConverter.Format(items[i]);
            formatted[i] = f;

            if (!IsTildeSafe && useTildeSeparated && f.Contains('~'))
                useTildeSeparated = false;
        }

        return useTildeSeparated ? BuildTildeSeparated(formatted) : BuildLengthPrefixed(formatted);
    }

    /// <summary>
    /// Builds a tilde-separated string from pre-formatted item values. A leading tilde marks the format so the parser can detect it.
    /// </summary>
    private static string BuildTildeSeparated(string[] formatted)
    {
        int totalLength = formatted.Length; // one leading '~' + one '~' per separator = formatted.Length leading/separator tildes
        foreach (string f in formatted)
            totalLength += f.Length;

        var sb = new StringBuilder(totalLength);

        foreach (string f in formatted)
        {
            sb.Append('~');
            sb.Append(f);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Builds a length-prefixed string: <c>{charCount}~{value}</c> per item, concatenated, e.g. <c>5~hello5~world</c>.
    /// </summary>
    private static string BuildLengthPrefixed(string[] formatted)
    {
        var sb = new StringBuilder();

        foreach (string f in formatted)
        {
            sb.Append(f.Length);
            sb.Append('~');
            sb.Append(f);
        }

        return sb.ToString();
    }

    private static bool DetermineTildeSafe()
    {
        Type t = typeof(T);

        if (t.IsEnum)
            return true;

        if (t.IsPrimitive && t != typeof(char))
            return true;

        if (t == typeof(decimal) || t == typeof(BigInteger))
            return true;

        if (t == typeof(Guid) ||
            t == typeof(DateTime) || t == typeof(DateTimeOffset) ||
            t == typeof(DateOnly) || t == typeof(TimeOnly) ||
            t == typeof(TimeSpan))
        {
            return true;
        }

        return false;
    }

    #endregion

    #region Parsing

    private static bool TryParseTildeSeparated(string s, out ValueList<T> result)
    {
        // The input is guaranteed to start with '~'. Strip that marker, then split on '~' to get the items.
        var span = s.AsSpan()[1..];
        var segments = span.Split('~');
        int itemCount = 0;

        foreach (char c in s)
        {
            if (c == '~')
                itemCount++;
        }

        var builder = ImmutableArray.CreateBuilder<T>(itemCount);

        foreach (var segment in segments)
        {
            if (!RouteValueConverter.TryParse<T>(span[segment].ToString(), out var value))
            {
                result = default;
                return false;
            }

            builder.Add(value);
        }

        result = new ValueList<T>(builder.MoveToImmutable());
        return true;
    }

    private static bool TryParseLengthPrefixed(string s, out ValueList<T> result)
    {
        var builder = ImmutableArray.CreateBuilder<T>();
        int i = 0;
        int len = s.Length;

        while (i < len)
        {
            // Read digits for the length prefix.
            int sepIndex = s.IndexOf('~', i);
            if (sepIndex < 0 || sepIndex == i)
            {
                result = default;
                return false;
            }

            if (!int.TryParse(s.AsSpan(i, sepIndex - i), out int fieldLength) || fieldLength < 0)
            {
                result = default;
                return false;
            }

            int valueStart = sepIndex + 1;

            if (valueStart + fieldLength > len)
            {
                result = default;
                return false;
            }

            string field = s.Substring(valueStart, fieldLength);

            if (!RouteValueConverter.TryParse<T>(field, out var value))
            {
                result = default;
                return false;
            }

            builder.Add(value);
            i = valueStart + fieldLength;
        }

        result = new ValueList<T>(builder.DrainToImmutable());
        return true;
    }

    #endregion
}
