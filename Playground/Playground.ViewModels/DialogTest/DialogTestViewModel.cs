using CommunityToolkit.Mvvm.Input;
using Singulink.UI.Navigation;

namespace Playground.ViewModels.DialogTest;

public partial class DialogTestViewModel : RoutedViewModel
{
    public INavigator Navigator { get => field ?? throw new InvalidOperationException("Navigator not set."); set; }

    public override ValueTask OnNavigatedToAsync(INavigator navigator, NavigationArgs args)
    {
        Navigator = navigator;
        return ValueTask.CompletedTask;
    }

    [RelayCommand]
    public async Task ShowDialogAsync()
    {
        await Navigator.ShowDialogAsync(nav => new DismissableDialogViewModel(nav));
    }

    [RelayCommand]
    public async Task ShowTwoDialogsAsync()
    {
        await Navigator.ShowDialogAsync(nav => new DismissableDialogViewModel(nav));
        await Navigator.ShowDialogAsync(nav => new DismissableDialogViewModel(nav));
    }
}
