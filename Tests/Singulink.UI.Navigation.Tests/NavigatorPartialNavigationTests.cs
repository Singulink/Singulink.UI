using PrefixClassName.MsTest;
using Shouldly;
using Singulink.UI.Navigation.Tests.TestSupport;

namespace Singulink.UI.Navigation.Tests;

[PrefixTestClass]
public class NavigatorPartialNavigationTests
{
    [TestMethod]
    public void NavigatePartialAsync_AnchorOnly_UpdatesAnchor()
    {
        AsyncContextTest.Run(async () =>
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
        AsyncContextTest.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("p/c1");
            var parentBefore = ((FakeView)nav.RootViewNavigator.ActiveView!).DataContext;

            await nav.NavigatePartialAsync<ParentVm>(C2);

            var parentAfter = ((FakeView)nav.RootViewNavigator.ActiveView!).DataContext;
            parentAfter.ShouldBeSameAs(parentBefore);

            var pView = (ParentView)nav.RootViewNavigator.ActiveView!;
            ((FakeView)pView.ChildNavigator.ActiveView!).DataContext.ShouldBeOfType<C2Vm>();
            nav.CurrentRoute.ToString().ShouldBe("p/c2");
        });
    }

    [TestMethod]
    public void NavigatePartialAsync_NoMatchingParent_Throws()
    {
        AsyncContextTest.Run(async () =>
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
        AsyncContextTest.Run(async () =>
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
        AsyncContextTest.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("p/c1");

            nav.CurrentPathStartsWith(P, C1).ShouldBeTrue();
            nav.CurrentRouteHasParent<ParentVm>().ShouldBeTrue();
        });
    }

    [TestMethod]
    public void CurrentPathStartsWith_FalsePath()
    {
        AsyncContextTest.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home");

            nav.CurrentPathStartsWith(P, C1).ShouldBeFalse();
            nav.CurrentRouteHasParent<ParentVm>().ShouldBeFalse();
        });
    }

    private static readonly RootRoutePart<HomeVm> Home = Route.Build("home").Root<HomeVm>();
    private static readonly RootRoutePart<ParentVm> P = Route.Build("p").Root<ParentVm>();
    private static readonly ChildRoutePart<ParentVm, C1Vm> C1 = Route.Build("c1").Child<ParentVm, C1Vm>();
    private static readonly ChildRoutePart<ParentVm, C2Vm> C2 = Route.Build("c2").Child<ParentVm, C2Vm>();

    private static TestNavigator BuildNav() => new(b =>
    {
        b.MapRoutedView<HomeVm, FakeView>();
        b.MapRoutedView<ParentVm, ParentView>();
        b.MapRoutedView<C1Vm, FakeView>();
        b.MapRoutedView<C2Vm, FakeView>();

        b.AddRoute(Home);
        b.AddRoute(P);
        b.AddRoute(C1);
        b.AddRoute(C2);
    });

    public class HomeVm : RecordedLifecycleViewModel, IRoutedViewModel { }

    public class ParentVm : RecordedLifecycleViewModel, IRoutedViewModel { }

    public class C1Vm : RecordedLifecycleViewModel, IRoutedViewModel { }

    public class C2Vm : RecordedLifecycleViewModel, IRoutedViewModel { }

    public class ParentView : FakeParentView { }
}
