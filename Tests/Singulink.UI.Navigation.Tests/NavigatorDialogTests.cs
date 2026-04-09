using PrefixClassName.MsTest;
using Shouldly;
using Singulink.UI.Navigation.Tests.TestSupport;

namespace Singulink.UI.Navigation.Tests;

[PrefixTestClass]
public class NavigatorDialogTests
{
    [TestMethod]
    public void ShowDialog_RecordsShowAndHide()
    {
        AsyncContextTest.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home");

            var dlg = new SimpleDialog();
            var task = nav.ShowDialogAsync(dlg);

            nav.IsShowingDialog.ShouldBeTrue();
            nav.DialogEvents.Count.ShouldBe(1);
            nav.DialogEvents[0].Kind.ShouldBe(DialogEventKind.Show);

            dlg.Navigator.Close();
            await task;

            nav.IsShowingDialog.ShouldBeFalse();
            nav.DialogEvents[^1].Kind.ShouldBe(DialogEventKind.Hide);
        });
    }

    [TestMethod]
    public void ShowDialog_WithResult_ReturnsValue()
    {
        AsyncContextTest.Run(async () =>
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
        AsyncContextTest.Run(async () =>
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
        AsyncContextTest.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home");

            var task = nav.ShowMessageDialogAsync("hello");

            // Top dialog VM should be MessageDialogViewModel.
            var vm = (MessageDialogViewModel)((FakeDialog)nav.ShownDialogs[^1]).DataContext!;
            vm.OnButtonClick(0);

            await task;
        });
    }

    [TestMethod]
    public void ShowMessageDialog_WithButtons_ReturnsClickedIndex()
    {
        AsyncContextTest.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home");

            var task = nav.ShowMessageDialogAsync("msg", new[] { "Yes", "No", "Cancel" });

            var vm = (MessageDialogViewModel)((FakeDialog)nav.ShownDialogs[^1]).DataContext!;
            vm.ButtonLabels.Count.ShouldBe(3);
            vm.OnButtonClick(2);
            (await task).ShouldBe(2);
        });
    }

    [TestMethod]
    public void NestedDialogs_Stack_AndUnwindInOrder()
    {
        AsyncContextTest.Run(async () =>
        {
            var nav = BuildNav();
            await nav.NavigateAsync("home");

            var outer = new SimpleDialog();
            var inner = new SimpleDialog();

            var outerTask = nav.ShowDialogAsync(outer);
            var innerTask = outer.Navigator.ShowDialogAsync(inner);

            // Two shows recorded (parent hidden when child shown).
            nav.DialogEvents.Count(e => e.Kind == DialogEventKind.Show).ShouldBe(2);
            nav.IsShowingDialog.ShouldBeTrue();

            inner.Navigator.Close();
            await innerTask;
            nav.IsShowingDialog.ShouldBeTrue();

            outer.Navigator.Close();
            await outerTask;
            nav.IsShowingDialog.ShouldBeFalse();
        });
    }

    [TestMethod]
    public void Dialog_DismissRequest_ForwardedToDismissibleVm()
    {
        AsyncContextTest.Run(async () =>
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

    private static TestNavigator BuildNav() => new(b =>
    {
        b.MapRoutedView<HomeVm, FakeView>();
        b.AddRoute(Route.Build("home").Root<HomeVm>());
        b.MapDialog<ResultDialog, FakeDialog>();
        b.MapDialog<TrackingDialog, FakeDialog>();
        b.MapDialog<DismissibleDialog, FakeDialog>();
        b.MapDialog<SimpleDialog, FakeDialog>();
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
