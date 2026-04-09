namespace Singulink.UI.Navigation.Tests.TestSupport;

/// <summary>
/// Records every lifecycle hook invocation on a view model so tests can assert ordering.
/// </summary>
/// <remarks>
/// Methods are <c>public virtual</c> implicit implementations of <see cref="IRoutedViewModelBase"/>; subclasses can override them and call <c>base</c>.
/// </remarks>
public class RecordedLifecycleViewModel : IRoutedViewModelBase
{
    public List<LifecycleEvent> Events { get; } = [];

    public bool CancelOnNavigatingAway { get; set; }

    public bool CancelOnRouteNavigating { get; set; }

    public Redirect? RedirectOnNavigatedTo { get; set; }

    public Redirect? RedirectOnRouteNavigated { get; set; }

    public Func<NavigationArgs, Task>? OnNavigatedToCallback { get; set; }

    public Func<NavigationArgs, Task>? OnRouteNavigatedCallback { get; set; }

    public bool CanBeCachedValue { get; set; } = true;

    public virtual bool CanBeCached => CanBeCachedValue;

    public virtual Task OnNavigatedToAsync(NavigationArgs args)
    {
        Events.Add(new LifecycleEvent(LifecycleEventKind.NavigatedTo, args.NavigationType, args.HasChildNavigation));

        if (RedirectOnNavigatedTo is not null)
            args.Redirect = RedirectOnNavigatedTo;

        return OnNavigatedToCallback?.Invoke(args) ?? Task.CompletedTask;
    }

    public virtual Task OnRouteNavigatedAsync(NavigationArgs args)
    {
        Events.Add(new LifecycleEvent(LifecycleEventKind.RouteNavigated, args.NavigationType, args.HasChildNavigation));

        if (RedirectOnRouteNavigated is not null)
            args.Redirect = RedirectOnRouteNavigated;

        return OnRouteNavigatedCallback?.Invoke(args) ?? Task.CompletedTask;
    }

    public virtual Task OnNavigatingAwayAsync(NavigatingArgs args)
    {
        Events.Add(new LifecycleEvent(LifecycleEventKind.NavigatingAway, args.NavigationType, false));

        if (CancelOnNavigatingAway)
            args.Cancel = true;

        return Task.CompletedTask;
    }

    public virtual Task OnRouteNavigatingAsync(NavigatingArgs args)
    {
        Events.Add(new LifecycleEvent(LifecycleEventKind.RouteNavigating, args.NavigationType, false));

        if (CancelOnRouteNavigating)
            args.Cancel = true;

        return Task.CompletedTask;
    }

    public virtual Task OnNavigatedAwayAsync()
    {
        Events.Add(new LifecycleEvent(LifecycleEventKind.NavigatedAway, NavigationType.New, false));
        return Task.CompletedTask;
    }
}

public readonly record struct LifecycleEvent(LifecycleEventKind Kind, NavigationType NavigationType, bool HasChildNavigation);

public enum LifecycleEventKind
{
    NavigatedTo,
    RouteNavigated,
    NavigatingAway,
    RouteNavigating,
    NavigatedAway,
}
