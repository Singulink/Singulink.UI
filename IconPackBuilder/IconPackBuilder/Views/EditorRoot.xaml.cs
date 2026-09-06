using IconPackBuilder.ViewModels;

namespace IconPackBuilder.Views;

public sealed partial class EditorRoot : UserControl
{
    public EditorRootModel Model => (EditorRootModel)DataContext;

    public FontFamily IconFontFamily => field ??= new($"ms-appx:///{Model.IconsSource.FontFile.PathDisplay}#{Model.IconsSource.FontFamilyName}");

    public EditorRoot()
    {
        InitializeComponent();

        DataContextChanged += (s, e) => {
            if (e.NewValue is EditorRootModel model)
                model.PropertyChanged += OnModelPropertyChanged;
        };
    }

    private void OnModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Start from the top whenever the filter produces a new list.
        if (e.PropertyName is nameof(EditorRootModel.FilteredIconGroups))
            IconGroupsScrollViewer.ChangeView(null, 0, null, disableAnimation: true);
    }

    private void OnIconGroupClick(object sender, RoutedEventArgs e) => Model.SelectedIconGroup = (IconGroupModel)((Button)sender).DataContext;
}
