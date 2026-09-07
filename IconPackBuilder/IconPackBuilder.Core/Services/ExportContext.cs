using System.Text;
using Singulink.IO;

namespace IconPackBuilder.Core.Services;

/// <summary>
/// Everything an <see cref="IExporter"/> needs to write its output, plus the naming rules shared by all formats so that the C# member, the CSS
/// class and the JavaScript property of an icon always correspond.
/// </summary>
/// <param name="ProjectName">The project name in <c>Namespace.Class</c> form.</param>
/// <param name="ExportDir">The folder the export is written to. The subset font is written to the same folder.</param>
/// <param name="Icons">The selected icons with their final export names.</param>
/// <param name="DefaultVariantName">The variant exported under the plain icon name; other variants get the variant name appended.</param>
/// <param name="FontFileName">The file name of the subset font in <paramref name="ExportDir"/>.</param>
public sealed record ExportContext(
    string ProjectName,
    IAbsoluteDirectoryPath ExportDir,
    IReadOnlyList<ExportIconInfo> Icons,
    string DefaultVariantName,
    string FontFileName)
{
    /// <summary>
    /// Gets the namespace part of the project name, or the whole name if it has no namespace.
    /// </summary>
    public string NamespaceName { get; } = ParseProjectName(ProjectName).Namespace;

    /// <summary>
    /// Gets the class part of the project name. It names the generated files.
    /// </summary>
    public string ClassName { get; } = ParseProjectName(ProjectName).ClassName;

    /// <summary>
    /// Gets the member name of an icon: the export name, with the variant appended unless it is the default variant.
    /// </summary>
    public string GetMemberName(ExportIconInfo icon)
        => icon.Icon.Variant == DefaultVariantName ? icon.ExportName : icon.ExportName + icon.Icon.Variant;

    /// <summary>
    /// Gets the icons paired with their member names, ordered by member name. Throws if two icons resolve to the same member name.
    /// </summary>
    public IReadOnlyList<ExportMember> GetMembers()
    {
        var members = Icons
            .Select(i => new ExportMember(GetMemberName(i), i))
            .OrderBy(m => m.Name, StringComparer.Ordinal)
            .ToList();

        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (var member in members)
        {
            if (!names.Add(member.Name))
                throw new InvalidOperationException($"Duplicate icon member name detected: {member.Name}");
        }

        return members;
    }

    /// <summary>
    /// Converts a PascalCase member name to kebab-case for CSS class names, e.g. <c>SaveFilled</c> to <c>save-filled</c> and <c>QRCode</c> to
    /// <c>qr-code</c>.
    /// </summary>
    public static string ToKebabCase(string name)
    {
        var sb = new StringBuilder(name.Length + 4);

        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];

            if (char.IsUpper(c) && i > 0)
            {
                char prev = name[i - 1];
                bool startsWord = char.IsLower(prev) || char.IsDigit(prev) || (char.IsUpper(prev) && i + 1 < name.Length && char.IsLower(name[i + 1]));

                if (startsWord)
                    sb.Append('-');
            }

            sb.Append(c == '_' ? '-' : char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }

    private static (string Namespace, string ClassName) ParseProjectName(string projectName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectName);

        int lastDotIndex = projectName.LastIndexOf('.');

        if (lastDotIndex < 0)
            return (projectName, projectName);

        return (projectName[..lastDotIndex], projectName[(lastDotIndex + 1)..]);
    }
}

/// <summary>
/// An icon paired with the member name it is exported under.
/// </summary>
public sealed record ExportMember(string Name, ExportIconInfo Icon);
