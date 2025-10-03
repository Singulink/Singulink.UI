using CommunityToolkit.Mvvm.ComponentModel;
using IconPackBuilder.Core;

namespace IconPackBuilder.ViewModels;

public sealed partial class IconModel(IconGroupModel group, IconInfo info) : ObservableObject
{
    public IconInfo Info => info;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    partial void OnIsSelectedChanged(bool value) => group.OnIconSelectionChanged();
}
