using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Singulink.UI.Navigation.Generators;

[Generator(LanguageNames.CSharp)]
public sealed class RouteParamsModelGenerator : IIncrementalGenerator
{
    private const string AttributeFullName = "Singulink.UI.Navigation.RouteParamsModelAttribute";
    private const string RouteValuesCollectionFullName = "global::Singulink.UI.Navigation.RouteValuesCollection";
    private const string IRouteParamsModelFullName = "global::Singulink.UI.Navigation.IRouteParamsModel";
    private const string MaybeNullWhenFullName = "global::System.Diagnostics.CodeAnalysis.MaybeNullWhen";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var models = context.SyntaxProvider.ForAttributeWithMetadataName(
            AttributeFullName,
            predicate: static (node, _) => node is TypeDeclarationSyntax,
            transform: static (ctx, ct) => Analyze(ctx, ct));

        context.RegisterSourceOutput(models, static (spc, model) =>
        {
            if (model is null)
                return;

            foreach (var diag in model.Diagnostics)
                spc.ReportDiagnostic(diag);

            if (model.HasErrors)
                return;

            string source = Emit(model);
            spc.AddSource(model.HintName, source);
        });
    }

    private static ModelInfo? Analyze(GeneratorAttributeSyntaxContext ctx, System.Threading.CancellationToken ct)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol type)
            return null;

        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        bool hasErrors = false;

        // Check partial on all declarations
        bool isPartial = ctx.TargetNode is TypeDeclarationSyntax tds && tds.Modifiers.Any(m => m.ValueText == "partial");
        if (!isPartial)
        {
            diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.NotPartial, ctx.TargetNode.GetLocation(), type.Name));
            hasErrors = true;
        }

        if (!type.IsRecord)
        {
            diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.NotRecord, ctx.TargetNode.GetLocation(), type.Name));
            hasErrors = true;
        }

        var properties = ImmutableArray.CreateBuilder<PropertyInfo>();
        string? restPropertyName = null;

        // Collect primary-constructor parameter names (if any) so their matching properties are treated as implicitly required.
        var primaryCtorParams = ImmutableArray.CreateBuilder<string>();

        if (ctx.TargetNode is RecordDeclarationSyntax recordDecl && recordDecl.ParameterList is { } paramList)
        {
            foreach (var p in paramList.Parameters)
                primaryCtorParams.Add(p.Identifier.ValueText);
        }

        foreach (var member in type.GetMembers())
        {
            ct.ThrowIfCancellationRequested();

            // Reject any user-declared instance fields. Compiler-synthesized backing
            // fields for auto-properties (and the record primary-ctor params) are marked
            // IsImplicitlyDeclared and so are skipped here. Static fields (including
            // consts) are not part of the model's instance state and are ignored.
            if (member is IFieldSymbol field && !field.IsImplicitlyDeclared && !field.IsStatic)
            {
                var fieldLocation = field.Locations.FirstOrDefault() ?? ctx.TargetNode.GetLocation();
                diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.FieldDeclarationNotAllowed, fieldLocation, field.Name));
                hasErrors = true;
                continue;
            }

            if (member is not IPropertySymbol prop || prop.IsStatic || prop.IsIndexer)
                continue;

            // Skip compiler-synthesized EqualityContract on records
            if (prop.Name == "EqualityContract")
                continue;

            // Skip read-only / computed properties silently
            if (prop.SetMethod is null)
                continue;

            var location = prop.Locations.FirstOrDefault() ?? ctx.TargetNode.GetLocation();

            // Must be init-only (not a regular set)
            if (!prop.SetMethod.IsInitOnly)
            {
                diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.PropertyMustBeInitOnly, location, prop.Name));
                hasErrors = true;
                continue;
            }

            // Must be an auto-property: no expression body on the property and no body
            // (block or expression) on either accessor. Properties that come from the
            // record primary constructor have no syntax reference and are inherently auto-properties.
            if (!IsAutoProperty(prop, ct))
            {
                diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.PropertyMustBeAutoProperty, location, prop.Name));
                hasErrors = true;
                continue;
            }

            var propType = prop.Type;
            bool isRouteQuery = IsRouteQuery(propType, out bool isNullableRouteQuery);

            if (isRouteQuery)
            {
                // RouteQuery property
                if (isNullableRouteQuery || propType.NullableAnnotation == NullableAnnotation.Annotated)
                {
                    diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.RouteQueryPropertyMustNotBeNullable, location, prop.Name));
                    hasErrors = true;
                    continue;
                }

                if (restPropertyName is not null)
                {
                    diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.MultipleRouteQueryProperties, location, prop.Name));
                    hasErrors = true;
                    continue;
                }

                restPropertyName = prop.Name;
                properties.Add(new PropertyInfo(prop.Name, PropertyKind.Rest, string.Empty));
                continue;
            }

            // Determine nullability and unwrapped parsable type
            bool isNullable;
            ITypeSymbol underlyingType;

            if (propType.IsValueType)
            {
                if (propType is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullableT)
                {
                    isNullable = true;
                    underlyingType = nullableT.TypeArguments[0];
                }
                else
                {
                    isNullable = false;
                    underlyingType = propType;
                }
            }
            else
            {
                isNullable = propType.NullableAnnotation == NullableAnnotation.Annotated;
                underlyingType = propType.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
            }

            // Validate underlying type implements IParsable<TSelf>
            if (!ImplementsIParsable(underlyingType))
            {
                diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.UnsupportedPropertyType, location, prop.Name, propType.ToDisplayString()));
                hasErrors = true;
                continue;
            }

            if (!isNullable && !prop.IsRequired && !primaryCtorParams.Contains(prop.Name))
            {
                diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.RequiredPropertyMissingRequiredModifier, location, prop.Name));
                hasErrors = true;
                continue;
            }

            var kind = isNullable ? PropertyKind.Optional : PropertyKind.Required;
            properties.Add(new PropertyInfo(prop.Name, kind, underlyingType.ToDisplayString(FullyQualifiedNonNullable)));
        }

        var containing = BuildContainingInfo(type);

        return new ModelInfo(
            TypeName: type.Name,
            FullyQualifiedName: type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)),
            Containing: containing,
            Properties: properties.ToEquatableArray(),
            PrimaryCtorParamNames: primaryCtorParams.ToEquatableArray(),
            Diagnostics: diagnostics.ToEquatableArray(),
            HasErrors: hasErrors,
            HintName: BuildHintName(type));
    }

    private static readonly SymbolDisplayFormat FullyQualifiedNonNullable = SymbolDisplayFormat.FullyQualifiedFormat
        .WithMiscellaneousOptions(SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions & ~SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    private static bool IsRouteQuery(ITypeSymbol type, out bool isNullableWrapped)
    {
        // Unwrap Nullable<RouteQuery> (applies when RouteQuery is a struct).
        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullableT)
        {
            isNullableWrapped = true;
            type = nullableT.TypeArguments[0];
        }
        else
        {
            isNullableWrapped = false;
        }

        return type is INamedTypeSymbol named
            && named.Name == "RouteQuery"
            && named.ContainingNamespace?.ToDisplayString() == "Singulink.UI.Navigation";
    }

    private static bool ImplementsIParsable(ITypeSymbol type)
    {
        foreach (var iface in type.AllInterfaces)
        {
            if (iface.TypeArguments.Length == 1
                && iface.Name == "IParsable"
                && iface.ContainingNamespace?.ToDisplayString() == "System"
                && SymbolEqualityComparer.Default.Equals(iface.TypeArguments[0], type.WithNullableAnnotation(NullableAnnotation.NotAnnotated)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAutoProperty(IPropertySymbol prop, System.Threading.CancellationToken ct)
    {
        // Properties from a record primary constructor have no syntax reference and are auto-properties by definition.
        if (prop.DeclaringSyntaxReferences.Length == 0)
            return true;

        foreach (var syntaxRef in prop.DeclaringSyntaxReferences)
        {
            var node = syntaxRef.GetSyntax(ct);

            // A record primary-ctor parameter shows up here as ParameterSyntax — also an auto-property.
            if (node is not PropertyDeclarationSyntax propDecl)
                continue;

            // Expression-bodied property: `public int X => 42;` — not an auto-property.
            if (propDecl.ExpressionBody is not null)
                return false;

            if (propDecl.AccessorList is { } accessorList)
            {
                foreach (var accessor in accessorList.Accessors)
                {
                    // An auto-property accessor has neither a block body nor an expression body.
                    if (accessor.Body is not null || accessor.ExpressionBody is not null)
                        return false;
                }
            }
        }

        return true;
    }

    private static ContainingInfo BuildContainingInfo(INamedTypeSymbol type)
    {
        string? ns = type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToDisplayString();

        var chain = ImmutableArray.CreateBuilder<ContainingTypeInfo>();
        var current = type.ContainingType;
        while (current is not null)
        {
            chain.Insert(0, new ContainingTypeInfo(
                Name: current.Name,
                Kind: GetKind(current),
                TypeParameters: current.TypeParameters.IsDefaultOrEmpty ? null : string.Join(", ", current.TypeParameters.Select(tp => tp.Name))));
            current = current.ContainingType;
        }

        return new ContainingInfo(ns, chain.ToEquatableArray());
    }

    private static string GetKind(INamedTypeSymbol t)
    {
        if (t.IsRecord)
            return t.IsValueType ? "record struct" : "record";

        if (t.IsValueType)
            return "struct";

        return "class";
    }

    private static string BuildHintName(INamedTypeSymbol type)
    {
        var sb = new StringBuilder();
        var containing = type.ContainingType;
        var stack = new Stack<string>();
        stack.Push(type.Name);
        while (containing is not null)
        {
            stack.Push(containing.Name);
            containing = containing.ContainingType;
        }

        if (!type.ContainingNamespace.IsGlobalNamespace)
            sb.Append(type.ContainingNamespace.ToDisplayString()).Append('.');

        while (stack.Count > 0)
        {
            sb.Append(stack.Pop());
            if (stack.Count > 0)
                sb.Append('.');
        }

        sb.Append(".RouteParamsModel.g.cs");
        return sb.ToString();
    }

    private static string Emit(ModelInfo model)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        int indent = 0;

        if (model.Containing.Namespace is { } ns)
        {
            sb.Append("namespace ").Append(ns).AppendLine(";");
            sb.AppendLine();
        }

        // Open containing type declarations
        foreach (var ct in model.Containing.TypeChain)
        {
            AppendIndent(sb, indent);
            sb.Append("partial ").Append(ct.Kind).Append(' ').Append(ct.Name);
            if (ct.TypeParameters is { } tp)
                sb.Append('<').Append(tp).Append('>');
            sb.AppendLine();
            AppendIndent(sb, indent);
            sb.AppendLine("{");
            indent++;
        }

        // The target record itself
        AppendIndent(sb, indent);
        sb.Append("partial record ").Append(model.TypeName).Append(" : ").Append(IRouteParamsModelFullName).Append('<').Append(model.TypeName).AppendLine(">");
        AppendIndent(sb, indent);
        sb.AppendLine("{");
        indent++;

        EmitMembers(sb, indent, model);

        indent--;
        AppendIndent(sb, indent);
        sb.AppendLine("}");

        // Close containing types
        for (int i = 0; i < model.Containing.TypeChain.Length; i++)
        {
            indent--;
            AppendIndent(sb, indent);
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    private static void EmitMembers(StringBuilder sb, int indent, ModelInfo model)
    {
        var required = model.Properties.Where(p => p.Kind == PropertyKind.Required).ToList();
        var optional = model.Properties.Where(p => p.Kind == PropertyKind.Optional).ToList();
        var rest = model.Properties.FirstOrDefault(p => p.Kind == PropertyKind.Rest);
        bool hasRest = rest is not null;

        // RequiredParameterNames
        AppendIndent(sb, indent);
        sb.Append("static global::System.Collections.Generic.IReadOnlyList<string> ")
          .Append(IRouteParamsModelFullName).Append('<').Append(model.TypeName).AppendLine(">.RequiredParameterNames =>");
        AppendIndent(sb, indent + 1);
        if (required.Count == 0)
        {
            sb.AppendLine("global::System.Array.Empty<string>();");
        }
        else
        {
            sb.Append("new string[] { ");
            for (int i = 0; i < required.Count; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                sb.Append('"').Append(required[i].Name).Append('"');
            }

            sb.AppendLine(" };");
        }

        sb.AppendLine();

        // ProvidesRemainingQueryAccess
        AppendIndent(sb, indent);
        sb.Append("static bool ").Append(IRouteParamsModelFullName).Append('<').Append(model.TypeName).Append(">.ProvidesRemainingQueryAccess => ")
          .Append(hasRest ? "true" : "false").AppendLine(";");
        sb.AppendLine();

        // TryCreate
        AppendIndent(sb, indent);
        sb.Append("static bool ").Append(IRouteParamsModelFullName).Append('<').Append(model.TypeName).Append(">.TryCreate(")
                    .Append(RouteValuesCollectionFullName).Append(" values, [").Append(MaybeNullWhenFullName).Append("(false)] out ").Append(model.TypeName).AppendLine(" result)");
        AppendIndent(sb, indent);
        sb.AppendLine("{");
        int bodyIndent = indent + 1;

        AppendIndent(sb, bodyIndent);
        sb.AppendLine("result = default;");
        sb.AppendLine();

        // Required
        foreach (var p in required)
        {
            AppendIndent(sb, bodyIndent);
            sb.Append(p.TypeFullyQualified).Append(" __p_").Append(p.Name).AppendLine(";");
            AppendIndent(sb, bodyIndent);
            sb.Append("if (values.TryConsume<").Append(p.TypeFullyQualified).Append(">(\"").Append(p.Name).Append("\", out var __t_").Append(p.Name).AppendLine("))");
            AppendIndent(sb, bodyIndent);
            sb.AppendLine("{");
            AppendIndent(sb, bodyIndent + 1);
            sb.Append("__p_").Append(p.Name).Append(" = __t_").Append(p.Name).AppendLine(";");
            AppendIndent(sb, bodyIndent);
            sb.AppendLine("}");
            AppendIndent(sb, bodyIndent);
            sb.AppendLine("else");
            AppendIndent(sb, bodyIndent);
            sb.AppendLine("{");
            AppendIndent(sb, bodyIndent + 1);
            sb.AppendLine("return false;");
            AppendIndent(sb, bodyIndent);
            sb.AppendLine("}");
            sb.AppendLine();
        }

        // Optional
        foreach (var p in optional)
        {
            AppendIndent(sb, bodyIndent);
            sb.Append(p.TypeFullyQualified).Append("? __p_").Append(p.Name).AppendLine(" = null;");
            AppendIndent(sb, bodyIndent);
            sb.Append("if (values.TryConsume<").Append(p.TypeFullyQualified).Append(">(\"").Append(p.Name).Append("\", out var __t_").Append(p.Name).AppendLine("))");
            AppendIndent(sb, bodyIndent + 1);
            sb.Append("__p_").Append(p.Name).Append(" = __t_").Append(p.Name).AppendLine(";");
            sb.AppendLine();
        }

        // Rest
        if (hasRest)
        {
            AppendIndent(sb, bodyIndent);
            sb.Append("var __p_").Append(rest!.Name).AppendLine(" = values.ConsumeQuery();");
            sb.AppendLine();
        }

        // Construct
        AppendIndent(sb, bodyIndent);
        sb.Append("result = new ").Append(model.TypeName);

        var primaryCtorPropNames = model.PrimaryCtorParamNames;
        var objectInitProps = model.Properties
            .Where(p => !primaryCtorPropNames.Contains(p.Name))
            .ToList();

        if (primaryCtorPropNames.Length > 0)
        {
            sb.Append('(');
            for (int i = 0; i < primaryCtorPropNames.Length; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                sb.Append("__p_").Append(primaryCtorPropNames[i]);
            }

            sb.Append(')');
        }

        if (objectInitProps.Count > 0)
        {
            sb.Append(" {");
            bool first = true;
            foreach (var p in objectInitProps)
            {
                if (!first)
                    sb.Append(',');
                first = false;
                sb.AppendLine();
                AppendIndent(sb, bodyIndent + 1);
                sb.Append(p.Name).Append(" = __p_").Append(p.Name);
            }

            sb.AppendLine();
            AppendIndent(sb, bodyIndent);
            sb.Append('}');
        }
        else if (primaryCtorPropNames.Length == 0)
        {
            // No primary ctor params and no properties: emit empty object initializer for consistency.
            sb.Append("()");
        }

        sb.AppendLine(";");

        AppendIndent(sb, bodyIndent);
        sb.AppendLine("return true;");
        AppendIndent(sb, indent);
        sb.AppendLine("}");
        sb.AppendLine();

        // ToRouteValues
        AppendIndent(sb, indent);
        sb.Append(RouteValuesCollectionFullName).Append(' ').Append(IRouteParamsModelFullName).AppendLine(".ToRouteValues()");
        AppendIndent(sb, indent);
        sb.AppendLine("{");

        AppendIndent(sb, bodyIndent);
        sb.Append("var __c = new ").Append(RouteValuesCollectionFullName).Append('(').Append(required.Count).AppendLine(")");
        AppendIndent(sb, bodyIndent);
        sb.AppendLine("{");
        foreach (var p in required)
        {
            AppendIndent(sb, bodyIndent + 1);
            sb.Append("{ \"").Append(p.Name).Append("\", ").Append(p.Name).AppendLine(" },");
        }

        AppendIndent(sb, bodyIndent);
        sb.AppendLine("};");

        if (optional.Count > 0)
            sb.AppendLine();

        foreach (var p in optional)
        {
            AppendIndent(sb, bodyIndent);
            sb.Append("if (").Append(p.Name).Append(" is { } __v_").Append(p.Name).AppendLine(")");
            AppendIndent(sb, bodyIndent + 1);
            sb.Append("__c.Add(\"").Append(p.Name).Append("\", __v_").Append(p.Name).AppendLine(");");
            AppendIndent(sb, bodyIndent);
            sb.AppendLine("else");
            AppendIndent(sb, bodyIndent + 1);
            sb.Append("__c.Reserve(\"").Append(p.Name).AppendLine("\");");
        }

        if (hasRest)
        {
            sb.AppendLine();
            AppendIndent(sb, bodyIndent);
            sb.Append("__c.AddQuery(").Append(rest!.Name).AppendLine(");");
        }

        sb.AppendLine();
        AppendIndent(sb, bodyIndent);
        sb.AppendLine("return __c;");
        AppendIndent(sb, indent);
        sb.AppendLine("}");
    }

    private static void AppendIndent(StringBuilder sb, int count)
    {
        for (int i = 0; i < count; i++)
            sb.Append("    ");
    }
}
