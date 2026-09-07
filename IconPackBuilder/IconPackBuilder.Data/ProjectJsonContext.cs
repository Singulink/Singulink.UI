using System.Text.Json.Serialization;

namespace IconPackBuilder.Data;

/// <summary>
/// Source-generated JSON serialization for project files, so they can be read and written in trimmed (WebAssembly) builds.
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Project))]
public sealed partial class ProjectJsonContext : JsonSerializerContext;
