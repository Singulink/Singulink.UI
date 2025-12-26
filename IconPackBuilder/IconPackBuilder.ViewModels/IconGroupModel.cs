using CommunityToolkit.Mvvm.ComponentModel;
using IconPackBuilder.Core;

namespace IconPackBuilder.ViewModels;

public sealed partial class IconGroupModel(EditorRootModel editor, IconGroupInfo info) : ObservableObject
{
    public IconGroupInfo Info => info;

    public IReadOnlyList<IconModel> Icons => field ??= [.. info.Icons.Select(i => new IconModel(this, i))];

    [ObservableProperty]
    public partial string ExportName { get; set; } = string.Empty;

    partial void OnExportNameChanged(string value) => editor.IsDirty = true;

    public string FinalExportName => string.IsNullOrWhiteSpace(ExportName) ? Info.Id : ExportName;

    public string SaveExportName => ExportName == Info.Id ? string.Empty : ExportName;

    [ObservableProperty]
    public partial IconInfo? ActiveIconInfo { get; private set; }

    [ObservableProperty]
    public partial bool HasSelectedIcons { get; private set; }

    public void SetActiveVariant(string variant)
    {
        if (variant.Length is 0)
            ActiveIconInfo = Icons[0].Info;
        else
            ActiveIconInfo = Icons.FirstOrDefault(i => i.Info.Variant == variant)?.Info;
    }

    internal void OnIconSelectionChanged()
    {
        HasSelectedIcons = Icons.Any(i => i.IsSelected);
        editor.IsDirty = true;
    }
}
