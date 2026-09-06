using PrefixClassName.MsTest;
using Shouldly;
using Singulink.UI.Navigation.Testing;

namespace Singulink.UI.Navigation.Tests;

/// <summary>
/// Exercises the public <c>Singulink.UI.Navigation.Testing</c> package the way a consumer would.
/// </summary>
[PrefixTestClass]
public class TestingPackageTests
{
    [TestMethod]
    public void Navigate_ActivatesViewModels_AndRecordsLifecycle()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();

            await nav.NavigateAsync("main/details");

            nav.LastNavigationResult.ShouldBe(NavigationResult.Success);
            nav.ActiveViewModels.Count.ShouldBe(2);
            nav.ActiveViewModels[0].ShouldBeOfType<MainVm>();
            nav.ActiveViewModel<DetailsVm>().ShouldNotBeNull();

            nav.Events.OfType<ViewModelLifecycleEvent>().Select(e => e.Stage).ShouldBe([
                ViewModelLifecycleStage.NavigatedTo,
                ViewModelLifecycleStage.RouteNavigated,
                ViewModelLifecycleStage.NavigatedTo,
                ViewModelLifecycleStage.RouteNavigated,
            ]);

            nav.Events.OfType<ViewModelActivatedEvent>().Count().ShouldBe(2);
            nav.Events.OfType<NavigationStartedEvent>().ShouldHaveSingleItem();
            nav.Events.OfType<NavigationCompletedEvent>().Single().Result.ShouldBe(NavigationResult.Success);

            nav.ClearEvents();
            await nav.NavigateAsync("home");

