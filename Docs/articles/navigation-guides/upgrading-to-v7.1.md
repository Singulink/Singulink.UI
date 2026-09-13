<div class="article">

# Upgrading to 7.1

Version 7.1 adds route pinning and destination-aware guards, and changes two behaviors that existing code may depend on. Nothing stops compiling, but the changes below should be reviewed when upgrading.

## View Models Are No Longer Cached By Default

<xref:Singulink.UI.Navigation.IRoutedViewModelBase.CanBeCached> now defaults to `false`. Previously every view model was retained along with its view when navigated away from (up to the configured cache depth) unless it opted out, which meant its <xref:Singulink.UI.Navigation.IRoutedViewModelBase.OnNavigatedToAsync*> had to handle being called again on the same instance. That contract was easy to overlook and the failures (duplicate subscriptions, repeated setup) were silent, so caching is now opt-in.

Review view models that did not override <xref:Singulink.UI.Navigation.IRoutedViewModelBase.CanBeCached>: any that relied on default caching to preserve state across back navigation (e.g. scroll position or entered but unsaved text) should now return `true` explicitly:

```csharp
// 7.0: cached unless opted out
public partial class FolderPageViewModel : ObservableObject, IRoutedViewModel<long>
{
}

// 7.1: opt in to keep the previous behavior
public partial class FolderPageViewModel : ObservableObject, IRoutedViewModel<long>
{
    public bool CanBeCached => true;
}
```

Existing `CanBeCached => false` overrides are now redundant and can be removed. See [Caching](view-models.md#caching).

## Routes Are Live Views

<xref:Singulink.UI.Navigation.NavigatorRoute> instances obtained from <xref:Singulink.UI.Navigation.INavigator.CurrentRoute> or the navigation stacks now reflect in-place updates made with <xref:Singulink.UI.Navigation.INavigator.UpdateCurrentRoute*> after they were obtained. In 7.0 an anchor update replaced the route instance, so a previously obtained route kept the old anchor. Code that held on to a route as a snapshot should capture <xref:Singulink.UI.Navigation.NavigatorRoute.ToString> instead. See [The Current Route](navigating.md#the-current-route).

## New in 7.1

- <xref:Singulink.UI.Navigation.INavigator.PinCurrentRoute> and <xref:Singulink.UI.Navigation.RoutePin> keep a view alive with its state intact while the user navigates elsewhere, together with <xref:Singulink.UI.Navigation.IRoutedViewModelBase.CanBePinned> and <xref:Singulink.UI.Navigation.INavigator.NavigateAsync(Singulink.UI.Navigation.NavigatorRoute)> for returning to it. See [Pinning a Route](navigating.md#pinning-a-route).
- <xref:Singulink.UI.Navigation.NavigatingArgs.TargetRoute> lets guards decide based on the destination of a navigation. See [Guards and Redirects](guards-and-redirects.md).

</div>
