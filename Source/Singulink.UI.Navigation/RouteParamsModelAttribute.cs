namespace Singulink.UI.Navigation;

/// <summary>
/// Marks a <see langword="partial"/> <see langword="record"/> for source generation of <see cref="IRouteParamsModel{TSelf}"/> implementations,
/// enabling bidirectional conversion between the record and a <see cref="RouteValuesCollection"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class RouteParamsModelAttribute : Attribute;
