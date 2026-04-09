using System.Diagnostics.CodeAnalysis;
using Singulink.UI.Navigation.Utilities;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents an optional path parameter that may or may not have a value. Use this as the view model parameter in view models and route groups when some
/// aliases of the route don't supply the value in the path.
/// </summary>
/// <typeparam name="T">The type of the parameter value.</typeparam>
public readonly struct OptionalPathParam<[DynamicallyAccessedMembers(DAM.PublicDefaultCtor)] T> : ISingleRouteParam<OptionalPathParam<T>>, IEquatable<OptionalPathParam<T>>
    where T : notnull, IParsable<T>, IEquatable<T>
{
    private readonly bool _hasValue;
    private readonly T? _value;
    private readonly string? _toString;

    /// <summary>
    /// Gets a value indicating whether this instance has a value.
    /// </summary>
    [MemberNotNullWhen(true, nameof(_value))]
    [MemberNotNullWhen(true, nameof(_toString))]
    public bool HasValue => _hasValue;

    /// <summary>
    /// Gets the parameter value.
    /// </summary>
    /// <exception cref="InvalidOperationException">The instance does not have a value.</exception>
    public T Value => _value ?? throw new InvalidOperationException("The optional parameter does not have a value.");

    /// <summary>
    /// Gets an <see cref="OptionalPathParam{T}"/> instance with no value.
    /// </summary>
    public static OptionalPathParam<T> None => default;

    /// <summary>
    /// Initializes a new instance of the <see cref="OptionalPathParam{T}"/> struct with the specified value.
    /// </summary>
    /// <param name="value">The parameter value, or <see langword="null"/> for no value.</param>
    public OptionalPathParam(T? value)
    {
        if (value is not null)
        {
            _hasValue = true;
            _value = value;
            _toString = RouteValueConverter.Format(value);
            return;
        }

        _hasValue = false;
        _value = default;
        _toString = null;
    }

    /// <summary>
    /// Converts a value to an <see cref="OptionalPathParam{T}"/>.
    /// </summary>
    public static implicit operator OptionalPathParam<T>(T? value) => new(value);

    /// <summary>
    /// Gets the parameter value if this instance has a value, or the specified default value if this instance has no value.
    /// </summary>
    public T GetValueOrDefault(T defaultValue) => HasValue ? _value : defaultValue;

    /// <inheritdoc/>
    public static bool TryParse(string s, [MaybeNullWhen(false)] out OptionalPathParam<T> value)
    {
        if (!string.IsNullOrEmpty(s) && RouteValueConverter.TryParse(s, out T inner))
        {
            value = new OptionalPathParam<T>(inner);
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Returns the formatted string representation of the value, or <see langword="null"/> if this instance has no value.
    /// </summary>
    public override string? ToString() => _toString;

    /// <inheritdoc/>
    public bool Equals(OptionalPathParam<T> other)
    {
        if (!HasValue)
            return !other.HasValue;

        return other.HasValue && _value.Equals(other._value);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is OptionalPathParam<T> other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HasValue ? _value.GetHashCode() : 0;

    /// <summary>
    /// Determines whether two <see cref="OptionalPathParam{T}"/> instances are equal.
    /// </summary>
    public static bool operator ==(OptionalPathParam<T> left, OptionalPathParam<T> right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="OptionalPathParam{T}"/> instances are not equal.
    /// </summary>
    public static bool operator !=(OptionalPathParam<T> left, OptionalPathParam<T> right) => !left.Equals(right);
}

/// <summary>
/// Provides extension methods for <see cref="OptionalPathParam{T}"/> with value parameter types.
/// </summary>
public static class OptionalPathParamValueExtensions
{
    /// <summary>
    /// Converts an <see cref="OptionalPathParam{T}"/> to a nullable value of the parameter type, treating an instance with no value as <see langword="null"/>.
    /// </summary>
    public static T? ToNullable<T>(this OptionalPathParam<T> optional) where T : struct, IParsable<T>, IEquatable<T> => optional.HasValue ? optional.Value : null;
}

/// <summary>
/// Provides extension methods for <see cref="OptionalPathParam{T}"/> with reference parameter types.
/// </summary>
public static class OptionalPathParamReferenceExtensions
{
    /// <summary>
    /// Converts an <see cref="OptionalPathParam{T}"/> to the parameter value, treating an instance with no value as <see langword="null"/>.
    /// </summary>
    public static T? ToNullable<T>(this OptionalPathParam<T> optional) where T : class, IParsable<T>, IEquatable<T> => optional.HasValue ? optional.Value : null;
}
