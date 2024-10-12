namespace Singulink.UI.Navigation;

internal sealed partial class MessageDialog : ContentDialog
{
    public MessageDialogViewModel Model => (MessageDialogViewModel)DataContext;

    public MessageDialog()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var panel = ButtonItemsControl.ItemsPanelRoot;

        if (panel is null)
            return;

        for (int i = 0; i < panel.Children.Count; i++)
        {
            var button = (Button)panel.Children[i];

            if (i == Model.DefaultButtonIndex)
            {
                if (Resources.TryGetValue("MessageDialogAccentButtonStyle", out object accentStyle))
                    button.Style = (Style)accentStyle;
                else
                    button.Style = (Style)Resources["AccentButtonStyle"];

                button.Focus(FocusState.Programmatic);
            }
            else
            {
                if (Resources.TryGetValue("MessageDialogNormalButtonStyle", out object normalStyle))
                    button.Style = (Style)normalStyle;
                else
                    button.Style = (Style)Resources["DefaultButtonStyle"];
            }
        }
    }

    private void OnButtonClick(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        var panel = (Panel)button.Parent;
        int index = panel.Children.IndexOf(button);

        Model.OnButtonClick(index);
    }
}
