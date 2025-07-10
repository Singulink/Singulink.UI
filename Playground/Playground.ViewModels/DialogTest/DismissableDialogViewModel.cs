using CommunityToolkit.Mvvm.Input;
using Singulink.UI.Navigation;

namespace Playground.ViewModels.DialogTest;

public partial class DismissableDialogViewModel : IDismissableDialogViewModel
{
    public IDialogNavigator Navigator => this.GetNavigator();

    [RelayCommand]
    public async Task DoSomethingAsync()
    {
        await Navigator.TaskRunner.RunAsBusyAsync(async () => {
            // Simulate a long-running task
            await Task.Delay(1500);
        });

        // Show a message dialog after the task is done
        await Navigator.ShowMessageDialogAsync("Task completed successfully!", "Success");
    }

    [RelayCommand]
    public void Close() => Navigator.Close();

    public async void OnDismissRequested()
    {
        int result = await Navigator.ShowMessageDialogAsync("Are you sure you want to close this dialog?", "Confirm", DialogButtonLabels.YesNo);

        if (result is 0)
            Navigator.Close();
    }
}
