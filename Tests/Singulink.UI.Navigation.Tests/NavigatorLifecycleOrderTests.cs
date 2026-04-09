using PrefixClassName.MsTest;
using Shouldly;
using Singulink.UI.Navigation.Tests.TestSupport;

namespace Singulink.UI.Navigation.Tests;

[PrefixTestClass]
public class NavigatorLifecycleOrderTests
{
    [TestMethod]
    public void FirstNavigation_FiresOnNavigatedTo_ThenOnRouteNavigated()
    {
        AsyncContextTest.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("a");

            var vm = (AVm)((FakeView)nav.RootViewNavigator.ActiveView!).DataContext!;
            vm.Events.Select(e => e.Kind).ShouldBe(new[]
            {
                LifecycleEventKind.NavigatedTo,
                LifecycleEventKind.RouteNavigated,
            });
        });
    }

    [TestMethod]
    public void NavigateBetweenRoots_FiresAwayThenTo()
    {
        AsyncContextTest.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("a");
            var aVm = (AVm)((FakeView)nav.RootViewNavigator.ActiveView!).DataContext!;
            aVm.Events.Clear();

            await nav.NavigateAsync("b");
            var bVm = (BVm)((FakeView)nav.RootViewNavigator.ActiveView!).DataContext!;

            // Navigating-away on previous VM
            aVm.Events.Select(e => e.Kind).ShouldContain(LifecycleEventKind.RouteNavigating);
            aVm.Events.Select(e => e.Kind).ShouldContain(LifecycleEventKind.NavigatingAway);
            aVm.Events.Select(e => e.Kind).ShouldContain(LifecycleEventKind.NavigatedAway);

            // To/Route on new VM
            bVm.Events.Select(e => e.Kind).ShouldBe(new[]
            {
                LifecycleEventKind.NavigatedTo,
                LifecycleEventKind.RouteNavigated,
            });
        });
    }

    [TestMethod]
    public void Refresh_FiresRouteNavigating_ThenRouteNavigated_OnSameInstance()
    {
        AsyncContextTest.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("a");
            var vm = (AVm)((FakeView)nav.RootViewNavigator.ActiveView!).DataContext!;
            vm.Events.Clear();

            await nav.RefreshAsync();

            vm.Events.Select(e => e.Kind).ShouldBe(new[]
            {
                LifecycleEventKind.RouteNavigating,
                LifecycleEventKind.RouteNavigated,
            });
            vm.Events[1].NavigationType.ShouldBe(NavigationType.Refresh);
        });
    }

    [TestMethod]
    public void GoBack_NavigationType_IsBack()
    {
        AsyncContextTest.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("a");
            await nav.NavigateAsync("b");

            await nav.GoBackAsync();

            var current = (AVm)((FakeView)nav.RootViewNavigator.ActiveView!).DataContext!;
            current.Events[^1].NavigationType.ShouldBe(NavigationType.Back);
        });
    }

    [TestMethod]
    public void HasChildNavigation_TrueOnParent_FalseOnChild()
    {
        AsyncContextTest.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("p/c");

            var parentVm = (ParentVm)((FakeView)nav.RootViewNavigator.ActiveView!).DataContext!;
            var parentView = (ParentView)nav.RootViewNavigator.ActiveView!;
            var childVm = (ChildVm)((FakeView)parentView.ChildNavigator.ActiveView!).DataContext!;

            parentVm.Events.First(e => e.Kind == LifecycleEventKind.NavigatedTo).HasChildNavigation.ShouldBeTrue();
            childVm.Events.First(e => e.Kind == LifecycleEventKind.NavigatedTo).HasChildNavigation.ShouldBeFalse();
        });
    }

    [TestMethod]
    public void CancelOnNavigatingAway_BlocksNavigation()
    {
        AsyncContextTest.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("a");
            var aVm = (AVm)((FakeView)nav.RootViewNavigator.ActiveView!).DataContext!;
            aVm.CancelOnNavigatingAway = true;

            var result = await nav.NavigateAsync("b");

            result.ShouldBe(NavigationResult.Cancelled);
            nav.CurrentRoute.ToString().ShouldBe("a");
            ((FakeView)nav.RootViewNavigator.ActiveView!).DataContext.ShouldBeSameAs(aVm);
        });
    }

    [TestMethod]
    public void CancelOnRouteNavigating_BlocksRefresh()
    {
        AsyncContextTest.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("a");
            var vm = (AVm)((FakeView)nav.RootViewNavigator.ActiveView!).DataContext!;
            vm.CancelOnRouteNavigating = true;

            (await nav.RefreshAsync()).ShouldBe(NavigationResult.Cancelled);
        });
    }

    [TestMethod]
    public void RedirectOnNavigatedTo_TriggersAlternateRoute()
    {
        AsyncContextTest.Run(async () =>
        {
            var nav = new TestNavigator(b =>
            {
                b.MapRoutedView<RedirectVm, FakeView>();
                b.MapRoutedView<TargetVm, FakeView>();
                b.AddRoute(Route.Build("source").Root<RedirectVm>());
                b.AddRoute(Route.Build("target").Root<TargetVm>());
            });

            await nav.NavigateAsync("source");

            nav.CurrentRoute.ToString().ShouldBe("target");
            ((FakeView)nav.RootViewNavigator.ActiveView!).DataContext.ShouldBeOfType<TargetVm>();
        });
    }

    private static TestNavigator BuildNav() => new(b =>
    {
        b.MapRoutedView<AVm, FakeView>();
        b.MapRoutedView<BVm, FakeView>();
        b.MapRoutedView<ParentVm, ParentView>();
        b.MapRoutedView<ChildVm, FakeView>();

        b.AddRoute(Route.Build("a").Root<AVm>());
        b.AddRoute(Route.Build("b").Root<BVm>());
        b.AddRoute(Route.Build("p").Root<ParentVm>());
        b.AddRoute(Route.Build("c").Child<ParentVm, ChildVm>());
    });

    public class AVm : RecordedLifecycleViewModel, IRoutedViewModel { }

    public class BVm : RecordedLifecycleViewModel, IRoutedViewModel { }

    public class ParentVm : RecordedLifecycleViewModel, IRoutedViewModel { }

    public class ChildVm : RecordedLifecycleViewModel, IRoutedViewModel { }

    public class ParentView : FakeParentView { }

    public class RedirectVm : RecordedLifecycleViewModel, IRoutedViewModel
    {
        public RedirectVm()
        {
            RedirectOnNavigatedTo = Redirect.Navigate("target");
        }
    }

    public class TargetVm : RecordedLifecycleViewModel, IRoutedViewModel { }
}
