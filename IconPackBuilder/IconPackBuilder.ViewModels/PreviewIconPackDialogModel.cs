using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IconPackBuilder.Core;
using Singulink.UI.Navigation;

namespace IconPackBuilder.ViewModels;

public partial class PreviewIconPackDialogModel(IEnumerable<PreviewIconItem> icons, IconsSource iconsSource) : ObservableObject, IDismissibleDialogViewModel
{
    public IReadOnlyList<PreviewIconItem> Icons { get; } = [.. icons];

    public IconsSource IconsSource { get; } = iconsSource;

    [RelayCommand]
    public void Close() => this.Navigator.Close();

    public async Task OnDismissRequestedAsync() => Close();
}