            nav.ActiveViewModel<HomeVm>().ShouldNotBeNull();
            nav.Events.OfType<ViewModelDeactivatedEvent>().Count().ShouldBe(2);
            nav.Events.OfType<ViewModelLifecycleEvent>().Select(e => e.Stage).ShouldContain(ViewModelLifecycleStage.NavigatedAway);
        });
    }

    [TestMethod]
    public void Navigate_SetsParameter_AndInjectsServices()
    {
        NavigationTestContext.Run(async () =>
        {
            var service = new FakeService();
            var nav = BuildNav(services: new SingleServiceProvider(service));

            await nav.NavigateAsync("items/42");

            var item = nav.ActiveViewModel<ItemVm>();
            item.Parameter.ShouldBe(42);
            item.Service.ShouldBeSameAs(service);
        });
    }

    [TestMethod]
    public void Redirect_IsRecorded()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();

            await nav.NavigateAsync("redirecting");

            nav.ActiveViewModel<HomeVm>().ShouldNotBeNull();
            nav.Events.OfType<NavigationRedirectedEvent>().Single().ViewModel.ShouldBeOfType<RedirectingVm>();
        });
    }

    [TestMethod]
    public void DialogScript_ClosesDialogWithResult()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home");

            nav.OnDialogShown<ConfirmDialogVm>(dialog => dialog.Confirm());

            var home = nav.ActiveViewModel<HomeVm>();
            await home.DeleteAsync();
            await nav.WaitUntilIdleAsync();

            home.Deleted.ShouldBeTrue();
            nav.ShowingDialogs.ShouldBeEmpty();
            nav.Events.OfType<DialogShownEvent>().Single().ParentViewModel.ShouldBeNull();
            nav.Events.OfType<DialogClosedEvent>().ShouldHaveSingleItem();
        });
    }

    [TestMethod]
    public void UnscriptedDialog_StaysOpen_AndCanBeDrivenManually()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home");

            var home = nav.ActiveViewModel<HomeVm>();
            var deleteTask = home.DeleteAsync();

            var dialog = nav.TopDialog.ShouldBeOfType<ConfirmDialogVm>();
            nav.CanShowDialog.ShouldBeFalse();
            dialog.Navigator.CanShowDialog.ShouldBeTrue();

            dialog.Navigator.Close();
            await deleteTask;

            home.Deleted.ShouldBeFalse();
            nav.TopDialog.ShouldBeNull();
        });
    }

    [TestMethod]
    public void MessageDialog_ScriptedAnswer_IsReturned()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home");

            nav.OnMessageDialog(dialog => dialog.ButtonLabels.ToList().IndexOf("No"));

            int result = await nav.ShowMessageDialogAsync("Sure?", "Confirm", ["Yes", "No"]);

            result.ShouldBe(1);
            nav.Events.OfType<DialogShownEvent>().Single().ViewModel.ShouldBeOfType<MessageDialogViewModel>();
        });
    }

    [TestMethod]
    public void MessageDialog_Unscripted_FailsTest()
    {
        var ex = Should.Throw<InvalidOperationException>(() => NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home");
            await nav.ShowMessageDialogAsync("Something happened", "Oops");
        }));

        ex.Message.ShouldContain("Unexpected message dialog");
        ex.Message.ShouldContain("Oops");
    }

    [TestMethod]
    public void MessageDialog_AutoAccept_UsesDefaultButton()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            nav.AutoAcceptMessageDialogs = true;
            await nav.NavigateAsync("home");

            var options = new MessageDialogOptions("Pick", ["A", "B", "C"]) { DefaultButtonIndex = 2 };
            (await nav.ShowMessageDialogAsync(options)).ShouldBe(2);
        });
    }

    [TestMethod]
    public void DialogViewModel_TestedDirectly_WithNestedConfirmation()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            nav.OnMessageDialog(dialog => dialog.ButtonLabels.ToList().IndexOf("Yes"));

            var editor = new EditorDialogVm();
            var showTask = nav.ShowDialogAsync(editor);

            editor.Navigator.ShouldNotBeNull();
            editor.Text = "changed";

            nav.RequestDismissTop();
            await showTask;

            editor.DiscardConfirmed.ShouldBeTrue();
            nav.Events.OfType<DialogDismissRequestedEvent>().ShouldHaveSingleItem();
            nav.Events.OfType<DialogShownEvent>().Last().ParentViewModel.ShouldBeSameAs(editor);
        });
    }

    [TestMethod]
    public void WaitUntilIdle_WaitsForFireAndForgetWork()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home");

            var home = nav.ActiveViewModel<HomeVm>();
            home.StartBackgroundWork();

            nav.TaskRunner.IsBusy.ShouldBeTrue();
            await nav.WaitUntilIdleAsync();

            nav.TaskRunner.IsBusy.ShouldBeFalse();
            home.BackgroundWorkDone.ShouldBeTrue();
            nav.Events.OfType<BusyChangedEvent>().Count(e => e.DialogViewModel is null).ShouldBeGreaterThanOrEqualTo(2);
        });
    }

    [TestMethod]
    public void TestNavigator_OutsideContext_Throws()
    {
        Should.Throw<InvalidOperationException>(() => BuildNav());
    }

    [TestMethod]
    public void AttachNavigator_DialogViewModel_UsesAttachedNavigator()
    {
        var navigator = new RecordingDialogNavigator();
        var dialog = new ConfirmDialogVm();
        dialog.AttachNavigator(navigator);

        dialog.Navigator.ShouldBeSameAs(navigator);
        dialog.Confirm();

        navigator.CloseCount.ShouldBe(1);
        dialog.Result.ShouldBeTrue();
        Should.Throw<InvalidOperationException>(() => dialog.AttachNavigator(navigator));
    }

    [TestMethod]
    public void SetParameter_RoutedViewModel_ExposesParameter()
    {
        var item = new ItemVm(new FakeService());
        item.SetParameter(7);

        item.Parameter.ShouldBe(7);
    }

    private sealed class RecordingDialogNavigator : IDialogNavigator
    {
        public int CloseCount { get; private set; }

        public bool CanShowDialog => true;

        public IServiceProvider RootServices => throw new NotSupportedException();

        public Tasks.ITaskRunner TaskRunner => throw new NotSupportedException();

        public void Close() => CloseCount++;

        public TViewModel CreateDialogViewModel<TViewModel>(params object?[] explicitArgs) where TViewModel : class, IDialogViewModel => throw new NotSupportedException();

        public Task ShowDialogAsync(IDialogViewModel viewModel) => throw new NotSupportedException();

        public Task<TResult> ShowDialogAsync<TResult>(IDialogViewModel<TResult> viewModel) => throw new NotSupportedException();
    }

    private static TestNavigator BuildNav(IServiceProvider? services = null) => new(b =>
    {
        b.MapViewModel<HomeVm>();
        b.MapViewModel<ItemVm>();
        b.MapViewModel<MainVm>();
        b.MapViewModel<DetailsVm>();
        b.MapViewModel<RedirectingVm>();

        b.AddRoute(Routes.Home);
        b.AddRoute(Routes.Item);
        b.AddRoute(Routes.Main);
        b.AddRoute(Routes.MainDetails);
        b.AddRoute(Routes.Redirecting);

        if (services is not null)
            b.Services = services;
    });

    public static class Routes
    {
        public static readonly RootRoutePart<HomeVm> Home = Route.Build("home").Root<HomeVm>();
        public static readonly RootRoutePart<ItemVm, int> Item = Route.Build<int>(p => $"items/{p}").Root<ItemVm>();
        public static readonly RootRoutePart<MainVm> Main = Route.Build("main").Root<MainVm>();
        public static readonly ChildRoutePart<MainVm, DetailsVm> MainDetails = Route.Build("details").Child<MainVm, DetailsVm>();
        public static readonly RootRoutePart<RedirectingVm> Redirecting = Route.Build("redirecting").Root<RedirectingVm>();
    }

    public sealed class FakeService
    {
    }

    public sealed class SingleServiceProvider(object service) : IServiceProvider
    {
        public object? GetService(Type serviceType) => serviceType.IsInstanceOfType(service) ? service : null;
    }

    public class HomeVm : IRoutedViewModel
    {
        public bool Deleted { get; private set; }

        public bool BackgroundWorkDone { get; private set; }

        public async Task DeleteAsync()
        {
            if (await this.Navigator.ShowDialogAsync(new ConfirmDialogVm()))
                Deleted = true;
        }

        public void StartBackgroundWork()
        {
            this.TaskRunner.RunAsBusyAndForget(async () => {
                await Task.Delay(10);
                BackgroundWorkDone = true;
            });
        }
    }

    public class ItemVm(FakeService service) : IRoutedViewModel<int>
    {
        public FakeService Service => service;
    }

    public class MainVm : IRoutedViewModel
    {
    }

    public class DetailsVm : IRoutedViewModel
    {
    }

    public class RedirectingVm : IRoutedViewModel
    {
        public Task OnNavigatedToAsync(NavigationArgs args)
        {
            args.Redirect = Redirect.Navigate(Routes.Home);
            return Task.CompletedTask;
        }
    }

    public class ConfirmDialogVm : IDialogViewModel<bool>
    {
        public bool Result { get; private set; }

        public void Confirm()
        {
            Result = true;
            this.Navigator.Close();
        }
    }

    public class EditorDialogVm : IDismissibleDialogViewModel
    {
        public string Text { get; set; } = string.Empty;

        public bool DiscardConfirmed { get; private set; }

        public async Task OnDismissRequestedAsync()
        {
            if (Text.Length > 0)
            {
                int result = await this.Navigator.ShowMessageDialogAsync("Discard changes?", "Confirm", ["Yes", "No"]);

                if (result is not 0)
                    return;

                DiscardConfirmed = true;
            }

            this.Navigator.Close();
        }
    }
}
