using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents an immutable byte array parameter that is Base64-encoded for use in route paths and query strings.
/// </summary>
public readonly struct Base64Bytes : IParsable<Base64Bytes>, IEquatable<Base64Bytes>
{
    private const int StackAllocThreshold = 512;

    private readonly ImmutableArray<byte> _data;
    private readonly string? _toString;

    /// <summary>
    /// Gets the immutable byte array value.
    /// </summary>
    public ImmutableArray<byte> Value => _data.IsDefault ? [] : _data;

    /// <summary>
    /// Initializes a new instance of the <see cref="Base64Bytes"/> struct by copying the specified span.
    /// </summary>
    /// <param name="data">The byte span data to copy.</param>
    public Base64Bytes(ReadOnlySpan<byte> data)
    {
        _data = [.. data];
        _toString = Convert.ToBase64String(data);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Base64Bytes"/> struct with the specified immutable array.
    /// </summary>
    /// <param name="data">The immutable byte array data.</param>
    public Base64Bytes(ImmutableArray<byte> data)
    {
        _data = data;
        _toString = Convert.ToBase64String(Value.AsSpan());
    }

    /// <summary>
    /// Converts an <see cref="ImmutableArray{T}"/> to a <see cref="Base64Bytes"/> without copying.
    /// </summary>
    public static implicit operator Base64Bytes(ImmutableArray<byte> data) => new(data);

    /// <summary>
    /// Converts a <see cref="Base64Bytes"/> to an <see cref="ImmutableArray{T}"/> without copying.
    /// </summary>
    public static implicit operator ImmutableArray<byte>(Base64Bytes param) => param.Value;

    /// <summary>
    /// Converts a <see cref="Base64Bytes"/> to a <see cref="ReadOnlySpan{T}"/> without copying.
    /// </summary>
    public static implicit operator ReadOnlySpan<byte>(Base64Bytes param) => param.AsSpan();

    /// <summary>
    /// Converts a <see cref="Base64Bytes"/> to a <see cref="ReadOnlyMemory{T}"/> without copying.
    /// </summary>
    public static implicit operator ReadOnlyMemory<byte>(Base64Bytes param) => param.Value.AsMemory();

    /// <summary>
    /// Returns the byte data as a <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    public ReadOnlySpan<byte> AsSpan() => Value.AsSpan();

    /// <summary>
    /// Returns the byte data as a <see cref="ReadOnlyMemory{T}"/>.
    /// </summary>
    public ReadOnlyMemory<byte> AsMemory() => Value.AsMemory();

    /// <summary>
    /// Returns a copy of the byte data as a new array.
    /// </summary>
    public byte[] ToArray() => [.. Value];

    /// <summary>
    /// Parses a Base64-encoded string into a <see cref="Base64Bytes"/>.
    /// </summary>
    public static Base64Bytes Parse(string s)
    {
        return new Base64Bytes(Convert.FromBase64String(s));
    }

    /// <inheritdoc/>
    static Base64Bytes IParsable<Base64Bytes>.Parse(string s, IFormatProvider? provider) => Parse(s);

    /// <summary>
    /// Tries to parse a Base64-encoded string into a <see cref="Base64Bytes"/>.
    /// </summary>
    public static bool TryParse([NotNullWhen(true)] string? s, out Base64Bytes result)
    {
        if (s is not null)
        {
            int maxByteCount = ((s.Length * 3) + 3) / 4;
            byte[]? rented = null;

            Span<byte> buffer = maxByteCount <= StackAllocThreshold
                ? stackalloc byte[StackAllocThreshold]
                : (rented = ArrayPool<byte>.Shared.Rent(maxByteCount));

            try
            {
                if (Convert.TryFromBase64String(s, buffer, out int bytesWritten))
                {
                    result = new Base64Bytes(buffer[..bytesWritten]);
                    return true;
                }
            }
            finally
            {
                if (rented is not null)
                    ArrayPool<byte>.Shared.Return(rented);
            }
        }

        result = default;
        return false;
    }

    /// <inheritdoc/>
    static bool IParsable<Base64Bytes>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Base64Bytes result) => TryParse(s, out result);

    /// <summary>
    /// Returns the Base64-encoded string representation of the byte array.
    /// </summary>
    public override string ToString() => _toString ?? string.Empty;

    /// <inheritdoc/>
    public bool Equals(Base64Bytes other) => AsSpan().SequenceEqual(other.AsSpan());

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Base64Bytes other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        HashCode hash = default;
        hash.AddBytes(AsSpan());
        return hash.ToHashCode();
    }

    /// <summary>
    /// Determines whether two <see cref="Base64Bytes"/> instances are equal.
    /// </summary>
    public static bool operator ==(Base64Bytes left, Base64Bytes right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="Base64Bytes"/> instances are not equal.
    /// </summary>
    public static bool operator !=(Base64Bytes left, Base64Bytes right) => !left.Equals(right);
}
