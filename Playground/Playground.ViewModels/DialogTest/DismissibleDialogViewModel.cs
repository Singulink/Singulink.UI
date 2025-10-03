using CommunityToolkit.Mvvm.Input;
using Singulink.UI.Navigation;

namespace Playground.ViewModels.DialogTest;

public partial class DismissibleDialogViewModel : IDismissibleDialogViewModel
{
    [RelayCommand]
    public async Task DoSomethingAsync()
    {
        await this.TaskRunner.RunAsBusyAsync(async () => {
            // Simulate a long-running task
            await Task.Delay(1500);
        });

        // Show a message dialog after the task is done
        await this.Navigator.ShowMessageDialogAsync("Task completed successfully!", "Success");
    }

    [RelayCommand]
    public void Close() => this.Navigator.Close();

    public async Task OnDismissRequestedAsync()
    {
        int result = await this.Navigator.ShowMessageDialogAsync("Are you sure you want to close this dialog?", "Confirm", DialogButtonLabels.YesNo);

        if (result is 0)
            this.Navigator.Close();
    }
}
