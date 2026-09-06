#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Singulink.UI.Navigation.Testing;

/// <summary>
/// Base type for the events recorded by <see cref="TestNavigator.Events"/>, in the order they occurred.
/// </summary>
public abstract record NavigatorEvent;

/// <summary>
/// A navigation started.
/// </summary>
public sealed record NavigationStartedEvent(NavigationType NavigationType, NavigatorRoute Route) : NavigatorEvent;

/// <summary>
/// A navigation completed with the specified result.
/// </summary>
public sealed record NavigationCompletedEvent(NavigationType NavigationType, NavigatorRoute Route, NavigationResult Result) : NavigatorEvent;

/// <summary>
/// A view model requested a redirect during navigation. The redirect is executed immediately after this event.
/// </summary>
public sealed record NavigationRedirectedEvent(IRoutedViewModelBase ViewModel, Redirect Redirect) : NavigatorEvent;

/// <summary>
/// A routed view model was created for a route.
/// </summary>
public sealed record ViewModelCreatedEvent(IRoutedViewModelBase ViewModel) : NavigatorEvent;

/// <summary>
/// A routed view model became the active view model at its level of the view hierarchy.
/// </summary>
public sealed record ViewModelActivatedEvent(IRoutedViewModelBase ViewModel) : NavigatorEvent;

/// <summary>
/// A routed view model stopped being the active view model at its level of the view hierarchy.
/// </summary>
public sealed record ViewModelDeactivatedEvent(IRoutedViewModelBase ViewModel) : NavigatorEvent;

/// <summary>
/// A lifecycle method is about to be invoked on a routed view model.
/// </summary>
public sealed record ViewModelLifecycleEvent(IRoutedViewModelBase ViewModel, ViewModelLifecycleStage Stage) : NavigatorEvent;

/// <summary>
/// A dialog was shown. <see cref="ParentViewModel"/> is the dialog it was shown from, or <see langword="null"/> for a top-level dialog.
/// </summary>
public sealed record DialogShownEvent(IDialogViewModel ViewModel, IDialogViewModel? ParentViewModel) : NavigatorEvent;

/// <summary>
/// A dialog was closed. The result (if any) is available through the dialog view model and the task returned by <c>ShowDialogAsync</c>.
/// </summary>
public sealed record DialogClosedEvent(IDialogViewModel ViewModel) : NavigatorEvent;

/// <summary>
/// A dismiss request (equivalent to the escape key or system back) was delivered to the top dialog.
/// </summary>
public sealed record DialogDismissRequestedEvent(IDialogViewModel ViewModel) : NavigatorEvent;

/// <summary>
/// A layer became covered by a dialog shown above it, or was uncovered when that dialog closed. <see cref="ViewModel"/> is the covered dialog, or
/// <see langword="null"/> for the root view.
/// </summary>
public sealed record LayerCoveredEvent(IDialogViewModel? ViewModel, bool IsCovered) : NavigatorEvent;

/// <summary>
/// Focus was restored to a dialog after a dialog above it closed.
/// </summary>
public sealed record DialogFocusRestoredEvent(IDialogViewModel ViewModel) : NavigatorEvent;

/// <summary>
/// The busy state of a task runner changed. <see cref="DialogViewModel"/> identifies the dialog whose task runner changed, or <see langword="null"/>
/// for the navigator's root task runner.
/// </summary>
public sealed record BusyChangedEvent(IDialogViewModel? DialogViewModel, bool IsBusy) : NavigatorEvent;
