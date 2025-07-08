using Playground.ViewModels.ParamsTest;
using Singulink.UI.Navigation;

namespace Playground.Views.ParamsTest;

public sealed partial class ShowParamsTestPage : Page, IRoutedView<ShowParamsTestViewModel>
{
    public ShowParamsTestViewModel Model { get; } = new();

    public ShowParamsTestPage()
    {
        InitializeComponent();
    }
}
