namespace Singulink.UI.Navigation;

internal sealed partial class MessageDialog : ContentDialog
{
    public MessageDialogViewModel Model => (MessageDialogViewModel)DataContext;

    public MessageDialog()
    {
        InitializeComponent();
    }

    private void OnButtonClick(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        var panel = (Panel)button.Parent;
        int index = panel.Children.IndexOf(button);

        Model.OnButtonClick(index);
    }
}
