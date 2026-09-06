using PrefixClassName.MsTest;
using Shouldly;
using Singulink.UI.Navigation.Testing;
using Singulink.UI.Navigation.Tests.TestSupport;

namespace Singulink.UI.Navigation.Tests;

[PrefixTestClass]
public class NavigatorPartialNavigationTests
{
    [TestMethod]
    public void NavigatePartialAsync_AnchorOnly_UpdatesAnchor()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home");

            await nav.NavigatePartialAsync("section1");
            nav.CurrentRoute.Anchor.ShouldBe("section1");
            nav.CurrentRoute.ToString().ShouldBe("home#section1");
        });
    }

    [TestMethod]
    public void NavigatePartialAsync_FromParent_SwapsChildOnly()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("p/c1");
            var parentBefore = nav.ActiveViewModels[0];

            await nav.NavigatePartialAsync<ParentVm>(C2);

            var parentAfter = nav.ActiveViewModels[0];
            parentAfter.ShouldBeSameAs(parentBefore);

            nav.ActiveViewModels[1].ShouldBeOfType<C2Vm>();
            nav.CurrentRoute.ToString().ShouldBe("p/c2");
        });
    }

    [TestMethod]
    public void NavigatePartialAsync_NoMatchingParent_Throws()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home");

            await Should.ThrowAsync<NavigationRouteException>(
                () => nav.NavigatePartialAsync<ParentVm>(C2));
        });
    }

    [TestMethod]
    public void NavigateToParentAsync_TruncatesToParent()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("p/c1");

            (await nav.NavigateToParentAsync<ParentVm>()).ShouldBe(NavigationResult.Success);
            nav.CurrentRoute.ToString().ShouldBe("p");
        });
    }

    [TestMethod]
    public void CurrentPathStartsWith_TruePath()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("p/c1");

            nav.CurrentPathStartsWith(P.Then(C1)).ShouldBeTrue();
            nav.CurrentRouteHasParent<ParentVm>().ShouldBeTrue();
        });
    }

    [TestMethod]
    public void CurrentPathStartsWith_FalsePath()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home");

            nav.CurrentPathStartsWith(P.Then(C1)).ShouldBeFalse();
            nav.CurrentRouteHasParent<ParentVm>().ShouldBeFalse();
        });
    }

    private static readonly RootRoutePart<HomeVm> Home = Route.Build("home").Root<HomeVm>();
    private static readonly RootRoutePart<ParentVm> P = Route.Build("p").Root<ParentVm>();
    private static readonly ChildRoutePart<ParentVm, C1Vm> C1 = Route.Build("c1").Child<ParentVm, C1Vm>();
    private static readonly ChildRoutePart<ParentVm, C2Vm> C2 = Route.Build("c2").Child<ParentVm, C2Vm>();

    private static TestNavigator BuildNav() => new(b =>
    {
        b.MapViewModel<HomeVm>();
        b.MapViewModel<ParentVm>();
        b.MapViewModel<C1Vm>();
        b.MapViewModel<C2Vm>();

        b.AddRoute(Home);
        b.AddRoute(P);
        b.AddRoute(C1);
        b.AddRoute(C2);
    });

    public class HomeVm : RecordedLifecycleViewModel, IRoutedViewModel { }

    public class ParentVm : RecordedLifecycleViewModel, IRoutedViewModel { }

    public class C1Vm : RecordedLifecycleViewModel, IRoutedViewModel { }

    public class C2Vm : RecordedLifecycleViewModel, IRoutedViewModel { }

}
