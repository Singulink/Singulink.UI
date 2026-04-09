using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Singulink.UI.Navigation.Generators;

/// <summary>
/// A lightweight wrapper around a backing array that implements structural equality, so it is safe to use as a field on
/// records flowing through the incremental generator pipeline (where the default <see cref="ImmutableArray{T}"/> struct
/// compares reference-equal arrays as unequal on every rebuild).
/// </summary>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IReadOnlyList<T>
{
    public static readonly EquatableArray<T> Empty = new([]);

    private readonly T[]? _array;

    public EquatableArray(T[] array)
    {
        _array = array;
    }

    public EquatableArray(ReadOnlySpan<T> items)
    {
        _array = items.IsEmpty ? [] : items.ToArray();
    }

    public int Length => _array?.Length ?? 0;

    public int Count => Length;

    public bool IsDefaultOrEmpty => _array is null || _array.Length == 0;

    public T this[int index] => (_array ?? throw new ArgumentOutOfRangeException(nameof(index)))[index];

    public bool Equals(EquatableArray<T> other)
    {
        var a = _array;
        var b = other._array;

        if (ReferenceEquals(a, b))
            return true;

        int lenA = a?.Length ?? 0;
        int lenB = b?.Length ?? 0;
        if (lenA != lenB)
            return false;

        var comparer = EqualityComparer<T>.Default;
        for (int i = 0; i < lenA; i++)
        {
            if (!comparer.Equals(a![i], b![i]))
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        if (_array is null)
            return 0;

        // Simple FNV-style hash; avoids dependency on System.HashCode (not available on netstandard2.0).
        unchecked
        {
            int hash = (int)2166136261;
            var comparer = EqualityComparer<T>.Default;
            foreach (var item in _array)
            {
                int itemHash = item is null ? 0 : comparer.GetHashCode(item);
                hash = (hash ^ itemHash) * 16777619;
            }

            return hash;
        }
    }

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)(_array ?? Array.Empty<T>())).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);
}

internal static class EquatableArrayExtensions
{
    public static EquatableArray<T> ToEquatableArray<T>(this ImmutableArray<T> array)
        => array.IsDefaultOrEmpty ? EquatableArray<T>.Empty : new(array.ToArray());

    public static EquatableArray<T> ToEquatableArray<T>(this ImmutableArray<T>.Builder builder)
        => builder.Count == 0 ? EquatableArray<T>.Empty : new(builder.ToArray());

    public static EquatableArray<T> ToEquatableArray<T>(this IEnumerable<T> source) => new(source.ToArray());
}
