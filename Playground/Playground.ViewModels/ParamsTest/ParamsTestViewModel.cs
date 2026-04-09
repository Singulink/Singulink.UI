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

    [RelayCommand]
    public async Task NavigateWithParameters()
    {
        // Navigate to the ShowParamsViewModel with the current IntValue and StringValue

        try
        {
            string? stringValue = StringValue.Length is 0 ? null : StringValue;
            await this.Navigator.NavigatePartialAsync(Routes.Main.ShowParamsTestChild.ToConcrete(new ShowParamsTestViewModel.Params { IntValue = IntValue, StringValue = stringValue }));
        }
        catch (Exception ex)
        {
            _ = this.Navigator.ShowMessageDialogAsync(ex.Message, "Error");
        }
    }
}
