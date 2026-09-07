using System.Text.Json.Serialization;

namespace IconPackBuilder.Data;

/// <summary>
/// Output formats an export can produce alongside the subset font.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ExportFormat>))]
public enum ExportFormat
{
    /// <summary>
    /// A C# class with a strongly typed member per icon for use with the Singulink.UI.Icons packages.
    /// </summary>
    CSharp,

    /// <summary>
    /// A CSS stylesheet with an <c>@font-face</c> rule for the subset font and a class per icon.
    /// </summary>
    Css,

    /// <summary>
    /// A JavaScript ES module (with TypeScript declarations) exposing the icon glyphs and CSS class names.
    /// </summary>
    JavaScript,
}
