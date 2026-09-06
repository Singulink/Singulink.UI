using PrefixClassName.MsTest;
using Shouldly;
using Singulink.UI.Navigation.Testing;

namespace Singulink.UI.Navigation.Tests;

[PrefixTestClass]
public class NavigatorRouteBoundaryTests
{
    [TestMethod]
    public void Navigate_ParentA_ChildB_DoesNotMatchConcatenatedUrl()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = new TestNavigator(b =>
            {
                b.MapViewModel<ParentVm>();
                b.MapViewModel<ChildVm>();
                b.AddRoute(Route.Build("a").Root<ParentVm>());
                b.AddRoute(Route.Build("b").Child<ParentVm, ChildVm>());
            });

            // "ab" must NOT match parent literal "a" + child literal "b"; a segment boundary is required.
            await Should.ThrowAsync<NavigationRouteException>(() => nav.NavigateAsync("ab"));
        });
    }

    [TestMethod]
    public void Navigate_ParentA_ChildB_MatchesProperlySeparatedUrl()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = new TestNavigator(b =>
            {
                b.MapViewModel<ParentVm>();
                b.MapViewModel<ChildVm>();
                b.AddRoute(Route.Build("a").Root<ParentVm>());
                b.AddRoute(Route.Build("b").Child<ParentVm, ChildVm>());
            });

            (await nav.NavigateAsync("a/b")).ShouldBe(NavigationResult.Success);
            nav.CurrentRoute.ToString().ShouldBe("a/b");
        });
    }

    public class ParentVm : IRoutedViewModel { }

    public class ChildVm : IRoutedViewModel { }

}
