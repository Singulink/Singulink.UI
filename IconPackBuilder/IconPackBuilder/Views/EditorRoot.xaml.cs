using IconPackBuilder.ViewModels;

namespace IconPackBuilder.Views;

public sealed partial class EditorRoot : UserControl
{
    public EditorRootModel Model => (EditorRootModel)DataContext;

    public FontFamily IconFontFamily => field ??= new($"ms-appx:///{Model.IconsSource.FontFile.PathDisplay}#{Model.IconsSource.FontFamilyName}");

    public EditorRoot()
    {
        InitializeComponent();
    }
}
