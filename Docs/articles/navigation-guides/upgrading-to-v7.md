<div class="article">

# Upgrading to 7.0

Version 7.0 makes a small number of breaking changes. This page lists each one with what it looked like before, so readers still on 6.x can map the current guides to their code. There are no separate 6.x guides; everything else in the documentation applies to both versions.

## Multi-Level Routes Are Chained

Navigating, partial navigation, path checks and redirects with more than one route part now take a single chained route built with <xref:Singulink.UI.Navigation.ConcreteRouteExtensions.Then*> (see [Navigating](navigating.md)). In 6.x the parts were passed as separate arguments to generic overloads, which were limited to three levels:

```csharp
// 6.x
await this.Navigator.NavigateAsync(
    Routes.RepoRoot.ToConcrete("my-repo"),
    Routes.Repo.HomePage);

await this.Navigator.NavigatePartialAsync<RepoRootModel>(
    Routes.Repo.DocumentPage.ToConcrete(documentParams),
    Routes.Repo.DocumentPage.History);

bool inRepoHome = this.Navigator.CurrentPathStartsWith(
    Routes.RepoRoot.ToConcrete("my-repo"),
    Routes.Repo.HomePage);

args.Redirect = Redirect.Navigate(
    Routes.RepoRoot.ToConcrete("my-repo"),
    Routes.Repo.HomePage);

// 7.0
await this.Navigator.NavigateAsync(
    Routes.RepoRoot.ToConcrete("my-repo")
        .Then(Routes.Repo.HomePage));

await this.Navigator.NavigatePartialAsync<RepoRootModel>(
    Routes.Repo.DocumentPage.ToConcrete(documentParams)
        .Then(Routes.Repo.DocumentPage.History));

bool inRepoHome = this.Navigator.CurrentPathStartsWith(
    Routes.RepoRoot.ToConcrete("my-repo")
        .Then(Routes.Repo.HomePage));

args.Redirect = Redirect.Navigate(
    Routes.RepoRoot.ToConcrete("my-repo")
        .Then(Routes.Repo.HomePage));
```

Calls with a single route part are unchanged, and there is no longer a depth limit. <xref:Singulink.UI.Navigation.INavigator.CurrentPathStartsWith(Singulink.UI.Navigation.IConcreteRootRoutePart)> was added for checking a root route part on its own.

## Host Operations Moved Off INavigator

`HandleSystemBackRequest`, `HandleSystemForwardRequest` and `TryShutDownAsync` are host-level operations invoked by window or browser integration rather than by view models, so they are no longer part of <xref:Singulink.UI.Navigation.INavigator>. They remain available on the concrete navigator (<xref:Singulink.UI.Navigation.NavigatorCore>), which is what hosting code holds. The WinUI navigator's <xref:Singulink.UI.Navigation.WinUI.Navigator.HookSystemNavigationRequests> and <xref:Singulink.UI.Navigation.WinUI.Navigator.HookWindowClosedEvents*> helpers are unaffected.

## IDialogPresenter Additions

<xref:Singulink.UI.Navigation.IDialogPresenter> gained <xref:Singulink.UI.Navigation.IDialogPresenter.CanShowDialog>, <xref:Singulink.UI.Navigation.IDialogPresenter.RootServices> and <xref:Singulink.UI.Navigation.IDialogPresenter.TaskRunner>. The latter two moved down from <xref:Singulink.UI.Navigation.INavigator> and <xref:Singulink.UI.Navigation.IDialogNavigator>, so existing call sites compile unchanged; only custom implementations of the interface need to add the members. Dialog view models can now reach root services through `this.Navigator.RootServices` instead of needing them passed in, and <xref:Singulink.UI.Navigation.IDialogPresenter.CreateDialogViewModel*> creates dialog view models with constructor injection (see [Dialogs](dialogs.md#creating-dialogs-with-injected-services)). Dialog view models can also register child services for nested dialogs with `SetChildService`.

## Nested Dialogs Stay Visible

In 6.x a parent dialog was hidden while a nested dialog was showing and re-shown when it closed. In 7.0 the parent stays visible underneath the nested dialog, dimmed and non-interactive, keeps its state, and regains focus when the nested dialog closes (see [Dialogs](dialogs.md#nested-dialogs)). Because the parent is no longer re-shown, view models can no longer rely on any side effects of that re-show; <xref:Singulink.UI.Navigation.IDialogViewModel.OnDialogShownAsync> was never re-invoked in either version.

The rules are unchanged: only the top dialog can show a nested dialog or be closed, and closing a dialog underneath a showing nested dialog throws.

## Testing Package

A new **Singulink.UI.Navigation.Testing** package provides an in-memory navigator for unit testing view models without a UI framework (see [Testing View Models](testing.md)). This is additive; nothing in existing projects changes.

</div>
