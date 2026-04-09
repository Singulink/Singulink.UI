using PrefixClassName.MsTest;
using Shouldly;
using Singulink.UI.Navigation.Tests.TestSupport;

namespace Singulink.UI.Navigation.Tests;

[PrefixTestClass]
public class NavigatorHistoryTests
{
    [TestMethod]
    public void GoBack_ReturnsToPreviousRoute()
    {
        AsyncContextTest.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("a");
            await nav.NavigateAsync("b");

            nav.CanGoBack.ShouldBeTrue();
            nav.HasBackHistory.ShouldBeTrue();

            (await nav.GoBackAsync()).ShouldBe(NavigationResult.Success);
            nav.CurrentRoute.ToString().ShouldBe("a");
            nav.HasForwardHistory.ShouldBeTrue();
            nav.CanGoForward.ShouldBeTrue();
        });
    }

    [TestMethod]
    public void GoForward_AfterBack_RestoresRoute()
    {
        AsyncContextTest.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("a");
            await nav.NavigateAsync("b");
            await nav.GoBackAsync();

            (await nav.GoForwardAsync()).ShouldBe(NavigationResult.Success);
            nav.CurrentRoute.ToString().ShouldBe("b");
            nav.HasForwardHistory.ShouldBeFalse();
        });
    }

    [TestMethod]
    public void Refresh_RecreatesCurrentRoute()
    {
        AsyncContextTest.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("a");

            nav.CanRefresh.ShouldBeTrue();
            (await nav.RefreshAsync()).ShouldBe(NavigationResult.Success);
            nav.CurrentRoute.ToString().ShouldBe("a");
        });
    }

    [TestMethod]
    public void GoBack_NoHistory_Throws()
    {
        AsyncContextTest.Run(async () =>
        {
            var nav = BuildNav();
            await Should.ThrowAsync<InvalidOperationException>(() => nav.GoBackAsync());
        });
    }

    [TestMethod]
    public void GoForward_NoHistory_Throws()
    {
        AsyncContextTest.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("a");
            await Should.ThrowAsync<InvalidOperationException>(() => nav.GoForwardAsync());
        });
    }

    [TestMethod]
    public void Navigate_AfterBack_DiscardsForwardStack()
    {
        AsyncContextTest.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("a");
            await nav.NavigateAsync("b");
            await nav.GoBackAsync();

            await nav.NavigateAsync("c");

            nav.HasForwardHistory.ShouldBeFalse();
            nav.HasBackHistory.ShouldBeTrue();
        });
    }

    [TestMethod]
    public void GetBackStack_RecentFirst()
    {
        AsyncContextTest.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("a");
            await nav.NavigateAsync("b");
            await nav.NavigateAsync("c");

            var stack = nav.GetBackStack();
            stack.Count.ShouldBe(2);
            stack[0].ToString().ShouldBe("b");
            stack[1].ToString().ShouldBe("a");
        });
    }

    [TestMethod]
    public void GetForwardStack_OrderedFromCurrentForward()
    {
        AsyncContextTest.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("a");
            await nav.NavigateAsync("b");
            await nav.NavigateAsync("c");
            await nav.GoBackAsync();
            await nav.GoBackAsync();

            var stack = nav.GetForwardStack();
            stack.Count.ShouldBe(2);
            stack[0].ToString().ShouldBe("b");
            stack[1].ToString().ShouldBe("c");
        });
    }

    [TestMethod]
    public void ClearHistoryAsync_LeavesOnlyCurrent()
    {
        AsyncContextTest.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("a");
            await nav.NavigateAsync("b");
            await nav.NavigateAsync("c");

            await nav.ClearHistoryAsync();

            nav.HasBackHistory.ShouldBeFalse();
            nav.HasForwardHistory.ShouldBeFalse();
            nav.CurrentRoute.ToString().ShouldBe("c");
        });
    }

    [TestMethod]
    public void CanBeCached_False_RecreatesOnBackForward()
    {
        AsyncContextTest.Run(async () =>
        {
            var nav = new TestNavigator(b =>
            {
                b.MapRoutedView<UncachedVm, FakeView>();
                b.MapRoutedView<OtherVm, FakeView>();
                b.AddRoute(Route.Build("u").Root<UncachedVm>());
                b.AddRoute(Route.Build("o").Root<OtherVm>());
            });

            await nav.NavigateAsync("u");
            var firstVm = ((FakeView)nav.RootViewNavigator.ActiveView!).DataContext;
            await nav.NavigateAsync("o");
            await nav.GoBackAsync();
            var secondVm = ((FakeView)nav.RootViewNavigator.ActiveView!).DataContext;

            secondVm.ShouldNotBeSameAs(firstVm);
        });
    }

    [TestMethod]
    public void CanBeCached_True_ReusesOnBackForward()
    {
        AsyncContextTest.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("a");
            var firstVm = ((FakeView)nav.RootViewNavigator.ActiveView!).DataContext;
            await nav.NavigateAsync("b");
            await nav.GoBackAsync();
            var secondVm = ((FakeView)nav.RootViewNavigator.ActiveView!).DataContext;

            secondVm.ShouldBeSameAs(firstVm);
        });
    }

    [TestMethod]
    public void CanGoBack_NotifiesPropertyChanged()
    {
        AsyncContextTest.Run(async () =>
        {
            var nav = BuildNav();
            var changes = new List<string?>();
            ((System.ComponentModel.INotifyPropertyChanged)nav).PropertyChanged += (_, e) => changes.Add(e.PropertyName);

            await nav.NavigateAsync("a");
            changes.ShouldContain(nameof(nav.CurrentRoute));
            changes.ShouldContain(nameof(nav.IsNavigating));

            changes.Clear();
            await nav.NavigateAsync("b");
            changes.ShouldContain(nameof(nav.CanGoBack));
            changes.ShouldContain(nameof(nav.HasBackHistory));
        });
    }

    private static TestNavigator BuildNav() => new(b =>
    {
        b.MapRoutedView<AVm, FakeView>();
        b.MapRoutedView<BVm, FakeView>();
        b.MapRoutedView<CVm, FakeView>();
        b.AddRoute(Route.Build("a").Root<AVm>());
        b.AddRoute(Route.Build("b").Root<BVm>());
        b.AddRoute(Route.Build("c").Root<CVm>());
    });

    public class AVm : RecordedLifecycleViewModel, IRoutedViewModel { }

    public class BVm : RecordedLifecycleViewModel, IRoutedViewModel { }

    public class CVm : RecordedLifecycleViewModel, IRoutedViewModel { }

    public class UncachedVm : RecordedLifecycleViewModel, IRoutedViewModel
    {
        public UncachedVm() { CanBeCachedValue = false; }
    }

    public class OtherVm : RecordedLifecycleViewModel, IRoutedViewModel { }
}
