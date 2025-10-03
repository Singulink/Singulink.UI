using Singulink.IO;

namespace IconPackBuilder.Core;

public abstract class IconsSource
{
    public abstract string Id { get; }

    public abstract string Name { get; }

    public abstract Version Version { get; }

    /// <summary>
    /// Gets the font file path relative to the app directory.
    /// </summary>
    public abstract IRelativeFilePath FontFile { get; }

    public abstract string FontFamilyName { get; }

    public abstract IReadOnlyList<string> Variants { get; }

    public abstract IEnumerable<IconGroupInfo> LoadIconGroups();
}
