using Playground.ViewModels.ParamsTest;
using Singulink.UI.Navigation.WinUI;

namespace Playground.Views.ParamsTest;

public sealed partial class ParamsTestPage : UserControl, IRoutedView<ParamsTestViewModel>
{
    public ParamsTestViewModel Model { get; } = new();

    public ParamsTestPage()
    {
        InitializeComponent();
    }
}
