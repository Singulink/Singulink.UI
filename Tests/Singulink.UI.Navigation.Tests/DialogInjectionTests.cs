using PrefixClassName.MsTest;
using Shouldly;
using Singulink.UI.Navigation.Testing;
using Singulink.UI.Tasks;

namespace Singulink.UI.Navigation.Tests;

/// <summary>
/// Tests for dialog view models created through <see cref="IDialogPresenter.CreateDialogViewModel{TViewModel}(object?[])"/>.
/// </summary>
[PrefixTestClass]
public class DialogInjectionTests
{
    [TestMethod]
    public void Create_InjectsExplicitArgsRootAndPageServices_AndNavigatorInConstructor()
    {
        NavigationTestContext.Run(async () =>
        {
            var rootService = new RootService();
            var nav = BuildNav(rootService);
            await nav.NavigateAsync("page");

            var page = nav.ActiveViewModel<PageVm>();
            var item = new Item("one");

            var dialog = nav.CreateDialogViewModel<EditDialogVm>(item);

            dialog.Item.ShouldBeSameAs(item);
            dialog.RootService.ShouldBeSameAs(rootService);
            dialog.PageService.ShouldBeSameAs(page.PageService);
            dialog.NavigatorInCtor.ShouldNotBeNull();
            dialog.NavigatorInCtor.RootServices.ShouldBeSameAs(nav.RootServices);

            var showTask = nav.ShowDialogAsync(dialog);
            nav.TopDialog.ShouldBeSameAs(dialog);
            dialog.Navigator.Close();
            await showTask;
        });
    }

    [TestMethod]
    public void Create_ConstructorRunsBusyWorkOnTaskRunner_RecordedForTheDialog()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav(new RootService());
            await nav.NavigateAsync("page");
            nav.ClearEvents();

            var work = new TaskCompletionSource();
            var dialog = nav.CreateDialogViewModel<BusyInCtorDialogVm>(work.Task);

            dialog.TaskRunnerInCtor.ShouldBeSameAs(dialog.Navigator.TaskRunner);
            dialog.Navigator.TaskRunner.IsBusy.ShouldBeTrue();
            nav.Events.OfType<BusyChangedEvent>().ShouldBe([new BusyChangedEvent(dialog, true)]);

            work.SetResult();
            await nav.WaitUntilIdleAsync();

