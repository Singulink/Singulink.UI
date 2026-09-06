using PrefixClassName.MsTest;
using Shouldly;
using Singulink.UI.Navigation.Testing;

namespace Singulink.UI.Navigation.Tests;

/// <summary>
/// Tests for multi-level routes composed with <c>Then</c>.
/// </summary>
[PrefixTestClass]
public class RouteChainTests
{
    [TestMethod]
    public void Then_BuildsPath()
    {
        var route = Routes.Root.Then(Routes.Child).Then(Routes.GrandChild.ToConcrete(7));

        route.Path.ShouldBe("root/child/gc/7");
        route.ToString().ShouldBe("root/child/gc/7");
        route.RouteParts.Count.ShouldBe(3);

        var partial = Routes.Child.Then(Routes.GrandChild.ToConcrete(7));
        partial.ChildRouteParts.Count.ShouldBe(2);
    }

    [TestMethod]
    public void Then_ParentTypedChild_ProducesTerminalRoute()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();

            // A child typed only by its parent (as menu-style collections of child parts often are) can still be chained; the result just cannot be
            // extended further. When the static type carries the child view model type, the typed overload wins instead.
            IConcreteChildRoutePart<RootVm> child = Routes.Child;
            ConcreteRoute route = Routes.Root.Then(child);

            route.Path.ShouldBe("root/child");
            (await nav.NavigateAsync(route)).ShouldBe(NavigationResult.Success);
            nav.CurrentPathStartsWith(route).ShouldBeTrue();

            IConcreteChildRoutePart<ChildVm> grandChild = Routes.GrandChild.ToConcrete(4);
            ConcretePartialRoute<RootVm> partial = Routes.Child.Then(grandChild);
            await nav.NavigatePartialAsync<RootVm>(partial);
            nav.CurrentRoute.ToString().ShouldBe("root/child/gc/4");
        });
    }

    [TestMethod]
    public void Navigate_ThreeLevelChain()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();

            (await nav.NavigateAsync(Routes.Root.Then(Routes.Child).Then(Routes.GrandChild.ToConcrete(7)))).ShouldBe(NavigationResult.Success);

            nav.CurrentRoute.ToString().ShouldBe("root/child/gc/7");
            nav.ActiveViewModels.Select(vm => vm.GetType()).ShouldBe([typeof(RootVm), typeof(ChildVm), typeof(GrandChildVm)]);
            nav.ActiveViewModel<GrandChildVm>().Parameter.ShouldBe(7);
        });
    }

    [TestMethod]
    public void Navigate_ChainWithAnchor()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();

            await nav.NavigateAsync(Routes.Root.Then(Routes.Child), anchor: "top");

            nav.CurrentRoute.Path.ShouldBe("root/child");
            nav.CurrentRoute.Anchor.ShouldBe("top");
        });
    }

    [TestMethod]
    public void NavigatePartial_Chain_KeepsParent()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync(Routes.Root.Then(Routes.Child).Then(Routes.GrandChild.ToConcrete(1)));

            var root = nav.ActiveViewModel<RootVm>();

            await nav.NavigatePartialAsync<RootVm>(Routes.Child.Then(Routes.GrandChild.ToConcrete(2)));

            nav.CurrentRoute.ToString().ShouldBe("root/child/gc/2");
            nav.ActiveViewModel<RootVm>().ShouldBeSameAs(root);
            nav.ActiveViewModel<GrandChildVm>().Parameter.ShouldBe(2);
        });
    }

    [TestMethod]
    public void CurrentPathStartsWith_RootAndChain()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();

            nav.CurrentPathStartsWith(Routes.Root).ShouldBeFalse();

            await nav.NavigateAsync(Routes.Root.Then(Routes.Child).Then(Routes.GrandChild.ToConcrete(7)));

            nav.CurrentPathStartsWith(Routes.Root).ShouldBeTrue();
            nav.CurrentPathStartsWith(Routes.Root.Then(Routes.Child)).ShouldBeTrue();
            nav.CurrentPathStartsWith(Routes.Root.Then(Routes.Child).Then(Routes.GrandChild.ToConcrete(7))).ShouldBeTrue();
            nav.CurrentPathStartsWith(Routes.Root.Then(Routes.Child).Then(Routes.GrandChild.ToConcrete(9))).ShouldBeFalse();
            nav.CurrentPathStartsWith(Routes.Other).ShouldBeFalse();
        });
    }

    [TestMethod]
    public void Redirect_Chain()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();

            await nav.NavigateAsync(Routes.Other);

            nav.CurrentRoute.ToString().ShouldBe("root/child/gc/3");
            nav.Events.OfType<NavigationRedirectedEvent>().Single().ViewModel.ShouldBeOfType<OtherVm>();
        });
    }

    [TestMethod]
    public void Redirect_PartialChain()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync(Routes.Root.Then(Routes.Child).Then(Routes.GrandChild.ToConcrete(1)));

            var root = nav.ActiveViewModel<RootVm>();
            root.RedirectPartialTo = Routes.Child.Then(Routes.GrandChild.ToConcrete(5));

            await nav.RefreshAsync();

            nav.CurrentRoute.ToString().ShouldBe("root/child/gc/5");
        });
    }

    private static TestNavigator BuildNav() => new(b =>
    {
        b.MapViewModel<RootVm>();
        b.MapViewModel<ChildVm>();
        b.MapViewModel<GrandChildVm>();
        b.MapViewModel<OtherVm>();

        b.AddRoute(Routes.Root);
        b.AddRoute(Routes.Child);
        b.AddRoute(Routes.GrandChild);
        b.AddRoute(Routes.Other);
    });

    public static class Routes
    {
        public static readonly RootRoutePart<RootVm> Root = Route.Build("root").Root<RootVm>();
        public static readonly ChildRoutePart<RootVm, ChildVm> Child = Route.Build("child").Child<RootVm, ChildVm>();
        public static readonly ChildRoutePart<ChildVm, GrandChildVm, int> GrandChild = Route.Build<int>(p => $"gc/{p}").Child<ChildVm, GrandChildVm>();
        public static readonly RootRoutePart<OtherVm> Other = Route.Build("other").Root<OtherVm>();
    }

    public class RootVm : IRoutedViewModel
    {
        public ConcretePartialRoute<RootVm>? RedirectPartialTo { get; set; }

        public Task OnRouteNavigatedAsync(NavigationArgs args)
        {
            if (RedirectPartialTo is { } route)
            {
                RedirectPartialTo = null;
                args.Redirect = Redirect.NavigatePartial(route);
            }

            return Task.CompletedTask;
        }
    }

    public class ChildVm : IRoutedViewModel
    {
    }

    public class GrandChildVm : IRoutedViewModel<int>
    {
    }

    public class OtherVm : IRoutedViewModel
    {
        public Task OnNavigatedToAsync(NavigationArgs args)
        {
            args.Redirect = Redirect.Navigate(Routes.Root.Then(Routes.Child).Then(Routes.GrandChild.ToConcrete(3)));
            return Task.CompletedTask;
        }
    }
}
