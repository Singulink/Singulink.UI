using PrefixClassName.MsTest;
using Shouldly;
using Singulink.UI.Navigation.Testing;
using Singulink.UI.Navigation.Tests.TestSupport;

namespace Singulink.UI.Navigation.Tests;

/// <summary>
/// Smoke test that proves the <see cref="TestNavigator"/> from the testing package works end-to-end:
/// build → navigate → view model materialization → lifecycle hooks → dialog show/close.
/// </summary>
[PrefixTestClass]
public class TestInfrastructureSmokeTests
{
    [TestMethod]
    public void Navigate_RootRoute_MaterializesViewAndCallsLifecycle()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = new TestNavigator(b =>
            {
                b.MapViewModel<HomeVm>();
                b.AddRoute(Route.Build("home").Root<HomeVm>());
            });

            var result = await nav.NavigateAsync("home");

            result.ShouldBe(NavigationResult.Success);
            nav.Events.OfType<ViewModelCreatedEvent>().Count().ShouldBe(1);
            nav.Events.OfType<ViewModelCreatedEvent>().ElementAt(0).ViewModel.ShouldBeOfType<HomeVm>();
            nav.ActiveViewModels[0].ShouldBeOfType<HomeVm>();

            var vm = nav.ActiveViewModel<HomeVm>();
            vm.Events.Select(e => e.Kind).ShouldBe([LifecycleEventKind.NavigatedTo, LifecycleEventKind.RouteNavigated]);
            vm.Events[0].NavigationType.ShouldBe(NavigationType.New);
            vm.Events[0].HasChildNavigation.ShouldBeFalse();
        });
    }

    [TestMethod]
    public void ShowDialogAsync_ResolvesResult()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = new TestNavigator(b =>
            {
                b.MapViewModel<HomeVm>();
                b.AddRoute(Route.Build("home").Root<HomeVm>());
            });

            await nav.NavigateAsync("home");

            var dialogVm = new TestDialogVm();
            var showTask = nav.ShowDialogAsync(dialogVm);

            if (showTask.IsFaulted)
                throw showTask.Exception!;

            nav.ShowingDialogs.Count.ShouldBe(1);
            nav.IsShowingDialog.ShouldBeTrue();

            // Close from inside the test context loop:
            nav.TopDialog!.Navigator.Close();
            await showTask;

            nav.ShowingDialogs.Count.ShouldBe(0);
            nav.IsShowingDialog.ShouldBeFalse();
            nav.Events.Where(e => e is DialogShownEvent or DialogClosedEvent or LayerCoveredEvent).ShouldBe([
                new LayerCoveredEvent(null, true),
                new DialogShownEvent(dialogVm, null),
                new DialogClosedEvent(dialogVm),
                new LayerCoveredEvent(null, false),
            ]);
        });
    }

    public class HomeVm : RecordedLifecycleViewModel, IRoutedViewModel
    {
    }


    public class TestDialogVm : IDialogViewModel
    {
    }
}
