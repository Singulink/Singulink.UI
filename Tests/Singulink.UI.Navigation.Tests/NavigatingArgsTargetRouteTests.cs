using PrefixClassName.MsTest;
using Shouldly;
using Singulink.UI.Navigation.Testing;
using Singulink.UI.Navigation.Tests.TestSupport;

namespace Singulink.UI.Navigation.Tests;

[PrefixTestClass]
public class NavigatingArgsTargetRouteTests
{
    [TestMethod]
    public void NavigatingAway_ExposesTargetRoute()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home");

            NavigatorRoute? target = null;
            nav.ActiveViewModel<HomeVm>().OnNavigatingAwayCallback = args => {
                target = args.TargetRoute;
                return Task.CompletedTask;
            };

            await nav.NavigateAsync("items/5#top");

            target.ShouldNotBeNull();
            target.ToString().ShouldBe("items/5#top");
            target.ShouldBeSameAs(nav.CurrentRoute);
        });
    }

    [TestMethod]
    public void RouteNavigating_OnRetainedParent_ExposesTargetRoute()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("main/details");

            NavigatorRoute? target = null;
            nav.ActiveViewModel<MainVm>().OnRouteNavigatingCallback = args => {
                target = args.TargetRoute;
                return Task.CompletedTask;
            };

            await nav.NavigateAsync("main/other");

            target.ShouldNotBeNull();
            target.Path.ShouldBe("main/other");
            target.Parts[^1].RoutePart.ShouldBeSameAs(Routes.MainOther);
        });
    }

    [TestMethod]
    public void Back_ExposesBackStackRouteAsTarget()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home#a");
            await nav.NavigateAsync("items/1");

            var expected = nav.GetBackStack()[0];
            NavigatorRoute? target = null;

            nav.ActiveViewModel<ItemVm>().OnNavigatingAwayCallback = args => {
                target = args.TargetRoute;
                return Task.CompletedTask;
            };

            await nav.GoBackAsync();

            target.ShouldNotBeNull();
            target.ShouldBeSameAs(expected);
            target.ToString().ShouldBe("home#a");
        });
    }

    [TestMethod]
    public void Refresh_ExposesCurrentRouteAsTarget()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home");

            var current = nav.CurrentRoute;
            NavigatorRoute? target = null;

            // A refresh retains every route item, so only the route-navigating hook runs.
            nav.ActiveViewModel<HomeVm>().OnRouteNavigatingCallback = args => {
                target = args.TargetRoute;
                return Task.CompletedTask;
            };

            await nav.RefreshAsync();
            target.ShouldBeSameAs(current);
        });
    }

    [TestMethod]
    public void ShutDown_TargetRouteIsNull()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home");

            bool invoked = false;
            NavigatorRoute? target = null;

            nav.ActiveViewModel<HomeVm>().OnNavigatingAwayCallback = args => {
                invoked = true;
                target = args.TargetRoute;
                return Task.CompletedTask;
            };

            (await nav.TryShutDownAsync()).ShouldBeTrue();

            invoked.ShouldBeTrue();
            target.ShouldBeNull();
        });
    }

    [TestMethod]
    public void CancelledNavigation_TargetRouteNotAdopted()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home");

            var vm = nav.ActiveViewModel<HomeVm>();
            vm.OnNavigatingAwayCallback = args => {
                args.Cancel = args.TargetRoute?.Path == "items/1";
                return Task.CompletedTask;
            };

            (await nav.NavigateAsync("items/1")).ShouldBe(NavigationResult.Cancelled);
            nav.CurrentRoute.Path.ShouldBe("home");

            (await nav.NavigateAsync("items/2")).ShouldBe(NavigationResult.Success);
            nav.CurrentRoute.Path.ShouldBe("items/2");
        });
    }

    private static TestNavigator BuildNav() => new(b =>
    {
        b.MapViewModel<HomeVm>();
        b.MapViewModel<ItemVm>();
        b.MapViewModel<MainVm>();
        b.MapViewModel<DetailsVm>();
        b.MapViewModel<OtherVm>();

        b.AddRoute(Routes.Home);
        b.AddRoute(Routes.Item);
        b.AddRoute(Routes.Main);
        b.AddRoute(Routes.MainDetails);
        b.AddRoute(Routes.MainOther);
    });

    public static class Routes
    {
        public static readonly RootRoutePart<HomeVm> Home = Route.Build("home").Root<HomeVm>();
        public static readonly RootRoutePart<ItemVm, int> Item = Route.Build<int>(p => $"items/{p}").Root<ItemVm>();
        public static readonly RootRoutePart<MainVm> Main = Route.Build("main").Root<MainVm>();
        public static readonly ChildRoutePart<MainVm, DetailsVm> MainDetails = Route.Build("details").Child<MainVm, DetailsVm>();
        public static readonly ChildRoutePart<MainVm, OtherVm> MainOther = Route.Build("other").Child<MainVm, OtherVm>();
    }

    public class HomeVm : RecordedLifecycleViewModel, IRoutedViewModel
    {
    }

    public class ItemVm : RecordedLifecycleViewModel, IRoutedViewModel<int>
    {
    }

    public class MainVm : RecordedLifecycleViewModel, IRoutedViewModel
    {
    }

    public class DetailsVm : RecordedLifecycleViewModel, IRoutedViewModel
    {
    }

    public class OtherVm : RecordedLifecycleViewModel, IRoutedViewModel
    {
    }
}
