using PrefixClassName.MsTest;
using Shouldly;
using Singulink.UI.Navigation.Testing;
using Singulink.UI.Navigation.Tests.TestSupport;

namespace Singulink.UI.Navigation.Tests;

[PrefixTestClass]
public class NavigatorLifecycleOrderTests
{
    [TestMethod]
    public void FirstNavigation_FiresOnNavigatedTo_ThenOnRouteNavigated()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("a");

            var vm = nav.ActiveViewModel<AVm>();
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
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("a");
            var aVm = nav.ActiveViewModel<AVm>();
            aVm.Events.Clear();

            await nav.NavigateAsync("b");
            var bVm = nav.ActiveViewModel<BVm>();

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
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("a");
            var vm = nav.ActiveViewModel<AVm>();
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
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("a");
            await nav.NavigateAsync("b");

            await nav.GoBackAsync();

            var current = nav.ActiveViewModel<AVm>();
            current.Events[^1].NavigationType.ShouldBe(NavigationType.Back);
        });
    }

    [TestMethod]
    public void HasChildNavigation_TrueOnParent_FalseOnChild()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("p/c");

            var parentVm = nav.ActiveViewModel<ParentVm>();
            var childVm = nav.ActiveViewModel<ChildVm>();

            parentVm.Events.First(e => e.Kind == LifecycleEventKind.NavigatedTo).HasChildNavigation.ShouldBeTrue();
            childVm.Events.First(e => e.Kind == LifecycleEventKind.NavigatedTo).HasChildNavigation.ShouldBeFalse();
        });
    }

    [TestMethod]
    public void CancelOnNavigatingAway_BlocksNavigation()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("a");
            var aVm = nav.ActiveViewModel<AVm>();
            aVm.CancelOnNavigatingAway = true;

            var result = await nav.NavigateAsync("b");

            result.ShouldBe(NavigationResult.Cancelled);
            nav.CurrentRoute.ToString().ShouldBe("a");
            nav.ActiveViewModels[0].ShouldBeSameAs(aVm);
        });
    }

    [TestMethod]
    public void CancelOnRouteNavigating_BlocksRefresh()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("a");
            var vm = nav.ActiveViewModel<AVm>();
            vm.CancelOnRouteNavigating = true;

            (await nav.RefreshAsync()).ShouldBe(NavigationResult.Cancelled);
        });
    }

    [TestMethod]
    public void RedirectOnNavigatedTo_TriggersAlternateRoute()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = new TestNavigator(b =>
            {
                b.MapViewModel<RedirectVm>();
                b.MapViewModel<TargetVm>();
                b.AddRoute(Route.Build("source").Root<RedirectVm>());
                b.AddRoute(Route.Build("target").Root<TargetVm>());
            });

            await nav.NavigateAsync("source");

            nav.CurrentRoute.ToString().ShouldBe("target");
            nav.ActiveViewModels[0].ShouldBeOfType<TargetVm>();
        });
    }

    private static TestNavigator BuildNav() => new(b =>
    {
        b.MapViewModel<AVm>();
        b.MapViewModel<BVm>();
        b.MapViewModel<ParentVm>();
        b.MapViewModel<ChildVm>();

        b.AddRoute(Route.Build("a").Root<AVm>());
        b.AddRoute(Route.Build("b").Root<BVm>());
        b.AddRoute(Route.Build("p").Root<ParentVm>());
        b.AddRoute(Route.Build("c").Child<ParentVm, ChildVm>());
    });

    public class AVm : RecordedLifecycleViewModel, IRoutedViewModel { }

    public class BVm : RecordedLifecycleViewModel, IRoutedViewModel { }

    public class ParentVm : RecordedLifecycleViewModel, IRoutedViewModel { }

    public class ChildVm : RecordedLifecycleViewModel, IRoutedViewModel { }


    public class RedirectVm : RecordedLifecycleViewModel, IRoutedViewModel
    {
        public RedirectVm()
        {
            RedirectOnNavigatedTo = Redirect.Navigate("target");
        }
    }

    public class TargetVm : RecordedLifecycleViewModel, IRoutedViewModel { }
}
