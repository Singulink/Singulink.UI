using System.Runtime.CompilerServices;
using PrefixClassName.MsTest;
using Shouldly;
using Singulink.UI.Navigation.Testing;
using Singulink.UI.Navigation.Tests.TestSupport;

namespace Singulink.UI.Navigation.Tests;

/// <summary>
/// Verifies that the navigator lets go of view models once it no longer needs them, by checking that they become collectable rather than
/// only that they were disposed.
/// </summary>
[PrefixTestClass]
public class NavigatorReleaseTests
{
    [TestMethod]
    public void NavigateAway_UncachedViewModel_IsCollectable()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("uncached");
            var weakVm = CaptureActive<UncachedVm>(nav);

            await nav.NavigateAsync("a");
            Collect(nav);

            weakVm.IsAlive.ShouldBeFalse("an uncached view model that was navigated away from is still referenced");
        });
    }

    [TestMethod]
    public void NavigateAway_CachedViewModel_StaysWhileInHistory_ThenIsCollectable()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("a");
            var weakVm = CaptureActive<AVm>(nav);

            await nav.NavigateAsync("b");
            Collect(nav);
            weakVm.IsAlive.ShouldBeTrue("a cached view model in the back history was collected");

            await nav.ClearHistoryAsync();
            Collect(nav);
            weakVm.IsAlive.ShouldBeFalse("a cached view model is still referenced after the history was cleared");
        });
    }

    [TestMethod]
    public void GoBack_UncachedForwardViewModel_IsCollectable()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("a");
            await nav.NavigateAsync("uncached");
            var weakVm = CaptureActive<UncachedVm>(nav);

            await nav.GoBackAsync();
            Collect(nav);

            weakVm.IsAlive.ShouldBeFalse("an uncached view model in the forward history is still referenced");
        });
    }

    [TestMethod]
    public void Shutdown_ReleasesCurrentViewModel()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("a");
            var weakVm = CaptureActive<AVm>(nav);

            (await nav.TryShutDownAsync()).ShouldBeTrue();
            Collect(nav);

            weakVm.IsAlive.ShouldBeFalse("the current view model is still referenced after shutdown");
        });
    }

    // Kept out of the async state machine (which would hold the instance in a hoisted local) and not inlined, so that nothing but the
    // navigator can keep the view model alive.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CaptureActive<T>(TestNavigator nav) where T : class, IRoutedViewModelBase => new(nav.ActiveViewModel<T>());

    // The test navigator's event log records every view model it created, so it is cleared first; only the navigator proper should be able
    // to keep an instance alive.
    private static void Collect(TestNavigator nav)
    {
        nav.ClearEvents();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private static TestNavigator BuildNav() => new(b =>
    {
        b.MapViewModel<AVm>();
        b.MapViewModel<BVm>();
        b.MapViewModel<UncachedVm>();
        b.AddRoute(Route.Build("a").Root<AVm>());
        b.AddRoute(Route.Build("b").Root<BVm>());
        b.AddRoute(Route.Build("uncached").Root<UncachedVm>());
    });

    public class AVm : RecordedLifecycleViewModel, IRoutedViewModel { }

    public class BVm : RecordedLifecycleViewModel, IRoutedViewModel { }

    public class UncachedVm : RecordedLifecycleViewModel, IRoutedViewModel
    {
        public UncachedVm() { CanBeCachedValue = false; }
    }
}
