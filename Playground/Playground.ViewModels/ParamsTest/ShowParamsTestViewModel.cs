using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Singulink.UI.Navigation;

namespace Playground.ViewModels.ParamsTest;

public partial class ShowParamsTestViewModel : ObservableObject, IRoutedViewModel<ShowParamsTestViewModel.Params>
{
    [RouteParamsModel]
    public partial record Params
    {
        public required int IntValue { get; init; }

        public string? StringValue { get; init; }

        public RouteQuery Rest { get; init; }
    }

    public int IntValue => this.Parameter.IntValue;

    public string? StringValue => this.Parameter.StringValue;

    [RelayCommand]
    public async Task GoBackAsync()
    {
        await this.Navigator.NavigatePartialAsync(Routes.Main.ParamsTestChild);
    }
}
