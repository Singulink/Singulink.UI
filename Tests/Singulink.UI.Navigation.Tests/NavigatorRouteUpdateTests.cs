using System.ComponentModel;
using PrefixClassName.MsTest;
using Shouldly;
using Singulink.UI.Navigation.Testing;
using Singulink.UI.Navigation.Tests.TestSupport;

namespace Singulink.UI.Navigation.Tests;

[PrefixTestClass]
public class NavigatorRouteUpdateTests
{
    [TestMethod]
    public void UpdateAnchor_UpdatesCurrentRouteInPlace()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home");

            var route = nav.CurrentRoute;
            nav.UpdateCurrentRoute("section-2");

            nav.CurrentRoute.ShouldBeSameAs(route);
            route.Anchor.ShouldBe("section-2");
            route.ToString().ShouldBe("home#section-2");
        });
    }

    [TestMethod]
    public void UpdateAnchor_RaisesCurrentRouteChanged()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home");

            var changed = new List<string?>();
            nav.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            nav.UpdateCurrentRoute("section-2");
            changed.ShouldBe([nameof(INavigator.CurrentRoute)]);

            changed.Clear();
            nav.UpdateCurrentRoute("section-2");
            changed.ShouldBeEmpty();
        });
    }

    [TestMethod]
    public void UpdateAnchor_DoesNotFireLifecycleEvents()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home");

            var vm = nav.ActiveViewModel<HomeVm>();
            int eventCount = vm.Events.Count;

            nav.UpdateCurrentRoute("section-2");
            vm.Events.Count.ShouldBe(eventCount);
        });
    }

    [TestMethod]
    public void UpdateLeafPart_UpdatesPreviouslyObtainedRoute()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("items/1#top");

            var route = nav.CurrentRoute;
            var vm = nav.ActiveViewModel<ItemVm>();

            nav.UpdateCurrentRoute(Routes.Item.ToConcrete(2));

            nav.CurrentRoute.ShouldBeSameAs(route);
            route.Path.ShouldBe("items/2");
            route.Parts[^1].Path.ShouldBe("items/2");
            route.Anchor.ShouldBeNull();
            route.ToString().ShouldBe("items/2");
            nav.ActiveViewModel<ItemVm>().ShouldBeSameAs(vm);
        });
    }

    [TestMethod]
    public void UpdateLeafPart_WithAnchor_RaisesCurrentRouteChangedOnce()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("items/1");

            var changed = new List<string?>();
            nav.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            nav.UpdateCurrentRoute(Routes.Item.ToConcrete(2), "bottom");

            changed.ShouldBe([nameof(INavigator.CurrentRoute)]);
            nav.CurrentRoute.ToString().ShouldBe("items/2#bottom");
        });
    }

    [TestMethod]
    public void UpdateLeafPart_SamePartAndAnchor_NoNotification()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("items/1#top");

            var changed = new List<string?>();
            nav.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            nav.UpdateCurrentRoute(Routes.Item.ToConcrete(1), "top");
            changed.ShouldBeEmpty();
        });
    }

    [TestMethod]
    public void UpdateLeafPart_DifferentViewModelType_Throws()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home");

            Should.Throw<ArgumentException>(() => nav.UpdateCurrentRoute(Routes.Item.ToConcrete(1)));
            nav.CurrentRoute.ToString().ShouldBe("home");
        });
    }

    [TestMethod]
    public void Update_BeforeFirstNavigation_Throws()
    {
        NavigationTestContext.Run(() =>
        {
            var nav = BuildNav();

            Should.Throw<InvalidOperationException>(() => nav.UpdateCurrentRoute("x"));
            Should.Throw<InvalidOperationException>(() => nav.UpdateCurrentRoute(Routes.Item.ToConcrete(1)));

            return Task.CompletedTask;
        });
    }

    [TestMethod]
    public void UpdateAnchor_DoesNotAffectOtherStackEntries()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home#one");
            await nav.NavigateAsync("items/1");

            var previous = nav.GetBackStack()[0];

            nav.UpdateCurrentRoute("two");

            previous.ToString().ShouldBe("home#one");
            nav.CurrentRoute.ToString().ShouldBe("items/1#two");
        });
    }

    private static TestNavigator BuildNav() => new(b =>
    {
        b.MapViewModel<HomeVm>();
        b.MapViewModel<ItemVm>();

        b.AddRoute(Routes.Home);
        b.AddRoute(Routes.Item);
    });

    public static class Routes
    {
        public static readonly RootRoutePart<HomeVm> Home = Route.Build("home").Root<HomeVm>();
        public static readonly RootRoutePart<ItemVm, int> Item = Route.Build<int>(p => $"items/{p}").Root<ItemVm>();
    }

    public class HomeVm : RecordedLifecycleViewModel, IRoutedViewModel
    {
    }

    public class ItemVm : RecordedLifecycleViewModel, IRoutedViewModel<int>
    {
    }
}
