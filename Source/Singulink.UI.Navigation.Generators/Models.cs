using Microsoft.CodeAnalysis;

namespace Singulink.UI.Navigation.Generators;

internal enum PropertyKind
{
    Required,
    Optional,
    Rest,
}

internal sealed record PropertyInfo(string Name, PropertyKind Kind, string TypeFullyQualified);

internal sealed record ContainingTypeInfo(string Name, string Kind, string? TypeParameters);

internal sealed record ContainingInfo(string? Namespace, EquatableArray<ContainingTypeInfo> TypeChain);

internal sealed record ModelInfo(
    string TypeName,
    string FullyQualifiedName,
    ContainingInfo Containing,
    EquatableArray<PropertyInfo> Properties,
    EquatableArray<string> PrimaryCtorParamNames,
    EquatableArray<Diagnostic> Diagnostics,
    bool HasErrors,
    string HintName);
