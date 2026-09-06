namespace IconPackBuilder.Core;

public sealed class IconGroupInfo
{
    public string Id { get; }

    public string Name { get; }

    /// <summary>
    /// Gets a description of the icon's intended usage, if one is available.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets keywords (metaphors) associated with the icon that can be used to find it by concept rather than by name.
    /// </summary>
    public IReadOnlyList<string> Keywords { get; }

    public IReadOnlyList<IconInfo> Icons { get; }

    public bool HasUniqueRtlGlyphs { get; }

    public IconGroupInfo(string id, string name, IEnumerable<IconInfo> icons, string? description = null, IEnumerable<string>? keywords = null)
    {
        Id = id;
        Name = name;
        Description = string.IsNullOrWhiteSpace(description) ? null : description;
        Keywords = keywords is null ? [] : [.. keywords];
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
