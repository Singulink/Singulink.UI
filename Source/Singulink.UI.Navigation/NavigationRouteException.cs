namespace Singulink.UI.Navigation;

/// <summary>
/// Represents errors that occur during navigation route processing.
/// </summary>
public class NavigationRouteException : Exception
{
    private const string DefaultMessage = "An error occurred while processing the navigation route.";

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationRouteException"/> class.
    /// </summary>
    public NavigationRouteException() : base(DefaultMessage)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationRouteException"/> class with a specified error message.
    /// </summary>
    public NavigationRouteException(string? message) : base(message ?? DefaultMessage)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationRouteException"/> class with a specified error message and a reference to the inner exception
    /// that is the cause of this exception.
    /// </summary>
    public NavigationRouteException(string? message, Exception? innerException) : base(message ?? DefaultMessage, innerException)
    {
    }
}
