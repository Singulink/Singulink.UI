namespace IconPackBuilder.Core;

public sealed class IconGroupInfo
{
    public string Id { get; }

    public string Name { get; }

    public IReadOnlyList<IconInfo> Icons { get; }

    public bool HasUniqueRtlGlyphs { get; }

    public IconGroupInfo(string id, string name, IEnumerable<IconInfo> icons)
    {
        Id = id;
        Name = name;
        Icons = [.. icons];

        if (Icons.Count is 0)
            throw new ArgumentException("Icon group must contain at least one icon.", nameof(icons));

        foreach (var icon in Icons)
        {
            icon.Group = this;

            if (icon.RtlCodePoint is not null)
                HasUniqueRtlGlyphs = true;
        }
    }

    public override string ToString() => Name;
}
