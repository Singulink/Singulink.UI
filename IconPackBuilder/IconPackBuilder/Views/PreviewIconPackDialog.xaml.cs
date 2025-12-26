using IconPackBuilder.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace IconPackBuilder.Views;

public sealed partial class PreviewIconPackDialog : ContentDialog
{
    public PreviewIconPackDialogModel Model => (PreviewIconPackDialogModel)DataContext;

    public FontFamily IconFontFamily => field ??= new($"ms-appx:///{Model.IconsSource.FontFile.PathDisplay}#{Model.IconsSource.FontFamilyName}");

    public PreviewIconPackDialog()
    {
        InitializeComponent();
    }
}
