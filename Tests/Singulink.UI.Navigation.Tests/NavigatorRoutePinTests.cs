using System.Runtime.CompilerServices;
using PrefixClassName.MsTest;
using Shouldly;
using Singulink.UI.Navigation.Testing;
using Singulink.UI.Navigation.Tests.TestSupport;

namespace Singulink.UI.Navigation.Tests;

[PrefixTestClass]
public class NavigatorRoutePinTests
{
    [TestMethod]
    public void Pin_RetainsUncachedViewModel_AndReusesItOnReturn()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("form#field-1");

            var formVm = nav.ActiveViewModel<FormVm>();
            using var pin = nav.PinCurrentRoute();

            await nav.NavigateAsync("a");
            await nav.NavigateAsync("b");

            formVm.IsDisposed.ShouldBeFalse();
            pin.IsPinned.ShouldBeTrue();

            (await nav.NavigateAsync(pin.Route)).ShouldBe(NavigationResult.Success);

            nav.ActiveViewModel<FormVm>().ShouldBeSameAs(formVm);
            nav.CurrentRoute.ToString().ShouldBe("form#field-1");
            nav.Events.OfType<ViewModelCreatedEvent>().Count(e => e.ViewModel is FormVm).ShouldBe(1);
            formVm.Events.Count(e => e.Kind == LifecycleEventKind.NavigatedTo).ShouldBe(2);
        });
    }

    [TestMethod]
    public void Pin_SurvivesForwardStackBeingCleared()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("a");
            await nav.NavigateAsync("form");

            var formVm = nav.ActiveViewModel<FormVm>();
            using var pin = nav.PinCurrentRoute();

            await nav.GoBackAsync();
            await nav.NavigateAsync("b"); // Drops the form route from the forward stack.

            nav.GetForwardStack().ShouldBeEmpty();
            formVm.IsDisposed.ShouldBeFalse();

            await nav.NavigateAsync(pin.Route);
            nav.ActiveViewModel<FormVm>().ShouldBeSameAs(formVm);
        });
    }

    [TestMethod]
    public void Pin_SurvivesClearHistory()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("form");

            var formVm = nav.ActiveViewModel<FormVm>();
            using var pin = nav.PinCurrentRoute();

            await nav.NavigateAsync("a");
            await nav.ClearHistoryAsync();

            nav.GetBackStack().ShouldBeEmpty();
            formVm.IsDisposed.ShouldBeFalse();

            await nav.NavigateAsync(pin.Route);
            nav.ActiveViewModel<FormVm>().ShouldBeSameAs(formVm);
        });
    }

    [TestMethod]
    public void Pin_RetainsParentAndChild_WithoutReactivatingRetainedParent()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("main/details");

            var mainVm = nav.ActiveViewModel<MainVm>();
            var detailsVm = nav.ActiveViewModel<DetailsVm>();
            using var pin = nav.PinCurrentRoute();

            await nav.NavigateAsync("main/other");
            detailsVm.IsDisposed.ShouldBeFalse();

            mainVm.Events.Clear();
            await nav.NavigateAsync(pin.Route);

            nav.ActiveViewModel<MainVm>().ShouldBeSameAs(mainVm);
            nav.ActiveViewModel<DetailsVm>().ShouldBeSameAs(detailsVm);
            mainVm.Events.Select(e => e.Kind).ShouldBe([LifecycleEventKind.RouteNavigating, LifecycleEventKind.RouteNavigated]);
        });
    }

    [TestMethod]
    public void Pin_ItemsAreReusedByOtherNavigations_AfterLeavingTheStack()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("main/details");

            var mainVm = nav.ActiveViewModel<MainVm>();
            using var pin = nav.PinCurrentRoute();

            await nav.NavigateAsync("a");
            await nav.ClearHistoryAsync(); // Nothing under "main" remains in the stacks.

            // A navigation under the same parent must reuse the pinned parent rather than creating a second instance for the same route part.
            await nav.NavigateAsync("main/other");
            nav.ActiveViewModel<MainVm>().ShouldBeSameAs(mainVm);

            await nav.NavigateAsync(pin.Route);
            nav.ActiveViewModel<MainVm>().ShouldBeSameAs(mainVm);
        });
    }

    [TestMethod]
    public void Pin_RouteReflectsInPlaceUpdates_WhileCurrent()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("form#field-1");

            using var pin = nav.PinCurrentRoute();
            nav.UpdateCurrentRoute("field-2");

            pin.Route.Anchor.ShouldBe("field-2");
            pin.Route.ShouldBeSameAs(nav.CurrentRoute);
        });
    }

    [TestMethod]
    public void NavigateToPinnedRoute_PushesNewEntry()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("form#field-1");

            using var pin = nav.PinCurrentRoute();
            await nav.NavigateAsync("a");
            await nav.NavigateAsync(pin.Route);

            nav.GetBackStack().Select(r => r.ToString()).ShouldBe(["a", "form#field-1"]);
            nav.CurrentRoute.ShouldNotBeSameAs(pin.Route);

            // The new entry is independent of the pinned one.
            nav.UpdateCurrentRoute("field-2");
            pin.Route.Anchor.ShouldBe("field-1");
        });
    }

    [TestMethod]
    public void DisposedPin_ReleasesViewModel_OnNextNavigation()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("form");

            var formVm = nav.ActiveViewModel<FormVm>();
            var pin = nav.PinCurrentRoute();

            await nav.NavigateAsync("a");
            pin.Dispose();

            pin.IsPinned.ShouldBeFalse();
            pin.Route.ToString().ShouldBe("form");
            formVm.IsDisposed.ShouldBeFalse();

            await nav.NavigateAsync("b");
            formVm.IsDisposed.ShouldBeTrue();

            // Returning to the route now creates a fresh view model.
            await nav.NavigateAsync(pin.Route);
            nav.ActiveViewModel<FormVm>().ShouldNotBeSameAs(formVm);
        });
    }

    [TestMethod]
    public void DisposedPin_WhileRouteIsCurrent_KeepsItActive()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("form");

            var formVm = nav.ActiveViewModel<FormVm>();
            var pin = nav.PinCurrentRoute();
            pin.Dispose();

            formVm.IsDisposed.ShouldBeFalse();
            nav.ActiveViewModel<FormVm>().ShouldBeSameAs(formVm);

            await nav.NavigateAsync("a");
            formVm.IsDisposed.ShouldBeTrue();
        });
    }

    [TestMethod]
    public void DroppedPin_ReleasesViewModel_AfterCollection()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("form");

            var formVm = nav.ActiveViewModel<FormVm>();
            PinWithoutHolding(nav);

            await nav.NavigateAsync("a");
            formVm.IsDisposed.ShouldBeFalse();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // The dead pin is noticed and released by the next trim.
            await nav.NavigateAsync("b");
            formVm.IsDisposed.ShouldBeTrue();
        });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void PinWithoutHolding(TestNavigator nav) => nav.PinCurrentRoute();

    [TestMethod]
    public void Pin_DisposeIsIdempotent()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("form");

            var pin = nav.PinCurrentRoute();
            pin.Dispose();
            pin.Dispose();

            await nav.NavigateAsync("a");
            await nav.NavigateAsync("b");
        });
    }

    [TestMethod]
    public void ShutDown_ReleasesPins()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("form");

            var formVm = nav.ActiveViewModel<FormVm>();
            var pin = nav.PinCurrentRoute();
            await nav.NavigateAsync("a");

            (await nav.TryShutDownAsync()).ShouldBeTrue();

            pin.IsPinned.ShouldBeFalse();
            formVm.IsDisposed.ShouldBeTrue();
        });
    }

    [TestMethod]
    public void Redirect_ToRoute_ReusesPinnedViewModel()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("form");

            var formVm = nav.ActiveViewModel<FormVm>();
            using var pin = nav.PinCurrentRoute();
            await nav.NavigateAsync("a");

            RedirectingVm.Target = pin.Route;
            (await nav.NavigateAsync("redirect")).ShouldBe(NavigationResult.Success);

            nav.ActiveViewModel<FormVm>().ShouldBeSameAs(formVm);
            nav.CurrentRoute.ToString().ShouldBe("form");
        });
    }

    [TestMethod]
    public void PinCurrentRoute_BeforeFirstNavigation_Throws()
    {
        NavigationTestContext.Run(() =>
        {
            var nav = BuildNav();
            Should.Throw<InvalidOperationException>(() => nav.PinCurrentRoute());
            return Task.CompletedTask;
        });
    }

    [TestMethod]
    public void NavigateToRoute_EmptyRoute_Throws()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await Should.ThrowAsync<ArgumentException>(() => nav.NavigateAsync(nav.CurrentRoute));
        });
    }

    [TestMethod]
    public void PinCurrentRoute_LeafNotPinnable_Throws()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("unpinnable");

            var ex = Should.Throw<InvalidOperationException>(() => nav.PinCurrentRoute());
            ex.Message.ShouldContain(nameof(UnpinnableVm));
        });
    }

    [TestMethod]
    public void Pin_RetainsPinnableAncestor_BeyondCacheDepth()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("main/details");

            var mainVm = nav.ActiveViewModel<MainVm>();
            var detailsVm = nav.ActiveViewModel<DetailsVm>();
            using var pin = nav.PinCurrentRoute();

            // Leaves the parent context entirely; with zero cache depth only the pin keeps the (cacheable) parent alive.
            await nav.NavigateAsync("a");
            await nav.NavigateAsync("b");

            pin.IsPinned.ShouldBeTrue();
            detailsVm.IsDisposed.ShouldBeFalse();

            mainVm.Events.Clear();
            await nav.NavigateAsync(pin.Route);

            nav.ActiveViewModel<MainVm>().ShouldBeSameAs(mainVm);
            nav.ActiveViewModel<DetailsVm>().ShouldBeSameAs(detailsVm);
            mainVm.Events.Select(e => e.Kind).ShouldBe([LifecycleEventKind.NavigatedTo, LifecycleEventKind.RouteNavigated]);
        });
    }

    [TestMethod]
    public void Pin_ChainStopsAtNonPinnableAncestor_DependentLeafEvictedWithIt()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("root/dependent");

            var rootVm = nav.ActiveViewModel<RootVm>();
            var childVm = nav.ActiveViewModel<DependentVm>();
            var pin = nav.PinCurrentRoute();

            // Staying under the non-pinnable parent keeps everything.
            await nav.NavigateAsync("root/independent");
            pin.IsPinned.ShouldBeTrue();
            childVm.IsDisposed.ShouldBeFalse();

            // Leaving it evicts the parent, which takes its dependent (pinned) child with it and invalidates the pin.
            await nav.NavigateAsync("a");

            rootVm.IsDisposed.ShouldBeTrue();
            childVm.IsDisposed.ShouldBeTrue();
            pin.IsPinned.ShouldBeFalse();

            // The route is still navigable, with fresh instances.
            await nav.NavigateAsync(pin.Route);
            nav.ActiveViewModel<RootVm>().ShouldNotBeSameAs(rootVm);
            nav.ActiveViewModel<DependentVm>().ShouldNotBeSameAs(childVm);

            pin.Dispose(); // No-op on an invalidated pin.
        });
    }

    [TestMethod]
    public void Pin_ChainStopsAtNonPinnableAncestor_IndependentLeafSurvives()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("root/independent");

            var rootVm = nav.ActiveViewModel<RootVm>();
            var childVm = nav.ActiveViewModel<IndependentVm>();
            using var pin = nav.PinCurrentRoute();

            await nav.NavigateAsync("a");

            rootVm.IsDisposed.ShouldBeTrue();
            childVm.IsDisposed.ShouldBeFalse();
            pin.IsPinned.ShouldBeTrue();

            await nav.NavigateAsync(pin.Route);
            nav.ActiveViewModel<RootVm>().ShouldNotBeSameAs(rootVm);
            nav.ActiveViewModel<IndependentVm>().ShouldBeSameAs(childVm);
        });
    }

    [TestMethod]
    public void EvictedParent_EvictsDependentChildren_AcrossSharedRoutes()
    {
        NavigationTestContext.Run(async () =>
        {
            // Default cache depth: the cacheable children would normally be retained, but their non-cacheable parent is not.
            var nav = new TestNavigator(b =>
            {
                b.MapViewModel<RootVm>();
                b.MapViewModel<CachedDependentVm>();
                b.MapViewModel<IndependentVm>();
                b.MapViewModel<AVm>();
                b.AddRoute(Routes.Root);
                b.AddRoute(Routes.RootCachedDependent);
                b.AddRoute(Routes.RootIndependent);
                b.AddRoute(Routes.A);
            });

            await nav.NavigateAsync("root/independent");
            await nav.NavigateAsync("root/cached");

            var rootVm = nav.ActiveViewModel<RootVm>();
            var cachedVm = nav.ActiveViewModel<CachedDependentVm>();

            await nav.NavigateAsync("a");

            // The parent is shared by two back stack entries; evicting it via the first must still evict the dependent child on the second.
            rootVm.IsDisposed.ShouldBeTrue();
            cachedVm.IsDisposed.ShouldBeTrue();
        });
    }

    [TestMethod]
    public void ClearHistory_DisposesRemovedCachedViewModels()
    {
        NavigationTestContext.Run(async () =>
        {
            // Default cache depth, so the cached view model is retained until the history it lives in is cleared.
            var nav = new TestNavigator(b =>
            {
                b.MapViewModel<CachedVm>();
                b.MapViewModel<AVm>();
                b.AddRoute(Routes.Cached);
                b.AddRoute(Routes.A);
            });

            await nav.NavigateAsync("cached");

            var cachedVm = nav.ActiveViewModel<CachedVm>();
            await nav.NavigateAsync("a");

            cachedVm.IsDisposed.ShouldBeFalse();
            await nav.ClearHistoryAsync();
            cachedVm.IsDisposed.ShouldBeTrue();
        });
    }

    private static TestNavigator BuildNav() => new(b =>
    {
        // No caching beyond the current route, so anything retained across navigations is retained by a pin.
        b.ConfigureNavigationStacks(maxBackCachedDepth: 0, maxForwardCachedDepth: 0);

        b.MapViewModel<FormVm>();
        b.MapViewModel<CachedVm>();
        b.MapViewModel<AVm>();
        b.MapViewModel<BVm>();
        b.MapViewModel<MainVm>();
        b.MapViewModel<DetailsVm>();
        b.MapViewModel<OtherVm>();
        b.MapViewModel<RedirectingVm>();
        b.MapViewModel<UnpinnableVm>();
        b.MapViewModel<RootVm>();
        b.MapViewModel<DependentVm>();
        b.MapViewModel<IndependentVm>();
        b.MapViewModel<CachedDependentVm>();

        b.AddRoute(Routes.Form);
        b.AddRoute(Routes.Cached);
        b.AddRoute(Routes.A);
        b.AddRoute(Routes.B);
        b.AddRoute(Routes.Main);
        b.AddRoute(Routes.MainDetails);
        b.AddRoute(Routes.MainOther);
        b.AddRoute(Routes.Redirecting);
        b.AddRoute(Routes.Unpinnable);
        b.AddRoute(Routes.Root);
        b.AddRoute(Routes.RootDependent);
        b.AddRoute(Routes.RootIndependent);
        b.AddRoute(Routes.RootCachedDependent);
    });

    public static class Routes
    {
        public static readonly RootRoutePart<FormVm> Form = Route.Build("form").Root<FormVm>();
        public static readonly RootRoutePart<CachedVm> Cached = Route.Build("cached").Root<CachedVm>();
        public static readonly RootRoutePart<AVm> A = Route.Build("a").Root<AVm>();
        public static readonly RootRoutePart<BVm> B = Route.Build("b").Root<BVm>();
        public static readonly RootRoutePart<MainVm> Main = Route.Build("main").Root<MainVm>();
        public static readonly ChildRoutePart<MainVm, DetailsVm> MainDetails = Route.Build("details").Child<MainVm, DetailsVm>();
        public static readonly ChildRoutePart<MainVm, OtherVm> MainOther = Route.Build("other").Child<MainVm, OtherVm>();
        public static readonly RootRoutePart<RedirectingVm> Redirecting = Route.Build("redirect").Root<RedirectingVm>();
        public static readonly RootRoutePart<UnpinnableVm> Unpinnable = Route.Build("unpinnable").Root<UnpinnableVm>();
        public static readonly RootRoutePart<RootVm> Root = Route.Build("root").Root<RootVm>();
        public static readonly ChildRoutePart<RootVm, DependentVm> RootDependent = Route.Build("dependent").Child<RootVm, DependentVm>();
        public static readonly ChildRoutePart<RootVm, IndependentVm> RootIndependent = Route.Build("independent").Child<RootVm, IndependentVm>();
        public static readonly ChildRoutePart<RootVm, CachedDependentVm> RootCachedDependent = Route.Build("cached").Child<RootVm, CachedDependentVm>();
    }

    public class DisposableVm : RecordedLifecycleViewModel, IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }

    public class FormVm : DisposableVm, IRoutedViewModel
    {
        public FormVm()
        {
            CanBeCachedValue = false;
            CanBePinnedValue = true;
        }
    }

    public class CachedVm : DisposableVm, IRoutedViewModel
    {
    }

    public class AVm : RecordedLifecycleViewModel, IRoutedViewModel
    {
    }

    public class BVm : RecordedLifecycleViewModel, IRoutedViewModel
    {
    }

    public class MainVm : RecordedLifecycleViewModel, IRoutedViewModel
    {
    }

    public class DetailsVm : DisposableVm, IRoutedViewModel
    {
        public DetailsVm()
        {
            CanBeCachedValue = false;
            CanBePinnedValue = true;
        }
    }

    public class UnpinnableVm : RecordedLifecycleViewModel, IRoutedViewModel
    {
        public UnpinnableVm() { CanBeCachedValue = false; }
    }

    /// <summary>
    /// A non-cacheable (and therefore non-pinnable) parent that breaks a pin chain.
    /// </summary>
    public class RootVm : DisposableVm, IRoutedViewModel
    {
        public RootVm() { CanBeCachedValue = false; }
    }

    /// <summary>
    /// A pinnable child that depends on its parent (it resolves the parent as a service).
    /// </summary>
    public class DependentVm : DisposableVm, IRoutedViewModel
    {
        public DependentVm(RootVm parent)
        {
            Parent = parent;
            CanBeCachedValue = false;
            CanBePinnedValue = true;
        }

        public RootVm Parent { get; }
    }

    /// <summary>
    /// A pinnable child that does not depend on its parent.
    /// </summary>
    public class IndependentVm : DisposableVm, IRoutedViewModel
    {
        public IndependentVm()
        {
            CanBeCachedValue = false;
            CanBePinnedValue = true;
        }
    }

    /// <summary>
    /// A cacheable child that depends on its parent.
    /// </summary>
    public class CachedDependentVm : DisposableVm, IRoutedViewModel
    {
        public CachedDependentVm(RootVm parent) => Parent = parent;

        public RootVm Parent { get; }
    }

    public class OtherVm : RecordedLifecycleViewModel, IRoutedViewModel
    {
    }

    public class RedirectingVm : RecordedLifecycleViewModel, IRoutedViewModel
    {
        public static NavigatorRoute? Target { get; set; }

        public RedirectingVm()
        {
            if (Target is { } target)
                RedirectOnNavigatedTo = Redirect.Navigate(target);
        }
    }
}
