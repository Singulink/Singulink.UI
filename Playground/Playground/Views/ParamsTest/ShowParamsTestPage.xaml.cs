using Playground.ViewModels.ParamsTest;
using Singulink.UI.Navigation.WinUI;

namespace Playground.Views.ParamsTest;

public sealed partial class ShowParamsTestPage : UserControl, IRoutedView<ShowParamsTestViewModel>
{
    public ShowParamsTestViewModel Model { get; } = new();

    public ShowParamsTestPage()
    {
        InitializeComponent();
    }
}
