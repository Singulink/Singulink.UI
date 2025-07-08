using Playground.ViewModels.ParamsTest;
using Singulink.UI.Navigation;

namespace Playground.Views.ParamsTest;

public sealed partial class ParamsTestPage : Page, IRoutedView<ParamsTestViewModel>
{
    public ParamsTestViewModel Model { get; } = new();

    public ParamsTestPage()
    {
        InitializeComponent();
    }
}
