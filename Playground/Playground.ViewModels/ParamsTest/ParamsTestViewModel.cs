using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Singulink.UI.Navigation;

namespace Playground.ViewModels.ParamsTest;

public partial class ParamsTestViewModel : ObservableObject, IRoutedViewModel
{
    [ObservableProperty]
    public partial int IntValue { get; set; }

    [ObservableProperty]
    public partial string StringValue { get; set; } = string.Empty;

    public INavigator Navigator => this.GetNavigator();

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
