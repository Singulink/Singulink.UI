namespace Singulink.UI.Navigation.Utilities;

/// <summary>
/// Represents a value that must be set once and only once.
/// </summary>
public struct RequiredSingleSet<T>
{
    private T _value;
    private bool _isSet;

    /// <summary>
    /// Gets or sets the value. This property must be set once and only once. Getting the value before it is set or setting it more than once will throw an
    /// <see cref="InvalidOperationException"/>.
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
}
