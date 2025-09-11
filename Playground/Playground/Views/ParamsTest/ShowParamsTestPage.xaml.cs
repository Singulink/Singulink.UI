using Playground.ViewModels.ParamsTest;

namespace Playground.Views.ParamsTest;

public sealed partial class ShowParamsTestPage : UserControl
{
    public ShowParamsTestViewModel Model => (ShowParamsTestViewModel)DataContext;

    public ShowParamsTestPage()
    {
        InitializeComponent();
    }
}
