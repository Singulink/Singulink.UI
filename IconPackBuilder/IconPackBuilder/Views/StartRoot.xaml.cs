using IconPackBuilder.ViewModels;

namespace IconPackBuilder.Views;

public sealed partial class StartRoot : UserControl
{
    public StartRootModel Model => (StartRootModel)DataContext;

    public StartRoot()
    {
        InitializeComponent();
    }
}
