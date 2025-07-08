using CommunityToolkit.Mvvm.Input;
using Singulink.UI.Navigation;
using Singulink.UI.Tasks;

namespace Playground.ViewModels.DialogTest;

public partial class DismissableDialogViewModel(IDialogNavigator navigator) : IDismissableDialogViewModel, IProvideTaskRunner
{
    public ITaskRunner TaskRunner { get => field ?? throw new InvalidOperationException("Task runner not set."); set; }

    [RelayCommand]
    public async Task DoSomethingAsync()
    {
        await TaskRunner.RunAsBusyAsync(async () => {
            // Simulate a long-running task
            await Task.Delay(1000);
        });

        // Show a message dialog after the task is done
        await navigator.ShowMessageDialogAsync("Task completed successfully!", "Success");
    }

    [RelayCommand]
    public void Close() => navigator.Close();

    public async void OnDismissRequested()
    {
        int result = await navigator.ShowMessageDialogAsync("Are you sure you want to close this dialog?", "Confirm", DialogButtonLabels.YesNo);

        if (result is 0)
            navigator.Close();
    }
}
