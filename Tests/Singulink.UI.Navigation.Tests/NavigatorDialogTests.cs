using PrefixClassName.MsTest;
using Shouldly;
using Singulink.UI.Navigation.Testing;

namespace Singulink.UI.Navigation.Tests;

[PrefixTestClass]
public class NavigatorDialogTests
{
    [TestMethod]
    public void ShowDialog_RecordsShowAndHide()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home");

            var dlg = new SimpleDialog();
            var task = nav.ShowDialogAsync(dlg);

            nav.IsShowingDialog.ShouldBeTrue();
            // The root view is what gets covered by a top-level dialog.
            DialogEvents(nav).ShouldBe([new LayerCoveredEvent(null, true), new DialogShownEvent(dlg, null)]);

            dlg.Navigator.Close();
            await task;

            nav.IsShowingDialog.ShouldBeFalse();
            DialogEvents(nav).ShouldBe([
                new LayerCoveredEvent(null, true),
                new DialogShownEvent(dlg, null),
                new DialogClosedEvent(dlg),
                new LayerCoveredEvent(null, false),
            ]);
        });
    }

    [TestMethod]
    public void ShowDialog_WithResult_ReturnsValue()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home");

            var dlg = new ResultDialog();
            var task = nav.ShowDialogAsync(dlg);
            dlg.SetResult("answer");
            (await task).ShouldBe("answer");
        });
    }

    [TestMethod]
    public void ShowDialog_OnDialogShownAsync_Invoked()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home");

            var dlg = new TrackingDialog();
            var task = nav.ShowDialogAsync(dlg);

            await Task.Yield();
            dlg.ShownInvoked.ShouldBeTrue();

            dlg.Navigator.Close();
            await task;
        });
    }

    [TestMethod]
    public void ShowMessageDialog_OK_Returns0()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home");

            MessageDialogViewModel? shown = null;

            nav.OnMessageDialog(vm => {
                shown = vm;
                return 0;
            });

            await nav.ShowMessageDialogAsync("hello");

            shown.ShouldNotBeNull();
            shown.Message.ShouldBe("hello");
        });
    }

    [TestMethod]
    public void ShowMessageDialog_WithButtons_ReturnsClickedIndex()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home");

            MessageDialogViewModel? shown = null;

            nav.OnMessageDialog(vm => {
                shown = vm;
                return 2;
            });

            (await nav.ShowMessageDialogAsync("msg", new[] { "Yes", "No", "Cancel" })).ShouldBe(2);

            shown.ShouldNotBeNull();
            shown.ButtonLabels.Count.ShouldBe(3);
        });
    }

    [TestMethod]
    public void NestedDialogs_Stack_AndUnwindInOrder()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home");

            var outer = new SimpleDialog();
            var inner = new SimpleDialog();

            var outerTask = nav.ShowDialogAsync(outer);
            var innerTask = outer.Navigator.ShowDialogAsync(inner);

            // Both dialogs show at once; the parent is never hidden while the child is showing. Each layer is reported as covered before its child shows.
            nav.ShowingDialogs.ShouldBe([outer, inner]);
            nav.IsShowingDialog.ShouldBeTrue();

            DialogEvents(nav).ShouldBe([
                new LayerCoveredEvent(null, true),
                new DialogShownEvent(outer, null),
                new LayerCoveredEvent(outer, true),
                new DialogShownEvent(inner, outer),
            ]);

            inner.Navigator.Close();
            await innerTask;

            // Closing the child hides only the child, uncovers the parent and restores focus to it instead of re-showing it.
            DialogEvents(nav).Skip(4).ShouldBe([
                new DialogClosedEvent(inner),
                new LayerCoveredEvent(outer, false),
                new DialogFocusRestoredEvent(outer),
            ]);

            nav.ShowingDialogs.ShouldBe([outer]);
            nav.IsShowingDialog.ShouldBeTrue();

            outer.Navigator.Close();
            await outerTask;

            // Closing the last dialog uncovers the root view; focus restoration is left to the framework there.
            DialogEvents(nav).Skip(7).ShouldBe([
                new DialogClosedEvent(outer),
                new LayerCoveredEvent(null, false),
            ]);

            nav.ShowingDialogs.ShouldBeEmpty();
            nav.IsShowingDialog.ShouldBeFalse();
        });
    }

    [TestMethod]
    public void CanShowDialog_TracksTopDialog()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home");

            nav.CanShowDialog.ShouldBeTrue();

            var outer = new SimpleDialog();
            var outerTask = nav.ShowDialogAsync(outer);

            nav.CanShowDialog.ShouldBeFalse();
            outer.Navigator.CanShowDialog.ShouldBeTrue();

            var inner = new SimpleDialog();
            var innerTask = outer.Navigator.ShowDialogAsync(inner);

            outer.Navigator.CanShowDialog.ShouldBeFalse();
            inner.Navigator.CanShowDialog.ShouldBeTrue();

            inner.Navigator.Close();
            await innerTask;

            outer.Navigator.CanShowDialog.ShouldBeTrue();
            inner.Navigator.CanShowDialog.ShouldBeFalse();

            outer.Navigator.Close();
            await outerTask;

            nav.CanShowDialog.ShouldBeTrue();
        });
    }

    [TestMethod]
    public void ShowDialog_FromNonTopPresenter_Throws()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home");

            var outer = new SimpleDialog();
            var outerTask = nav.ShowDialogAsync(outer);

            await Should.ThrowAsync<InvalidOperationException>(() => nav.ShowDialogAsync(new SimpleDialog()));

            var inner = new SimpleDialog();
            var innerTask = outer.Navigator.ShowDialogAsync(inner);

            await Should.ThrowAsync<InvalidOperationException>(() => outer.Navigator.ShowDialogAsync(new SimpleDialog()));

            inner.Navigator.Close();
            await innerTask;
            outer.Navigator.Close();
            await outerTask;
        });
    }

    [TestMethod]
    public void CloseDialog_NotTop_Throws()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home");

            var outer = new SimpleDialog();
            var outerTask = nav.ShowDialogAsync(outer);
            var inner = new SimpleDialog();
            var innerTask = outer.Navigator.ShowDialogAsync(inner);

            // A parent cannot be closed underneath a showing child.
            Should.Throw<InvalidOperationException>(() => outer.Navigator.Close());
            nav.ShowingDialogs.Count.ShouldBe(2);

            inner.Navigator.Close();
            await innerTask;
            outer.Navigator.Close();
            await outerTask;
        });
    }

    [TestMethod]
    public void DialogNavigator_ExposesRootServicesAndTaskRunner()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home");

            var dlg = new SimpleDialog();
            var task = nav.ShowDialogAsync(dlg);

            IDialogPresenter presenter = dlg.Navigator;
            presenter.RootServices.ShouldBeSameAs(nav.RootServices);
            presenter.TaskRunner.ShouldBeSameAs(dlg.Navigator.TaskRunner);

            dlg.Navigator.Close();
            await task;
        });
    }

    [TestMethod]
    public void Dialog_DismissRequest_ForwardedToDismissibleVm()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home");

            var dlg = new DismissibleDialog();
            var task = nav.ShowDialogAsync(dlg);

            // Simulate dismiss via MessageDialogViewModel-style cancel: invoke OnDismissRequestedAsync directly.
            await ((IDismissibleDialogViewModel)dlg).OnDismissRequestedAsync();

            await task;
            dlg.DismissedCalled.ShouldBeTrue();
        });
    }

    private static IEnumerable<NavigatorEvent> DialogEvents(TestNavigator nav) =>
        nav.Events.Where(e => e is DialogShownEvent or DialogClosedEvent or LayerCoveredEvent or DialogFocusRestoredEvent);

    private static TestNavigator BuildNav() => new(b =>
    {
        b.MapViewModel<HomeVm>();
        b.AddRoute(Route.Build("home").Root<HomeVm>());
    });

    public class HomeVm : IRoutedViewModel { }

    public class SimpleDialog : IDialogViewModel { }

    public class ResultDialog : IDialogViewModel<string>
    {
        private string? _result;

        public string Result => _result ?? throw new InvalidOperationException();

        public void SetResult(string result)
        {
            _result = result;
            this.Navigator.Close();
        }
    }

    public class TrackingDialog : IDialogViewModel
    {
        public bool ShownInvoked { get; private set; }

        public Task OnDialogShownAsync()
        {
            ShownInvoked = true;
            return Task.CompletedTask;
        }
    }

    public class DismissibleDialog : IDismissibleDialogViewModel
    {
        public bool DismissedCalled { get; private set; }

        public Task OnDismissRequestedAsync()
        {
            DismissedCalled = true;
            this.Navigator.Close();
            return Task.CompletedTask;
        }
    }
}
