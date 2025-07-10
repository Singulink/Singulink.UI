using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Singulink.UI.Navigation;

namespace Playground.ViewModels.DialogTest;

public partial class DialogTestViewModel : ObservableObject, IRoutedViewModel
{
    public INavigator Navigator => this.GetNavigator();

    [RelayCommand]
    public async Task ShowDialogAsync()
    {
        await Navigator.ShowDialogAsync(new DismissableDialogViewModel());
    }

    [RelayCommand]
    public async Task ShowTwoDialogsAsync()
    {
        await Navigator.ShowDialogAsync(new DismissableDialogViewModel());
        await Navigator.ShowDialogAsync(new DismissableDialogViewModel());
    }
}
