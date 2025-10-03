using CommunityToolkit.Mvvm.ComponentModel;
using IconPackBuilder.Core;

namespace IconPackBuilder.ViewModels;

public sealed partial class IconGroupModel(EditorRootModel editor, IconGroupInfo info) : ObservableObject
{
    public IconGroupInfo Info => info;

    public IReadOnlyList<IconModel> Icons => field ??= [.. info.Icons.Select(i => new IconModel(this, i))];

    [ObservableProperty]
    public partial string ExportName { get; set; } = info.Id;

    partial void OnExportNameChanged(string value) => editor.IsDirty = true;

    [ObservableProperty]
    public partial IconInfo? ActiveIconInfo { get; private set; }

    [ObservableProperty]
    public partial bool HasSelectedIcons { get; private set; }

    public void SetActiveVariant(string type)
    {
        ActiveIconInfo = Icons.FirstOrDefault(i => i.Info.Variant == type)?.Info;
    }

    internal void OnIconSelectionChanged()
    {
        HasSelectedIcons = Icons.Any(i => i.IsSelected);
        editor.IsDirty = true;
    }
}
