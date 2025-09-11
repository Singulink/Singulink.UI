using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Singulink.UI.Navigation;

namespace Playground.ViewModels.ParamsTest;

public partial class ShowParamsTestViewModel : ObservableObject, IRoutedViewModel<(int IntValue, string StringValue)>
{
    public int IntValue => this.Parameter.IntValue;

    public string StringValue => this.Parameter.StringValue;

    [RelayCommand]
    public async Task GoBackAsync()
    {
        await this.Navigator.NavigatePartialAsync(Routes.Main.ParamsTestChild);
    }
}
