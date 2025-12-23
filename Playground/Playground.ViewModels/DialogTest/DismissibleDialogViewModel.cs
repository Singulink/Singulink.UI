using CommunityToolkit.Mvvm.Input;
using Singulink.UI.Navigation;
using Singulink.UI.Tasks;

namespace Playground.ViewModels.DialogTest;

public partial class DismissibleDialogViewModel : IDismissibleDialogViewModel
{
    [RelayCommand]
    public async Task DoSomethingAsync()
    {
        using var scope = this.TaskRunner.EnterBusyScope();

        await Task.Delay(1500);

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
