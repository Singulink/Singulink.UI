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
        for (int i = 0; i < Model.ButtonLabels.Count; i++)
        {
            int index = i;
            var button = new Button { Content = Model.ButtonLabels[i] };
            button.Click += (s, e) => Model.OnButtonClick(index);

            if (i == Model.DefaultButtonIndex)
            {
                if (Resources.TryGetValue("MessageDialogAccentButtonStyle", out object accentStyle))
                    button.Style = (Style)accentStyle;
                else
                    button.Style = (Style)Resources["DefaultMessageDialogAccentButtonStyle"];

                button.Loaded += (s, e) => button.Focus(FocusState.Programmatic);
            }
            else
            {
                if (Resources.TryGetValue("MessageDialogNormalButtonStyle", out object normalStyle))
                    button.Style = (Style)normalStyle;
                else
                    button.Style = (Style)Resources["DefaultMessageDialogNormalButtonStyle"];
            }

            ButtonsPanel.Children.Add(button);
        }
    }
}
