using PrefixClassName.MsTest;
using Shouldly;
using Singulink.UI.Navigation;

namespace Singulink.UI.Navigation.Generators.Tests;

/// <summary>
/// Verifies the generated <see cref="IRouteParamsModel{TSelf}"/> implementations behave correctly for a variety of model shapes.
/// </summary>
[PrefixTestClass]
public partial class RouteParamsModelTests
{
    #region Models

    [RouteParamsModel]
    public partial record AllExplicit
    {
        public required int IntValue { get; init; }

        public string? StringValue { get; init; }

        public Guid? NullableGuid { get; init; }

        public RouteQuery Rest { get; init; }
    }

    [RouteParamsModel]
    public partial record NoOptional
    {
        public required int A { get; init; }

        public required string B { get; init; }
    }

    [RouteParamsModel]
    public partial record NoRequired
    {
        public int? X { get; init; }

        public string? Y { get; init; }
    }

    [RouteParamsModel]
    public partial record NoRest
    {
        public required int Id { get; init; }

        public string? Name { get; init; }
    }

    [RouteParamsModel]
    public partial record RestOnly
    {
        public RouteQuery Query { get; init; }
    }

    [RouteParamsModel]
    public partial record Empty;

    [RouteParamsModel]
    public partial record PrimaryCtorOnly(int Id, string? Name);

    [RouteParamsModel]
    public partial record PrimaryCtorWithRest(int Id, string? Name)
    {
        public RouteQuery Rest { get; init; }
    }

    [RouteParamsModel]
    public partial record MixedCtorAndProperties(int Id)
    {
        public required string Name { get; init; }

        public Guid? Tag { get; init; }

        public RouteQuery Rest { get; init; }
    }

    [RouteParamsModel]
    public partial record NestedContainer
    {
        [RouteParamsModel]
        public partial record Inner
        {
            public required int Value { get; init; }
        }
    }

    #endregion

    private static IRouteParamsModel<T> Interface<T>() where T : IRouteParamsModel<T> => default!;

    private static bool TryCreate<T>(RouteValuesCollection values, out T? result) where T : IRouteParamsModel<T>
    {
        bool ok = T.TryCreate(values, out T? value);
        result = value;
        return ok;
    }

    private static IReadOnlyList<string> RequiredNames<T>() where T : IRouteParamsModel<T> => T.RequiredParameterNames;

    private static bool ProvidesRest<T>() where T : IRouteParamsModel<T> => T.ProvidesRemainingQueryAccess;

    [TestMethod]
    public void AllExplicit_RoundTrip()
    {
        var guid = Guid.NewGuid();

        var original = new AllExplicit
        {
            IntValue = 42,
            StringValue = "hello",
            NullableGuid = guid,
            Rest = new RouteQuery(("extra", "42")),
        };

        IRouteParamsModel model = original;
        var values = model.ToRouteValues();

        TryCreate<AllExplicit>(values, out var result).ShouldBeTrue();
        result.ShouldBe(original);
    }

    [TestMethod]
    public void AllExplicit_ReportsMetadata()
    {
        RequiredNames<AllExplicit>().ShouldBe(["IntValue"]);
        ProvidesRest<AllExplicit>().ShouldBeTrue();
    }

    [TestMethod]
    public void AllExplicit_MissingRequired_Fails()
    {
        var collection = new RouteValuesCollection();

        TryCreate<AllExplicit>(collection, out var result).ShouldBeFalse();
        result.ShouldBeNull();
    }

    [TestMethod]
    public void AllExplicit_UnparseableRequired_Fails()
    {
        var collection = new RouteValuesCollection() {
            { "IntValue", "not-an-int" },
        };

        TryCreate<AllExplicit>(collection, out _).ShouldBeFalse();
    }

    [TestMethod]
    public void AllExplicit_OptionalOmitted_DefaultsToNull()
    {
        var collection = new RouteValuesCollection() {
            { "IntValue", 7 },
        };

        TryCreate<AllExplicit>(collection, out var result).ShouldBeTrue();
        result.ShouldNotBeNull();
        result.IntValue.ShouldBe(7);
        result.StringValue.ShouldBeNull();
        result.NullableGuid.ShouldBeNull();
        result.Rest.Count.ShouldBe(0);
    }

    [TestMethod]
    public void AllExplicit_OptionalUnparseable_TreatedAsMissing()
    {
        var collection = new RouteValuesCollection {
            { "IntValue", 7 },
            { "NullableGuid", "not-a-guid" },
        };

        TryCreate<AllExplicit>(collection, out var result).ShouldBeTrue();
        result.ShouldNotBeNull();
        result.NullableGuid.ShouldBeNull();
    }

    [TestMethod]
    public void AllExplicit_RestCapturesUnconsumedQueryEntries()
    {
        var collection = new RouteValuesCollection {
            { "IntValue", 1 },
            { "extra1", "one" },
            { "extra2", "two" },
        };

        TryCreate<AllExplicit>(collection, out var result).ShouldBeTrue();
        result.ShouldNotBeNull();
        result.Rest.Count.ShouldBe(2);
        result.Rest.TryGetValue<string>("extra1", out string? e1).ShouldBeTrue();
        e1.ShouldBe("one");
        result.Rest.TryGetValue<string>("extra2", out string? e2).ShouldBeTrue();
        e2.ShouldBe("two");
    }

    [TestMethod]
    public void NoOptional_RoundTrip()
    {
        var original = new NoOptional { A = 10, B = "abc" };
        var values = ((IRouteParamsModel)original).ToRouteValues();

        TryCreate<NoOptional>(values, out var result).ShouldBeTrue();
        result.ShouldBe(original);
        ProvidesRest<NoOptional>().ShouldBeFalse();
        RequiredNames<NoOptional>().ShouldBe(["A", "B"]);
    }

