using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Singulink.UI.Navigation;

namespace Playground.ViewModels.DialogTest;

public partial class DialogTestViewModel : ObservableObject, IRoutedViewModel
{
    [RelayCommand]
    public async Task ShowDialogAsync()
    {
        await this.Navigator.ShowDialogAsync(new DismissableDialogViewModel());
    }

    [RelayCommand]
    public async Task ShowTwoDialogsAsync()
    {
        await this.Navigator.ShowDialogAsync(new DismissableDialogViewModel());
        await this.Navigator.ShowDialogAsync(new DismissableDialogViewModel());
    }
}