            dialog.Navigator.TaskRunner.IsBusy.ShouldBeFalse();
            nav.Events.OfType<BusyChangedEvent>().ShouldBe([new BusyChangedEvent(dialog, true), new BusyChangedEvent(dialog, false)]);
        });
    }

    [TestMethod]
    public void Create_ConstructorThrows_LeavesNothingWired()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav(new RootService());
            await nav.NavigateAsync("page");
            nav.ClearEvents();

            var work = new TaskCompletionSource();
            var leak = new Leak();

            var ex = Should.Throw<Exception>(() => nav.CreateDialogViewModel<ThrowingDialogVm>(work.Task, leak));
            ex.GetBaseException().Message.ShouldBe(ThrowingDialogVm.ErrorMessage);

            // The constructor leaked itself after starting busy work on its task runner, but neither its navigator nor that work is attributed to it.
            var leaked = leak.ViewModel.ShouldNotBeNull();
            Should.Throw<InvalidOperationException>(() => leaked.Navigator);
            nav.Events.OfType<BusyChangedEvent>().ShouldBeEmpty();

            work.SetResult();
            await nav.WaitUntilIdleAsync();

            nav.Events.OfType<BusyChangedEvent>().ShouldBeEmpty();
            nav.CanShowDialog.ShouldBeTrue();

            // Creating and showing dialogs is unaffected.
            var dialog = nav.CreateDialogViewModel<TwoStringsDialogVm>("a", "b");
            var task = nav.ShowDialogAsync(dialog);
            nav.TopDialog.ShouldBeSameAs(dialog);
            dialog.Navigator.Close();
            await task;
        });
    }

    [TestMethod]
    public void Create_OptionalAndNullableServices_AreAllowedToBeMissing()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav(new RootService());
            await nav.NavigateAsync("page");

            var dialog = nav.CreateDialogViewModel<OptionalServicesDialogVm>();

            dialog.Missing.ShouldBeNull();
            dialog.Count.ShouldBe(3);
        });
    }

    [TestMethod]
    public void Create_SameTypedExplicitArgs_MatchPositionally()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav(new RootService());
            await nav.NavigateAsync("page");

            var dialog = nav.CreateDialogViewModel<TwoStringsDialogVm>("Title", "Message");

            dialog.Title.ShouldBe("Title");
            dialog.Message.ShouldBe("Message");
        });
    }

    [TestMethod]
    public void Create_UnmatchedExplicitArg_Throws()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav(new RootService());
            await nav.NavigateAsync("page");

            var ex = Should.Throw<ArgumentException>(() => nav.CreateDialogViewModel<TwoStringsDialogVm>("Title", "Message", 42));
            ex.Message.ShouldContain("Int32");
        });
    }

    [TestMethod]
    public void Create_MissingRequiredService_Throws()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav(new RootService());
            await nav.NavigateAsync("page");

            // No Item explicit argument and no service provides one.
            var ex = Should.Throw<InvalidOperationException>(() => nav.CreateDialogViewModel<EditDialogVm>());
            ex.Message.ShouldContain("item");
        });
    }

    [TestMethod]
    public void NestedDialog_GetsParentDialogChildService()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav(new RootService());
            await nav.NavigateAsync("page");

            var parent = nav.CreateDialogViewModel<EditDialogVm>(new Item("one"));
            var parentTask = nav.ShowDialogAsync(parent);

            var picker = parent.Navigator.CreateDialogViewModel<PickerDialogVm>();

            picker.Draft.ShouldBeSameAs(parent.Draft);
            picker.PageService.ShouldBeSameAs(nav.ActiveViewModel<PageVm>().PageService);

            var pickerTask = parent.Navigator.ShowDialogAsync(picker);
            nav.ShowingDialogs.ShouldBe([parent, picker]);

            picker.Navigator.Close();
            await pickerTask;
            parent.Navigator.Close();
            await parentTask;
        });
    }

    [TestMethod]
    public void CreatedDialog_ShownByDifferentPresenter_Throws()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav(new RootService());
            await nav.NavigateAsync("page");

            var createdByRoot = nav.CreateDialogViewModel<TwoStringsDialogVm>("a", "b");

            var parent = nav.CreateDialogViewModel<EditDialogVm>(new Item("one"));
            var parentTask = nav.ShowDialogAsync(parent);

            var ex = await Should.ThrowAsync<InvalidOperationException>(() => parent.Navigator.ShowDialogAsync(createdByRoot));
            ex.Message.ShouldContain("created");

            parent.Navigator.Close();
            await parentTask;

            // The creating presenter can still show it.
            var task = nav.ShowDialogAsync(createdByRoot);
            createdByRoot.Navigator.Close();
            await task;
        });
    }

    [TestMethod]
    public void CreatedDialog_StaleProvider_Throws()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav(new RootService());
            await nav.NavigateAsync("page");

            // Receives PageService from the active page.
            var dialog = nav.CreateDialogViewModel<EditDialogVm>(new Item("one"));
            dialog.PageService.ShouldBeSameAs(nav.ActiveViewModel<PageVm>().PageService);

            await nav.NavigateAsync("other");

            // Checked before awaiting, so a regression fails here instead of waiting for a dialog that never closes.
            var showTask = nav.ShowDialogAsync(dialog);
            showTask.IsFaulted.ShouldBeTrue();

            var ex = await Should.ThrowAsync<InvalidOperationException>(() => showTask);
            ex.Message.ShouldContain("no longer active");
        });
    }

    [TestMethod]
    public void CreatedDialog_WithoutScopedProviders_CanBeShownAfterNavigation()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav(new RootService());
            await nav.NavigateAsync("page");

            // Only explicit args and root services: nothing scoped, so navigation does not invalidate it.
            var dialog = nav.CreateDialogViewModel<TwoStringsDialogVm>("a", "b");

            await nav.NavigateAsync("other");

            var task = nav.ShowDialogAsync(dialog);
            dialog.Navigator.Close();
            await task;
        });
    }

    [TestMethod]
    public void Create_FromNonTopDialog_Throws()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav(new RootService());
            await nav.NavigateAsync("page");

            var parent = nav.CreateDialogViewModel<EditDialogVm>(new Item("one"));
            var parentTask = nav.ShowDialogAsync(parent);
            var child = parent.Navigator.CreateDialogViewModel<PickerDialogVm>();
            var childTask = parent.Navigator.ShowDialogAsync(child);

            Should.Throw<InvalidOperationException>(() => parent.Navigator.CreateDialogViewModel<PickerDialogVm>());

            child.Navigator.Close();
            await childTask;
            parent.Navigator.Close();
            await parentTask;
        });
    }

    [TestMethod]
    public void Create_RouteViewModelImplementingParameterType_SuppliesItself()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNameProviderNav();
            await nav.NavigateAsync("outer/plain");

            // The leaf does not implement the type, so the root page supplies itself ahead of the root service.
            var dialog = nav.CreateDialogViewModel<NameDialogVm>();

            dialog.NameProvider.ShouldBeSameAs(nav.ActiveViewModel<OuterVm>());

            var showTask = nav.ShowDialogAsync(dialog);
            nav.TopDialog.ShouldBeSameAs(dialog);
            dialog.Navigator.Close();
            await showTask;
        });
    }

    [TestMethod]
    public void Create_NearestRouteViewModelImplementingParameterType_Wins()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNameProviderNav();
            await nav.NavigateAsync("outer/inner");

            var dialog = nav.CreateDialogViewModel<NameDialogVm>();

            dialog.NameProvider.ShouldBeSameAs(nav.ActiveViewModel<InnerVm>());
        });
    }

    [TestMethod]
    public void Create_RegisteredChildService_BeatsSelfImplementationOnSameViewModel()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNameProviderNav();
            await nav.NavigateAsync("registering");

            var dialog = nav.CreateDialogViewModel<NameDialogVm>();

            dialog.NameProvider.ShouldBeSameAs(nav.ActiveViewModel<RegisteringVm>().Registered);
        });
    }

    [TestMethod]
    public void Create_NearerSelfImplementation_BeatsFartherRegisteredChildService()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNameProviderNav();
            await nav.NavigateAsync("registering/inner");

            var dialog = nav.CreateDialogViewModel<NameDialogVm>();

            dialog.NameProvider.ShouldBeSameAs(nav.ActiveViewModel<InnerVm>());
        });
    }

    [TestMethod]
    public void NestedDialog_ParentDialogImplementingParameterType_SuppliesItself()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNameProviderNav();
            await nav.NavigateAsync("outer/inner");

            var parent = nav.CreateDialogViewModel<NameProvidingDialogVm>();
            var parentTask = nav.ShowDialogAsync(parent);

            // The parent dialog is nearer than the pages that also implement the type.
            var nested = parent.Navigator.CreateDialogViewModel<NameDialogVm>();

            nested.NameProvider.ShouldBeSameAs(parent);

            var nestedTask = parent.Navigator.ShowDialogAsync(nested);
            nav.ShowingDialogs.ShouldBe([parent, nested]);

            nested.Navigator.Close();
            await nestedTask;
            parent.Navigator.Close();
            await parentTask;
        });
    }

    [TestMethod]
    public void CreatedDialog_SelfSupplyingProviderNoLongerActive_Throws()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNameProviderNav();
            await nav.NavigateAsync("outer/inner");

            var dialog = nav.CreateDialogViewModel<NameDialogVm>();
            dialog.NameProvider.ShouldBeSameAs(nav.ActiveViewModel<InnerVm>());

            await nav.NavigateAsync("outer/plain");

            // Checked before awaiting, so a regression fails here instead of waiting for a dialog that never closes.
            var showTask = nav.ShowDialogAsync(dialog);
            showTask.IsFaulted.ShouldBeTrue();

            var ex = await Should.ThrowAsync<InvalidOperationException>(() => showTask);
            ex.Message.ShouldContain("no longer active");
            ex.Message.ShouldContain(nameof(InnerVm));
        });
    }

    private static TestNavigator BuildNav(RootService rootService) => new(b =>
    {
        b.MapViewModel<PageVm>();
        b.MapViewModel<OtherVm>();
        b.AddRoute(Routes.Page);
        b.AddRoute(Routes.Other);
        b.Services = new SingleServiceProvider(rootService);
    });

    private static TestNavigator BuildNameProviderNav() => new(b =>
    {
        b.MapViewModel<OuterVm>();
        b.MapViewModel<InnerVm>();
        b.MapViewModel<PlainVm>();
        b.MapViewModel<RegisteringVm>();
        b.AddRoute(Routes.Outer);
        b.AddRoute(Routes.OuterInner);
        b.AddRoute(Routes.OuterPlain);
        b.AddRoute(Routes.Registering);
        b.AddRoute(Routes.RegisteringInner);
        b.Services = new SingleServiceProvider(new NamedProvider("root"));
    });

    public static class Routes
    {
        public static readonly RootRoutePart<PageVm> Page = Route.Build("page").Root<PageVm>();
        public static readonly RootRoutePart<OtherVm> Other = Route.Build("other").Root<OtherVm>();

        public static readonly RootRoutePart<OuterVm> Outer = Route.Build("outer").Root<OuterVm>();
        public static readonly ChildRoutePart<OuterVm, InnerVm> OuterInner = Route.Build("inner").Child<OuterVm, InnerVm>();
        public static readonly ChildRoutePart<OuterVm, PlainVm> OuterPlain = Route.Build("plain").Child<OuterVm, PlainVm>();
        public static readonly RootRoutePart<RegisteringVm> Registering = Route.Build("registering").Root<RegisteringVm>();
        public static readonly ChildRoutePart<RegisteringVm, InnerVm> RegisteringInner = Route.Build("inner").Child<RegisteringVm, InnerVm>();
    }

    public interface INameProvider
    {
        public string Name { get; }
    }

    public sealed class NamedProvider(string name) : INameProvider
    {
        public string Name => name;
    }

    public class OuterVm : IRoutedViewModel, INameProvider
    {
        public string Name => "outer";
    }

    public class InnerVm : IRoutedViewModel, INameProvider
    {
        public string Name => "inner";
    }

    public class PlainVm : IRoutedViewModel
    {
    }

    public class RegisteringVm : IRoutedViewModel, INameProvider
    {
        public RegisteringVm()
        {
            this.SetChildService<INameProvider>(Registered);
        }

        public NamedProvider Registered { get; } = new("registered");

        public string Name => "self";
    }

    public class NameDialogVm(INameProvider nameProvider) : IDialogViewModel
    {
        public INameProvider NameProvider => nameProvider;
    }

    public class NameProvidingDialogVm : IDialogViewModel, INameProvider
    {
        public string Name => "dialog";
    }

    public sealed record Item(string Name);

    public sealed class RootService;

    public sealed class PageService;

    public sealed class Draft;

    public sealed class SingleServiceProvider(object service) : IServiceProvider
    {
        public object? GetService(Type serviceType) => serviceType.IsInstanceOfType(service) ? service : null;
    }

    public class PageVm : IRoutedViewModel
    {
        public PageVm()
        {
            this.SetChildService(PageService);
        }

        public PageService PageService { get; } = new();
    }

    public class OtherVm : IRoutedViewModel
    {
    }

    public class EditDialogVm : IDialogViewModel<bool>
    {
        public EditDialogVm(Item item, RootService rootService, PageService pageService)
        {
            Item = item;
            RootService = rootService;
            PageService = pageService;
            NavigatorInCtor = this.Navigator;

            this.SetChildService(Draft);
        }

        public Item Item { get; }

        public RootService RootService { get; }

        public PageService PageService { get; }

        public IDialogNavigator NavigatorInCtor { get; }

        public Draft Draft { get; } = new();

        public bool Result { get; private set; }
    }

    public class PickerDialogVm(Draft draft, PageService pageService) : IDialogViewModel
    {
        public Draft Draft => draft;

        public PageService PageService => pageService;
    }

    public class TwoStringsDialogVm(string title, string message) : IDialogViewModel
    {
        public string Title => title;

        public string Message => message;
    }

    public class BusyInCtorDialogVm : IDialogViewModel
    {
        public BusyInCtorDialogVm(Task work)
        {
            TaskRunnerInCtor = this.TaskRunner;
            TaskRunnerInCtor.RunAsBusyAndForget(work);
        }

        public ITaskRunner TaskRunnerInCtor { get; }
    }

    public sealed class Leak
    {
        public ThrowingDialogVm? ViewModel { get; set; }
    }

    public class ThrowingDialogVm : IDialogViewModel
    {
        public const string ErrorMessage = "The dialog view model constructor failed.";

        public ThrowingDialogVm(Task work, Leak leak)
        {
            leak.ViewModel = this;
            this.TaskRunner.RunAsBusyAndForget(work);

            throw new InvalidOperationException(ErrorMessage);
        }
    }

    public class OptionalServicesDialogVm(RootService rootService, Draft? missing = null, int count = 3) : IDialogViewModel
    {
        public RootService RootService => rootService;

        public Draft? Missing => missing;

        public int Count => count;
    }
}
