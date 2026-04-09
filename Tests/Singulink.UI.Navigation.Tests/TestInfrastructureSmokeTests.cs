using PrefixClassName.MsTest;
using Shouldly;
using Singulink.UI.Navigation.Tests.TestSupport;

namespace Singulink.UI.Navigation.Tests;

/// <summary>
/// Smoke test that proves the <see cref="TestNavigator"/> infrastructure works end-to-end:
/// build → navigate → view materialization → lifecycle hooks → dialog show/close.
/// </summary>
[PrefixTestClass]
public class TestInfrastructureSmokeTests
{
    [TestMethod]
    public void Navigate_RootRoute_MaterializesViewAndCallsLifecycle()
    {
        AsyncContextTest.Run(async () =>
        {
            var nav = new TestNavigator(b =>
            {
                b.MapRoutedView<HomeVm, HomeView>();
                b.AddRoute(Route.Build("home").Root<HomeVm>());
            });

            var result = await nav.NavigateAsync("home");

            result.ShouldBe(NavigationResult.Success);
            nav.WiredViews.Count.ShouldBe(1);
            nav.WiredViews[0].View.ShouldBeOfType<HomeView>();
            nav.WiredViews[0].ViewModel.ShouldBeOfType<HomeVm>();
            nav.RootViewNavigator.ActiveView.ShouldBeOfType<HomeView>();

            var vm = (HomeVm)nav.WiredViews[0].ViewModel;
            vm.Events.Select(e => e.Kind).ShouldBe([LifecycleEventKind.NavigatedTo, LifecycleEventKind.RouteNavigated]);
            vm.Events[0].NavigationType.ShouldBe(NavigationType.New);
            vm.Events[0].HasChildNavigation.ShouldBeFalse();
        });
    }

    [TestMethod]
    public void ShowDialogAsync_ResolvesResult()
    {
        AsyncContextTest.Run(async () =>
        {
            var nav = new TestNavigator(b =>
            {
                b.MapRoutedView<HomeVm, HomeView>();
                b.AddRoute(Route.Build("home").Root<HomeVm>());
                b.MapDialog<TestDialogVm, FakeDialog>();
            });

            await nav.NavigateAsync("home");

            var dialogVm = new TestDialogVm();
            var showTask = nav.ShowDialogAsync(dialogVm);

            if (showTask.IsFaulted)
                throw showTask.Exception!;

            nav.ShownDialogs.Count.ShouldBe(1);
            nav.IsShowingDialog.ShouldBeTrue();

            // Close from inside the AsyncContext loop:
            ((IDialogNavigator)nav.TryGetTopDialog()!.Value.Navigator).Close();
            await showTask;

            nav.ShownDialogs.Count.ShouldBe(0);
            nav.IsShowingDialog.ShouldBeFalse();
            nav.DialogEvents.Select(e => e.Kind).ShouldBe([DialogEventKind.Show, DialogEventKind.Hide]);
        });
    }

    public class HomeVm : RecordedLifecycleViewModel, IRoutedViewModel
    {
    }

    public class HomeView : FakeView
    {
    }

    public class TestDialogVm : IDialogViewModel
    {
    }
}
