using CommunityToolkit.Mvvm.Input;
using Singulink.UI.Navigation;

namespace Playground.ViewModels.ParamsTest;

public partial class ShowParamsTestViewModel : RoutedViewModel<(int IntValue, string StringValue)>
{
    public INavigator Navigator { get => field ?? throw new InvalidOperationException("Navigator not set."); set; }

    public int IntValue => Parameter.IntValue;

    public string StringValue => Parameter.StringValue;

    public override ValueTask OnNavigatedToAsync(INavigator navigator, NavigationArgs args)
    {
        Navigator = navigator;
        return ValueTask.CompletedTask;
    }

    [RelayCommand]
    public async Task GoBackAsync() => await Navigator.GoBackAsync(false);
}
