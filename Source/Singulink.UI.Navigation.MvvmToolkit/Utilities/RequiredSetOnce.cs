using System.Diagnostics.CodeAnalysis;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a value that must be set before it can be retrieved and can only be set once.
/// </summary>
internal struct RequiredSetOnce<T>
{
    private T _value;
    private bool _isSet;

    /// <summary>
    /// Gets or sets the value. This property can only be set once. Getting the value before it is set or setting it more than once will throw an <see
    /// cref="InvalidOperationException"/>.
    /// </summary>
    public T Value
    {
        readonly get => _isSet ? _value : throw new InvalidOperationException("Value has not been set.");
        set {
            if (_isSet)
                throw new InvalidOperationException("Value has already been set.");

            _value = value;
            _isSet = true;
        }
    }

    /// <summary>
    /// Gets the value if it has been set, otherwise the default value of <typeparamref name="T"/>.
    /// </summary>
    [MaybeNull]
    public readonly T ValueOrDefault => _isSet ? _value : default;
}
