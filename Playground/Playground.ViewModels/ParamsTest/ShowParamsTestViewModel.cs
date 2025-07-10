using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Singulink.UI.Navigation;

namespace Playground.ViewModels.ParamsTest;

public partial class ShowParamsTestViewModel : ObservableObject, IRoutedViewModel<(int IntValue, string StringValue)>
{
    public INavigator Navigator => this.GetNavigator();

    public int IntValue => this.GetParameter().IntValue;

    public string StringValue => this.GetParameter().StringValue;

    [RelayCommand]
    public async Task GoBackAsync() => await Navigator.GoBackAsync();
}
