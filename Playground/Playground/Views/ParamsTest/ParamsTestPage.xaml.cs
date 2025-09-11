using Playground.ViewModels.ParamsTest;

namespace Playground.Views.ParamsTest;

public sealed partial class ParamsTestPage : UserControl
{
    public ParamsTestViewModel Model => (ParamsTestViewModel)DataContext;

    public ParamsTestPage()
    {
        InitializeComponent();
    }
}
