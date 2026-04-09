using Microsoft.CodeAnalysis;

namespace Singulink.UI.Navigation.Generators;

internal static class DiagnosticDescriptors
{
    private const string Category = "Singulink.UI.Navigation";

    public static readonly DiagnosticDescriptor NotPartial = new(
        id: "SUIN001",
        title: "Route params model must be partial",
        messageFormat: "Type '{0}' must be declared partial to be used as a [RouteParamsModel]",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotRecord = new(
        id: "SUIN002",
        title: "Route params model must be a record",
        messageFormat: "Type '{0}' must be declared as a record to be used as a [RouteParamsModel]",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor PropertyMustBeInitOnly = new(
        id: "SUIN003",
        title: "Route params model property must be init-only",
        messageFormat: "Property '{0}' must have an 'init' accessor (and no 'set' accessor) on a [RouteParamsModel]",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor RequiredPropertyMissingRequiredModifier = new(
        id: "SUIN004",
        title: "Non-nullable route params property must be required",
        messageFormat: "Non-nullable property '{0}' must be declared 'required' on a [RouteParamsModel] (or use a nullable type for optional parameters)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor RouteQueryPropertyMustNotBeNullable = new(
        id: "SUIN005",
        title: "RouteQuery property must not be nullable",
        messageFormat: "Property '{0}' must be declared as RouteQuery (not RouteQuery?); the default value is an empty query",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MultipleRouteQueryProperties = new(
        id: "SUIN006",
        title: "Only one RouteQuery property is allowed",
        messageFormat: "A [RouteParamsModel] can declare at most one RouteQuery property; '{0}' is the second one found",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedPropertyType = new(
        id: "SUIN007",
        title: "Unsupported route params property type",
        messageFormat: "Property '{0}' has an unsupported type '{1}'. Route params model properties must be a type that implements IParsable<TSelf>, a nullable of such a type, or RouteQuery",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor FieldDeclarationNotAllowed = new(
        id: "SUIN008",
        title: "Route params model must not declare fields",
        messageFormat: "Field '{0}' is not allowed on a [RouteParamsModel]; declare data as init-only auto-properties instead",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor PropertyMustBeAutoProperty = new(
        id: "SUIN009",
        title: "Route params model property must be an auto-property",
        messageFormat: "Property '{0}' must be a simple auto-property on a [RouteParamsModel]; custom get/init bodies and expression-bodied properties are not allowed",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
