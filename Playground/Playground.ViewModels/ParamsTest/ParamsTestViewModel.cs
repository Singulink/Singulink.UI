using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Singulink.UI.Navigation;
using Singulink.UI.Navigation.MvvmToolkit;

namespace Playground.ViewModels.ParamsTest;

public partial class ParamsTestViewModel : RoutedObservableViewModel
{
    [ObservableProperty]
    public partial int IntValue { get; set; }

    [ObservableProperty]
    public partial string StringValue { get; set; } = string.Empty;

    public INavigator Navigator { get => field ?? throw new InvalidOperationException("Navigator not set."); set; }

    public override ValueTask OnNavigatedToAsync(INavigator navigator, NavigationArgs args)
    {
        Navigator = navigator;
        return ValueTask.CompletedTask;
    }

    [RelayCommand]
    public async Task NavigateWithParameters()
    {
        // Navigate to the ShowParamsViewModel with the current IntValue and StringValue

        try
        {
            await Navigator.NavigatePartialAsync(Routes.Main.ShowParamsTestRoute.GetConcrete((IntValue, StringValue)));
        }
        catch (Exception ex)
        {
            _ = Navigator.ShowMessageDialogAsync(ex.Message, "Error");
        }
    }
}
