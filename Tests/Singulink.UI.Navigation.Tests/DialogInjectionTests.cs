using PrefixClassName.MsTest;
using Shouldly;
using Singulink.UI.Navigation.Testing;

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

            await nav.NavigateAsync("other");

            var ex = await Should.ThrowAsync<InvalidOperationException>(() => nav.ShowDialogAsync(dialog));
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

    private static TestNavigator BuildNav(RootService rootService) => new(b =>
    {
        b.MapViewModel<PageVm>();
        b.MapViewModel<OtherVm>();
        b.AddRoute(Routes.Page);
        b.AddRoute(Routes.Other);
        b.Services = new SingleServiceProvider(rootService);
    });

    public static class Routes
    {
        public static readonly RootRoutePart<PageVm> Page = Route.Build("page").Root<PageVm>();
        public static readonly RootRoutePart<OtherVm> Other = Route.Build("other").Root<OtherVm>();
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

    public class OptionalServicesDialogVm(RootService rootService, Draft? missing = null, int count = 3) : IDialogViewModel
    {
        public RootService RootService => rootService;

        public Draft? Missing => missing;

        public int Count => count;
    }
}
