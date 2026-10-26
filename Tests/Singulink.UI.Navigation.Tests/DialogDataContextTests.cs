using PrefixClassName.MsTest;
using Shouldly;
using Singulink.UI.Navigation.InternalServices;
using Singulink.UI.Navigation.Testing;
using Singulink.UI.Tasks;

namespace Singulink.UI.Navigation.Tests;

/// <summary>
/// Tests for when dialogs receive their view model as their data context. The navigator's dialogs record the view model's state at the moment it is set as
/// their data context, which is what a <c>DataContextChanged</c> handler in a dialog view observes.
/// </summary>
[PrefixTestClass]
public class DialogDataContextTests
{
    private const string AttachFailedMessage = "Attaching the view model failed.";

    [TestMethod]
    public void Create_DataContextIsSetOnceConstructorHasFinished()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = new RecordingNavigator();

            var viewModel = nav.CreateDialogViewModel<ProbeDialogVm>("value");

            var dialog = nav.Dialogs.ShouldHaveSingleItem();
            dialog.DataContextSets.ShouldBe([new DataContextSnapshot(viewModel, "value", viewModel.Navigator)]);
            dialog.DataContext.ShouldBeSameAs(viewModel);

            // The constructor could use the navigator and the dialog's task runner.
            viewModel.NavigatorInCtor.ShouldBeSameAs(viewModel.Navigator);
            viewModel.TaskRunnerInCtor.ShouldBeSameAs(dialog.TaskRunner);

            // Showing the dialog does not set its data context again.
            var showTask = nav.ShowDialogAsync(viewModel);
            viewModel.Navigator.Close();
            await showTask;

