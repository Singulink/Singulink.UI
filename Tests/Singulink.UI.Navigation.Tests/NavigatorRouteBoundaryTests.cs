using PrefixClassName.MsTest;
using Shouldly;
using Singulink.UI.Navigation.Tests.TestSupport;

namespace Singulink.UI.Navigation.Tests;

[PrefixTestClass]
public class NavigatorRouteBoundaryTests
{
    [TestMethod]
    public void Navigate_ParentA_ChildB_DoesNotMatchConcatenatedUrl()
    {
        AsyncContextTest.Run(async () =>
        {
            var nav = new TestNavigator(b =>
            {
                b.MapRoutedView<ParentVm, ParentView>();
                b.MapRoutedView<ChildVm, FakeView>();
                b.AddRoute(Route.Build("a").Root<ParentVm>());
                b.AddRoute(Route.Build("b").Child<ParentVm, ChildVm>());
            });

            // "ab" must NOT match parent literal "a" + child literal "b"; a segment boundary is required.
            await Should.ThrowAsync<ArgumentException>(() => nav.NavigateAsync("ab"));
        });
    }

    [TestMethod]
    public void Navigate_ParentA_ChildB_MatchesProperlySeparatedUrl()
    {
        AsyncContextTest.Run(async () =>
        {
            var nav = new TestNavigator(b =>
            {
                b.MapRoutedView<ParentVm, ParentView>();
                b.MapRoutedView<ChildVm, FakeView>();
                b.AddRoute(Route.Build("a").Root<ParentVm>());
                b.AddRoute(Route.Build("b").Child<ParentVm, ChildVm>());
            });

            (await nav.NavigateAsync("a/b")).ShouldBe(NavigationResult.Success);
            nav.CurrentRoute.ToString().ShouldBe("a/b");
        });
    }

    public class ParentVm : IRoutedViewModel { }

    public class ChildVm : IRoutedViewModel { }

    public class ParentView : FakeParentView { }
}