    [TestMethod]
    public void NoRequired_AllOmitted_Succeeds()
    {
        var collection = new RouteValuesCollection();

        TryCreate<NoRequired>(collection, out var result).ShouldBeTrue();
        result.ShouldNotBeNull();
        result.X.ShouldBeNull();
        result.Y.ShouldBeNull();

        RequiredNames<NoRequired>().Count.ShouldBe(0);
    }

    [TestMethod]
    public void NoRequired_RoundTrip_WithValues()
    {
        var original = new NoRequired { X = 5, Y = "str" };
        var values = ((IRouteParamsModel)original).ToRouteValues();

        TryCreate<NoRequired>(values, out var result).ShouldBeTrue();
        result.ShouldBe(original);
    }

    [TestMethod]
    public void NoRequired_OnlyNonNullOptionalsAreInCollection()
    {
        var original = new NoRequired { X = 5, Y = null };
        var values = ((IRouteParamsModel)original).ToRouteValues();

        values.Count.ShouldBe(1);
    }

    [TestMethod]
    public void NoRest_ExtraQueryEntriesIgnored()
    {
        var collection = new RouteValuesCollection {
            { "Id", 1 },
            { "Name", "foo" },
            { "extra", "ignored" },
        };

        TryCreate<NoRest>(collection, out var result).ShouldBeTrue();
        result.ShouldNotBeNull();
        result.Id.ShouldBe(1);
        result.Name.ShouldBe("foo");

        ProvidesRest<NoRest>().ShouldBeFalse();
    }

    [TestMethod]
    public void RestOnly_CapturesAllEntries()
    {
        var collection = new RouteValuesCollection {
            { "a", "1" },
            { "b", "2" },
        };

        TryCreate<RestOnly>(collection, out var result).ShouldBeTrue();
        result.ShouldNotBeNull();
        result.Query.Count.ShouldBe(2);

        RequiredNames<RestOnly>().Count.ShouldBe(0);
        ProvidesRest<RestOnly>().ShouldBeTrue();
    }

    [TestMethod]
    public void Empty_TryCreate_AlwaysSucceeds()
    {
        var collection = new RouteValuesCollection();
        TryCreate<Empty>(collection, out var result).ShouldBeTrue();
        result.ShouldNotBeNull();

        RequiredNames<Empty>().Count.ShouldBe(0);
        ProvidesRest<Empty>().ShouldBeFalse();
    }

    [TestMethod]
    public void Empty_ToCollection_IsEmpty()
    {
        var e = new Empty();
        var values = ((IRouteParamsModel)e).ToRouteValues();
        values.Count.ShouldBe(0);
    }

    [TestMethod]
    public void PrimaryCtorOnly_RoundTrip()
    {
        var original = new PrimaryCtorOnly(42, "bar");
        var values = ((IRouteParamsModel)original).ToRouteValues();

        TryCreate<PrimaryCtorOnly>(values, out var result).ShouldBeTrue();
        result.ShouldBe(original);

        RequiredNames<PrimaryCtorOnly>().ShouldBe(["Id"]);
    }

    [TestMethod]
    public void PrimaryCtorOnly_OptionalOmitted()
    {
        var collection = new RouteValuesCollection() {
            { "Id", 99 },
        };

        TryCreate<PrimaryCtorOnly>(collection, out var result).ShouldBeTrue();
        result.ShouldNotBeNull();
        result.Id.ShouldBe(99);
        result.Name.ShouldBeNull();
    }

    [TestMethod]
    public void PrimaryCtorOnly_MissingRequired_Fails()
    {
        var collection = new RouteValuesCollection();
        TryCreate<PrimaryCtorOnly>(collection, out _).ShouldBeFalse();
    }

    [TestMethod]
    public void PrimaryCtorWithRest_RoundTrip()
    {
        var original = new PrimaryCtorWithRest(1, "n") { Rest = new RouteQuery(("k", "v")) };
        var values = ((IRouteParamsModel)original).ToRouteValues();

        TryCreate<PrimaryCtorWithRest>(values, out var result).ShouldBeTrue();
        result.ShouldBe(original);

        ProvidesRest<PrimaryCtorWithRest>().ShouldBeTrue();
    }

    [TestMethod]
    public void MixedCtorAndProperties_RoundTrip()
    {
        var guid = Guid.NewGuid();
        var original = new MixedCtorAndProperties(7)
        {
            Name = "abc",
            Tag = guid,
            Rest = new RouteQuery(("r", "1")),
        };

        var values = ((IRouteParamsModel)original).ToRouteValues();

        TryCreate<MixedCtorAndProperties>(values, out var result).ShouldBeTrue();
        result.ShouldBe(original);

        RequiredNames<MixedCtorAndProperties>().ShouldBe(["Id", "Name"], ignoreOrder: true);
    }

    [TestMethod]
    public void MixedCtorAndProperties_MissingPrimaryCtorParam_Fails()
    {
        var collection = new RouteValuesCollection() {
            { "Name", "abc" },
        };

        TryCreate<MixedCtorAndProperties>(collection, out _).ShouldBeFalse();
    }

    [TestMethod]
    public void MixedCtorAndProperties_MissingRequiredProperty_Fails()
    {
        var collection = new RouteValuesCollection() {
            { "Id", 7 },
        };

        TryCreate<MixedCtorAndProperties>(collection, out _).ShouldBeFalse();
    }

    [TestMethod]
    public void NestedModel_RoundTrip()
    {
        var original = new NestedContainer.Inner { Value = 99 };
        var values = ((IRouteParamsModel)original).ToRouteValues();

        TryCreate<NestedContainer.Inner>(values, out var result).ShouldBeTrue();
        result.ShouldBe(original);
    }
}
