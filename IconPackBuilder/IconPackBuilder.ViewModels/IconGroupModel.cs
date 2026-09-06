using CommunityToolkit.Mvvm.ComponentModel;
using IconPackBuilder.Core;
using IconPackBuilder.ViewModels.Utilities;

namespace IconPackBuilder.ViewModels;

public sealed partial class IconGroupModel(EditorRootModel editor, IconGroupInfo info) : ObservableObject
{
    public IconGroupInfo Info => info;

    public IReadOnlyList<IconModel> Icons => field ??= [.. info.Icons.Select(i => new IconModel(this, i))];

    /// <summary>
    /// Gets the icon's keywords as a comma-separated list for display, or <see langword="null"/> if it has none.
    /// </summary>
    public string? KeywordsText { get; } = info.Keywords.Count > 0 ? string.Join(", ", info.Keywords) : null;

    private string KeywordsSearchText { get; } = string.Join(' ', info.Keywords);

    [ObservableProperty]
    public partial string ExportName { get; set; } = string.Empty;

    partial void OnExportNameChanged(string value) => editor.IsDirty = true;

    public string FinalExportName => string.IsNullOrWhiteSpace(ExportName) ? Info.Id : ExportName;

    public string SaveExportName => ExportName == Info.Id ? string.Empty : ExportName;

    [ObservableProperty]
    public partial IconInfo? ActiveIconInfo { get; private set; }

    [ObservableProperty]
    public partial bool HasSelectedIcons { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this is the group currently selected in the editor.
    /// </summary>
    [ObservableProperty]
    public partial bool IsSelected { get; internal set; }

    public void SetActiveVariant(string variant)
    {
        if (variant.Length is 0)
            ActiveIconInfo = Icons[0].Info;
        else
            ActiveIconInfo = Icons.FirstOrDefault(i => i.Info.Variant == variant)?.Info;
    }

    /// <summary>
    /// Ranks how well the group matches the filter: 2 for a name (or export name) match, 1 for a keyword-only match, 0 for no match. Every filter
    /// word must prefix-match a word of the name or a word of the keywords.
    /// </summary>
    internal int GetFilterRank(string[] filterParts)
    {
        string nameText = string.IsNullOrWhiteSpace(ExportName) ? Info.Name : $"{Info.Name} {ExportName}";

        if (nameText.MatchesFilter(filterParts))
            return 2;

        if (KeywordsSearchText.Length > 0 && $"{nameText} {KeywordsSearchText}".MatchesFilter(filterParts))
            return 1;

        return 0;
    }

    internal void OnIconSelectionChanged()
    {
        HasSelectedIcons = Icons.Any(i => i.IsSelected);
        editor.IsDirty = true;
    }
}
