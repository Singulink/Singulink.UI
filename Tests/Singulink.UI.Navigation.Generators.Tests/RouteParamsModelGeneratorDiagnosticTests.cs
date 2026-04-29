using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using PrefixClassName.MsTest;
using Shouldly;
using Singulink.UI.Navigation;
using Singulink.UI.Navigation.Generators;

namespace Singulink.UI.Navigation.Generators.Tests;

/// <summary>
/// Exercises the generator against in-memory source strings so emission and diagnostic behavior can be verified in
/// isolation. Note that negative compilations (e.g. missing <c>partial</c>) also trigger Roslyn errors unrelated to our
/// diagnostics — we only assert on the generator's own <c>SUIN*</c> diagnostics.
/// </summary>
[PrefixTestClass]
public partial class RouteParamsModelGeneratorDiagnosticTests
{
    private const string Boilerplate =
        """
        using Singulink.UI.Navigation;
        namespace TestNs;
        """;

    private static readonly MetadataReference[] _references =
    [
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(IRouteParamsModel).Assembly.Location),
        MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
        MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
        MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location),
        MetadataReference.CreateFromFile(Assembly.Load("System.Linq").Location),
    ];

    private static ImmutableArray<Diagnostic> RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            assemblyName: "TestCompilation",
            syntaxTrees: [syntaxTree],
            references: _references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var generator = new RouteParamsModelGenerator().AsSourceGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        return driver.GetRunResult().Diagnostics;
    }

    [TestMethod]
    public void ValidModel_ProducesNoDiagnostics()
    {
        var diags = RunGenerator(Boilerplate + """

            [RouteParamsModel]
            public partial record Good
            {
                public required int Id { get; init; }
                public string? Name { get; init; }
            }
            """);

        diags.ShouldBeEmpty();
    }

    [TestMethod]
    public void SUIN001_NotPartial()
    {
        var diags = RunGenerator(Boilerplate + """

            [RouteParamsModel]
            public record NotPartial
            {
                public required int Id { get; init; }
            }
            """);

        diags.ShouldContain(d => d.Id == "SUIN001");
    }

    [TestMethod]
    public void SUIN002_NotRecord()
    {
        var diags = RunGenerator(Boilerplate + """

            [RouteParamsModel]
            public partial class NotRecord
            {
                public required int Id { get; init; }
            }
            """);

        diags.ShouldContain(d => d.Id == "SUIN002");
    }

    [TestMethod]
    public void SUIN003_PropertyNotInitOnly()
    {
        var diags = RunGenerator(Boilerplate + """

            [RouteParamsModel]
            public partial record Model
            {
                public required int Id { get; set; }
            }
            """);

        diags.ShouldContain(d => d.Id == "SUIN003");
    }

    [TestMethod]
    public void SUIN004_NonNullableWithoutRequired()
    {
        var diags = RunGenerator(Boilerplate + """

            [RouteParamsModel]
            public partial record Model
            {
                public int Id { get; init; }
            }
            """);

        diags.ShouldContain(d => d.Id == "SUIN004");
    }

    [TestMethod]
    public void SUIN004_SkippedForPrimaryCtorParams()
    {
        var diags = RunGenerator(Boilerplate + """

            [RouteParamsModel]
            public partial record Model(int Id, string? Name);
            """);

        diags.ShouldNotContain(d => d.Id == "SUIN004");
        diags.Where(d => d.Id.StartsWith("SUIN", System.StringComparison.Ordinal)).ShouldBeEmpty();
    }

    [TestMethod]
    public void SUIN005_NullableRouteQuery()
    {
        var diags = RunGenerator(Boilerplate + """

            [RouteParamsModel]
            public partial record Model
            {
                public RouteQuery? Rest { get; init; }
            }
            """);

        diags.ShouldContain(d => d.Id == "SUIN005");
    }

    [TestMethod]
    public void SUIN006_MultipleRouteQueryProperties()
    {
        var diags = RunGenerator(Boilerplate + """

            [RouteParamsModel]
            public partial record Model
            {
                public RouteQuery First { get; init; }
                public RouteQuery Second { get; init; }
            }
            """);

        diags.ShouldContain(d => d.Id == "SUIN006");
    }

    [TestMethod]
    public void SUIN007_UnsupportedPropertyType()
    {
        var diags = RunGenerator(Boilerplate + """

            public class NotParsable { }

            [RouteParamsModel]
            public partial record Model
            {
                public required NotParsable Thing { get; init; }
            }
            """);

        diags.ShouldContain(d => d.Id == "SUIN007");
    }

    [TestMethod]
    public void MixedPrimaryCtorAndRequiredProperty_NoDiagnostics()
    {
        var diags = RunGenerator(Boilerplate + """

            [RouteParamsModel]
            public partial record Model(int Id)
            {
                public required string Name { get; init; }
                public System.Guid? Tag { get; init; }
                public RouteQuery Rest { get; init; }
            }
            """);

        diags.Where(d => d.Id.StartsWith("SUIN", System.StringComparison.Ordinal)).ShouldBeEmpty();
    }

    [TestMethod]
    public void SUIN008_InstanceField()
    {
        var diags = RunGenerator(Boilerplate + """

            [RouteParamsModel]
            public partial record Model
            {
                private int _backing;
                public required int Id { get; init; }
            }
            """);

        diags.ShouldContain(d => d.Id == "SUIN008");
    }

    [TestMethod]
    public void SUIN008_InstanceReadonlyField()
    {
        var diags = RunGenerator(Boilerplate + """

            [RouteParamsModel]
            public partial record Model
            {
                public readonly int Tag = 0;
                public required int Id { get; init; }
            }
            """);

        diags.ShouldContain(d => d.Id == "SUIN008");
    }

    [TestMethod]
    public void StaticAndConstFields_AreIgnored()
    {
        var diags = RunGenerator(Boilerplate + """

            [RouteParamsModel]
            public partial record Model
            {
                public const int DefaultId = 1;
                public static readonly string Sentinel = "x";
                private static int _counter;

                public static int StaticProp { get; set; }

                public required int Id { get; init; }
            }
            """);

        diags.Where(d => d.Id.StartsWith("SUIN", System.StringComparison.Ordinal)).ShouldBeEmpty();
    }

    [TestMethod]
    public void SUIN009_ExpressionBodiedProperty_WithInit()
    {
        // An init-only property with a custom getter expression body is not an auto-property.
        var diags = RunGenerator(Boilerplate + """

            [RouteParamsModel]
            public partial record Model
            {
                private readonly int _id;
                public int Id { get => _id; init => _id = value; }
            }
            """);

        diags.ShouldContain(d => d.Id == "SUIN009");
    }

    [TestMethod]
    public void SUIN009_AccessorWithBlockBody()
    {
        var diags = RunGenerator(Boilerplate + """

            [RouteParamsModel]
            public partial record Model
            {
                public required int Id
                {
                    get { return field; }
                    init { field = value; }
                }
            }
            """);

        diags.ShouldContain(d => d.Id == "SUIN009");
    }
}
