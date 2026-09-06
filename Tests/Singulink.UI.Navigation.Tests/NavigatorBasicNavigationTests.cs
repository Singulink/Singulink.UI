using PrefixClassName.MsTest;
using Shouldly;
using Singulink.UI.Navigation.Testing;
using Singulink.UI.Navigation.Tests.TestSupport;

namespace Singulink.UI.Navigation.Tests;

[PrefixTestClass]
public class NavigatorBasicNavigationTests
{
    [TestMethod]
    public void Navigate_Root_MaterializesViewAndVm()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            (await nav.NavigateAsync("home")).ShouldBe(NavigationResult.Success);

            nav.Events.OfType<ViewModelCreatedEvent>().Count().ShouldBe(1);
            nav.Events.OfType<ViewModelCreatedEvent>().ElementAt(0).ViewModel.ShouldBeOfType<HomeVm>();
            nav.ActiveViewModels[0].ShouldBeOfType<HomeVm>();
            nav.CurrentRoute.ToString().ShouldBe("home");
        });
    }

    [TestMethod]
    public void Navigate_StronglyTyped_Equivalent_ToString_Form()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync(Routes.Home);

            nav.CurrentRoute.ToString().ShouldBe("home");
            nav.ActiveViewModels[0].ShouldBeOfType<HomeVm>();
        });
    }

    [TestMethod]
    public void Navigate_ParameterizedRoute_InjectsParameter()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("items/42");

            var vm = nav.ActiveViewModel<ItemVm>();
            vm.GetParameter().ShouldBe(42);
        });
    }

    [TestMethod]
    public void Navigate_ParentChild_MaterializesBoth_AndChildHostHasChild()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("main/details");

            nav.Events.OfType<ViewModelCreatedEvent>().Count().ShouldBe(2);
            nav.Events.OfType<ViewModelCreatedEvent>().ElementAt(0).ViewModel.ShouldBeOfType<MainVm>();
            nav.Events.OfType<ViewModelCreatedEvent>().ElementAt(1).ViewModel.ShouldBeOfType<DetailsVm>();
            nav.ActiveViewModels[0].ShouldBeOfType<MainVm>();

            nav.ActiveViewModels[1].ShouldBeOfType<DetailsVm>();
        });
    }

    [TestMethod]
    public void Navigate_UnmatchedRoute_Throws()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            var ex = await Should.ThrowAsync<NavigationRouteException>(() => nav.NavigateAsync("does-not-exist"));
            ex.Message.ShouldContain("does-not-exist");
        });
    }

    [TestMethod]
    public void Navigate_BadParameterFormat_Throws()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await Should.ThrowAsync<NavigationRouteException>(() => nav.NavigateAsync("items/not-a-number"));
        });
    }

    [TestMethod]
    public void CurrentRoute_RoundTripsThroughString()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav1 = BuildNav();
            await nav1.NavigateAsync(Routes.Home);
            string url = nav1.CurrentRoute.ToString();

            var nav2 = BuildNav();
            (await nav2.NavigateAsync(url)).ShouldBe(NavigationResult.Success);
            nav2.CurrentRoute.ToString().ShouldBe(url);
        });
    }

    [TestMethod]
    public void CurrentRoute_ParameterizedRoundTrip()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("items/7");
            nav.CurrentRoute.ToString().ShouldBe("items/7");
        });
    }

    [TestMethod]
    public void Navigate_Anchor_PreservedInCurrentRoute()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home#section1");
            nav.CurrentRoute.Anchor.ShouldBe("section1");
            nav.CurrentRoute.ToString().ShouldBe("home#section1");
        });
    }

    private static TestNavigator BuildNav() => new(b =>
    {
        b.MapViewModel<HomeVm>();
        b.MapViewModel<ItemVm>();
        b.MapViewModel<MainVm>();
        b.MapViewModel<DetailsVm>();

        b.AddRoute(Routes.Home);
        b.AddRoute(Routes.Item);
        b.AddRoute(Routes.Main);
        b.AddRoute(Routes.MainDetails);
    });

    public static class Routes
    {
        public static readonly RootRoutePart<HomeVm> Home = Route.Build("home").Root<HomeVm>();
        public static readonly RootRoutePart<ItemVm, int> Item = Route.Build<int>(p => $"items/{p}").Root<ItemVm>();
        public static readonly RootRoutePart<MainVm> Main = Route.Build("main").Root<MainVm>();
        public static readonly ChildRoutePart<MainVm, DetailsVm> MainDetails = Route.Build("details").Child<MainVm, DetailsVm>();
    }

    public class HomeVm : RecordedLifecycleViewModel, IRoutedViewModel
    {
    }

    public class ItemVm : RecordedLifecycleViewModel, IRoutedViewModel<int>
    {
        public int GetParameter() => this.Parameter;
    }

    public class MainVm : RecordedLifecycleViewModel, IRoutedViewModel
    {
    }

    public class DetailsVm : RecordedLifecycleViewModel, IRoutedViewModel
    {
    }




}