            dialog.DataContextSets.Count.ShouldBe(1);
        });
    }

    [TestMethod]
    public void Create_ConstructorThrows_DialogNeverReceivesViewModel()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = new RecordingNavigator();
            var leak = new Leak();

            var ex = Should.Throw<Exception>(() => nav.CreateDialogViewModel<ThrowingDialogVm>(leak));
            ex.GetBaseException().Message.ShouldBe(ThrowingDialogVm.ErrorMessage);

            var dialog = nav.Dialogs.ShouldHaveSingleItem();
            dialog.DataContextSets.ShouldBeEmpty();
            dialog.DataContext.ShouldBeNull();

            // The constructor could use its navigator before it threw, but the navigator is not left associated with it.
            var leaked = leak.ViewModel.ShouldNotBeNull();
            leaked.NavigatorInCtor.ShouldNotBeNull();
            Should.Throw<InvalidOperationException>(() => leaked.Navigator);

            var viewModel = nav.CreateDialogViewModel<ProbeDialogVm>("value");
            var showTask = nav.ShowDialogAsync(viewModel);
            nav.TopDialog.ShouldBeSameAs(viewModel);
            viewModel.Navigator.Close();
            await showTask;
        });
    }

    [TestMethod]
    public void Show_CallerConstructedViewModel_DataContextIsSetWithNavigatorAvailable()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = new RecordingNavigator();
            var viewModel = new ConstructedDialogVm("value");

            var showTask = nav.ShowDialogAsync(viewModel);

            var dialog = nav.Dialogs.ShouldHaveSingleItem();
            dialog.DataContextSets.ShouldBe([new DataContextSnapshot(viewModel, "value", viewModel.Navigator)]);
            nav.TopDialog.ShouldBeSameAs(viewModel);

            viewModel.Navigator.Close();
            await showTask;

            // Showing it again reuses the dialog without setting its data context again.
            showTask = nav.ShowDialogAsync(viewModel);
            nav.Dialogs.ShouldHaveSingleItem().ShouldBeSameAs(dialog);
            viewModel.Navigator.Close();
            await showTask;

            dialog.DataContextSets.Count.ShouldBe(1);
        });
    }

    [TestMethod]
    public void Show_AttachingViewModelFails_LeavesViewModelUnassociated()
    {
        NavigationTestContext.Run(async () =>
        {
            var nav = new RecordingNavigator { FailAttach = true };
            var viewModel = new ConstructedDialogVm("value");

            var ex = await Should.ThrowAsync<InvalidOperationException>(() => nav.ShowDialogAsync(viewModel));
            ex.Message.ShouldBe(AttachFailedMessage);

            Should.Throw<InvalidOperationException>(() => viewModel.Navigator);
            nav.TopDialog.ShouldBeNull();

            // Showing it again starts over with a new dialog.
            nav.FailAttach = false;
            var showTask = nav.ShowDialogAsync(viewModel);

            nav.Dialogs.Count.ShouldBe(2);
            nav.Dialogs[1].DataContextSets.ShouldBe([new DataContextSnapshot(viewModel, "value", viewModel.Navigator)]);

            viewModel.Navigator.Close();
            await showTask;
        });
    }

    public interface IValueDialogVm : IDialogViewModel
    {
        public string? Value { get; }
    }

    public class ProbeDialogVm : IValueDialogVm
    {
        public ProbeDialogVm(string value)
        {
            Value = value;
            NavigatorInCtor = this.Navigator;
            TaskRunnerInCtor = this.TaskRunner;
        }

        public string Value { get; }

        public IDialogNavigator NavigatorInCtor { get; }

        public ITaskRunner TaskRunnerInCtor { get; }
    }

    public class ConstructedDialogVm(string value) : IValueDialogVm
    {
        public string Value => value;
    }

    public sealed class Leak
    {
        public ThrowingDialogVm? ViewModel { get; set; }
    }

    public class ThrowingDialogVm : IDialogViewModel
    {
        public const string ErrorMessage = "The dialog view model constructor failed.";

        public ThrowingDialogVm(Leak leak)
        {
            leak.ViewModel = this;
            NavigatorInCtor = this.Navigator;

            throw new InvalidOperationException(ErrorMessage);
        }

        public IDialogNavigator NavigatorInCtor { get; }
    }

    /// <summary>
    /// The state of a view model when it was set as a dialog's data context.
    /// </summary>
    private sealed record DataContextSnapshot(IDialogViewModel? ViewModel, string? Value, IDialogNavigator? Navigator);

    private sealed class RecordingDialog
    {
        public ITaskRunner? TaskRunner { get; set; }

        public List<DataContextSnapshot> DataContextSets { get; } = [];

        public IDialogViewModel? DataContext
        {
            get;
            set {
                field = value;
                DataContextSets.Add(new(value, (value as IValueDialogVm)?.Value, value is null ? null : MixinManager.GetNavigator(value)));
            }
        }
    }

    /// <summary>
    /// A navigator without routes whose dialogs are <see cref="RecordingDialog"/> instances.
    /// </summary>
    private sealed class RecordingNavigator() : NavigatorCore(new object(), new TaskRunner(), new Builder())
    {
        public List<RecordingDialog> Dialogs { get; } = [];

        public bool FailAttach { get; set; }

        public IDialogViewModel? TopDialog => TryGetTopDialog()?.ViewModel;

        protected override void EnsureThreadAccess() { }

        protected override bool CloseLightDismissPopups() => false;

        protected override void WireView(object view, IRoutedViewModelBase viewModel, out object? childViewNavigator) => throw new NotSupportedException();

        protected override void SetActiveView(object viewNavigator, object? view) => throw new NotSupportedException();

        protected override object? CreateDefaultDialog(Type viewModelType)
        {
            var dialog = new RecordingDialog();
            Dialogs.Add(dialog);
            return dialog;
        }

        protected override void WireDialog(object dialog, out ITaskRunner taskRunner)
        {
            var recordingDialog = (RecordingDialog)dialog;
            recordingDialog.TaskRunner = taskRunner = new TaskRunner();
        }

        protected override void AttachDialogViewModel(object dialog, IDialogViewModel viewModel)
        {
            ((RecordingDialog)dialog).DataContext = viewModel;

            // Fails the way a dialog view's DataContextChanged handler that throws would.
            if (FailAttach)
                throw new InvalidOperationException(AttachFailedMessage);
        }

        protected override void StartShowingDialog(object dialog) { }

        protected override void HideDialog(object dialog) { }

        private sealed class Builder : NavigatorBuilderCore
        {
            protected override Type RequiredParentViewType => typeof(object);
        }
    }
}
