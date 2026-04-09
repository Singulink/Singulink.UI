using System.Collections.Immutable;
using PrefixClassName.MsTest;
using Shouldly;
using Singulink.UI.Navigation.Tests.TestSupport;

namespace Singulink.UI.Navigation.Tests;

[PrefixTestClass]
public class ValueListTests
{
    #region Construction / Properties

    [TestMethod]
    public void Default_IsEmpty()
    {
        ValueList<int> list = default;
        list.Count.ShouldBe(0);
        list.Value.Length.ShouldBe(0);
        list.ToString().ShouldBe(string.Empty);
        list.AsSpan().Length.ShouldBe(0);
    }

    [TestMethod]
    public void Construct_FromSpan_CopiesItems()
    {
        ReadOnlySpan<int> data = [1, 2, 3];
        var list = new ValueList<int>(data);
        list.Count.ShouldBe(3);
        list[0].ShouldBe(1);
        list[1].ShouldBe(2);
        list[2].ShouldBe(3);
    }

    [TestMethod]
    public void Construct_FromImmutableArray_NoCopy()
    {
        var arr = ImmutableArray.Create(10, 20, 30);
        var list = new ValueList<int>(arr);
        list.Value.ShouldBe(arr);
    }

    [TestMethod]
    public void Construct_FromIEnumerable_Materializes()
    {
        IEnumerable<int> seq = Enumerable.Range(1, 4);
        var list = new ValueList<int>(seq);
        list.Count.ShouldBe(4);
        list.AsSpan().ToArray().ShouldBe(new[] { 1, 2, 3, 4 });
    }

    [TestMethod]
    public void Indexer_OutOfRange_Throws()
    {
        var list = new ValueList<int>(1, 2);
        Should.Throw<IndexOutOfRangeException>(() => _ = list[5]);
    }

    [TestMethod]
    public void GetEnumerator_YieldsItemsInOrder()
    {
        var list = new ValueList<int>(3, 1, 4, 1, 5, 9);
        var collected = new List<int>();
        foreach (int v in list)
            collected.Add(v);

        collected.ShouldBe(new[] { 3, 1, 4, 1, 5, 9 });
    }

    [TestMethod]
    public void ToArray_ReturnsCopyOfItems()
    {
        var list = new ValueList<int>(1, 2, 3);
        int[] arr = list.ToArray();
        arr.ShouldBe(new[] { 1, 2, 3 });
    }

    #endregion

    #region Implicit Conversions

    [TestMethod]
    public void ImplicitConversion_FromImmutableArray_Works()
    {
        ValueList<int> list = ImmutableArray.Create(1, 2, 3);
        list.AsSpan().ToArray().ShouldBe(new[] { 1, 2, 3 });
    }

    [TestMethod]
    public void ImplicitConversion_ToImmutableArray_Works()
    {
        var list = new ValueList<int>(7, 8, 9);
        ImmutableArray<int> arr = list;
        arr.ShouldBe(ImmutableArray.Create(7, 8, 9));
    }

    [TestMethod]
    public void ImplicitConversion_ToReadOnlySpan_Works()
    {
        var list = new ValueList<int>(1, 2, 3);
        ReadOnlySpan<int> span = list;
        span.ToArray().ShouldBe(new[] { 1, 2, 3 });
    }

    [TestMethod]
    public void ImplicitConversion_ToReadOnlyMemory_Works()
    {
        var list = new ValueList<int>(1, 2, 3);
        ReadOnlyMemory<int> mem = list;
        mem.ToArray().ShouldBe(new[] { 1, 2, 3 });
    }

    #endregion

    #region Formatting / ToString

    [TestMethod]
    public void ToString_Empty_ProducesEmptyString()
    {
        default(ValueList<int>).ToString().ShouldBe(string.Empty);
    }

    [TestMethod]
    public void ToString_TildeSafeIntElements_UsesTildeSeparated()
    {
        new ValueList<int>(1, 2, 3).ToString().ShouldBe("~1~2~3");
    }

    [TestMethod]
    public void ToString_SingleIntElement_UsesTildeSeparated()
    {
        new ValueList<int>(42).ToString().ShouldBe("~42");
    }

    [TestMethod]
    public void ToString_StringsWithoutTildes_UsesTildeSeparated()
    {
        new ValueList<string>("foo", "bar").ToString().ShouldBe("~foo~bar");
    }

    [TestMethod]
    public void ToString_StringsContainingTilde_UsesLengthPrefixed()
    {
        // "a~b" contains a tilde so we must fall back to length-prefixed form.
        new ValueList<string>("a~b", "c").ToString().ShouldBe("3~a~b1~c");
    }

    [TestMethod]
    public void ToString_GuidElements_UsesTildeSeparated()
    {
        var g1 = new Guid("11111111-2222-3333-4444-555555555555");
        var g2 = new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        new ValueList<Guid>(g1, g2).ToString().ShouldBe($"~{g1}~{g2}");
    }

    #endregion

    #region Parse / TryParse

    [TestMethod]
    public void Parse_Empty_ReturnsEmpty()
    {
        var list = ValueList<int>.Parse(string.Empty);
        list.Count.ShouldBe(0);
    }

    [TestMethod]
    public void Parse_TildeSeparatedInts_RoundTrips()
    {
        var list = ValueList<int>.Parse("~1~2~3");
        list.AsSpan().ToArray().ShouldBe(new[] { 1, 2, 3 });
    }

    [TestMethod]
    public void Parse_LengthPrefixedStrings_RoundTrips()
    {
        var list = ValueList<string>.Parse("3~a~b1~c");
        list.Count.ShouldBe(2);
        list[0].ShouldBe("a~b");
        list[1].ShouldBe("c");
    }

    [TestMethod]
    public void Parse_InvalidFormat_Throws()
    {
        Should.Throw<FormatException>(() => ValueList<int>.Parse("~1~not-a-number"));
    }

    [TestMethod]
    public void Parse_MalformedLengthPrefix_Throws()
    {
        // No '~' separator after the digits, so we can't find the length.
        Should.Throw<FormatException>(() => ValueList<int>.Parse("garbage"));
    }

    [TestMethod]
    public void Parse_LengthPrefixOverflow_Throws()
    {
        // Length prefix says 99 chars but value only has 3.
        Should.Throw<FormatException>(() => ValueList<string>.Parse("99~abc"));
    }

    [TestMethod]
    public void Parse_NegativeLengthPrefix_Throws()
    {
        Should.Throw<FormatException>(() => ValueList<string>.Parse("-1~abc"));
    }

    [TestMethod]
    public void TryParse_Null_ReturnsFalse()
    {
        ValueList<int>.TryParse(null, out var result).ShouldBeFalse();
        result.Count.ShouldBe(0);
    }

    [TestMethod]
    public void TryParse_Empty_ReturnsTrueWithEmpty()
    {
        ValueList<int>.TryParse(string.Empty, out var result).ShouldBeTrue();
        result.Count.ShouldBe(0);
    }

    [TestMethod]
    public void TryParse_BadIntElement_ReturnsFalse()
    {
        ValueList<int>.TryParse("~1~abc~3", out _).ShouldBeFalse();
    }

    [TestMethod]
    public void IParsable_StaticDispatch_RoundTrips()
    {
        // Exercise the explicit IParsable<TSelf> implementation.
        var list = Parse<ValueList<int>>("~5~10");
        list.AsSpan().ToArray().ShouldBe(new[] { 5, 10 });

        TryParseStatic<ValueList<int>>("~5~10", out var parsed).ShouldBeTrue();
        parsed.AsSpan().ToArray().ShouldBe(new[] { 5, 10 });
    }

    private static T Parse<T>(string s) where T : IParsable<T> => T.Parse(s, null);

    private static bool TryParseStatic<T>(string s, out T result) where T : IParsable<T> => T.TryParse(s, null, out result!);

    #endregion

    #region Round-trip

    [TestMethod]
    public void RoundTrip_Ints()
    {
        var original = new ValueList<int>(1, -2, 3, int.MaxValue, int.MinValue);
        var parsed = ValueList<int>.Parse(original.ToString());
        parsed.ShouldBe(original);
    }

    [TestMethod]
    public void RoundTrip_StringsWithoutTildes()
    {
        var original = new ValueList<string>("alpha", "beta", "gamma");
        var parsed = ValueList<string>.Parse(original.ToString());
        parsed.ShouldBe(original);
    }

    [TestMethod]
    public void RoundTrip_StringsContainingTildes()
    {
        var original = new ValueList<string>("a~b", "~~", "no-tilde");
        string encoded = original.ToString();
        var parsed = ValueList<string>.Parse(encoded);
        parsed.ShouldBe(original);
    }

    [TestMethod]
    public void RoundTrip_Guids()
    {
        var original = new ValueList<Guid>(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var parsed = ValueList<Guid>.Parse(original.ToString());
        parsed.ShouldBe(original);
    }

    #endregion

    #region Equality

    [TestMethod]
    public void Equality_SameItems_Equal()
    {
        var a = new ValueList<int>(1, 2, 3);
        var b = new ValueList<int>(1, 2, 3);
        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
        (a != b).ShouldBeFalse();
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [TestMethod]
    public void Equality_DifferentLength_NotEqual()
    {
        var a = new ValueList<int>(1, 2);
        var b = new ValueList<int>(1, 2, 3);
        a.ShouldNotBe(b);
        (a != b).ShouldBeTrue();
    }

    [TestMethod]
    public void Equality_DifferentItems_NotEqual()
    {
        var a = new ValueList<int>(1, 2, 3);
        var b = new ValueList<int>(1, 2, 4);
        a.ShouldNotBe(b);
    }

    [TestMethod]
    public void Equality_DefaultAndExplicitEmpty_Equal()
    {
        ValueList<int> a = default;
        var b = new ValueList<int>(ImmutableArray<int>.Empty);
        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [TestMethod]
    public void Equality_AgainstObject_HandlesNonValueList()
    {
        var a = new ValueList<int>(1);
        a.Equals((object)"not a list").ShouldBeFalse();
        a.Equals((object?)null).ShouldBeFalse();
        a.Equals((object)new ValueList<int>(1)).ShouldBeTrue();
    }

    #endregion

    #region Route usage

    [TestMethod]
    public void RouteValue_AsPathParameter_RoundTrips()
    {
        AsyncContextTest.Run(async () =>
        {
            var nav = BuildNav();

            var part = Route.Build<ValueList<int>>(p => $"tags/{p}").Root<TagsVm>();
            var concrete = part.ToConcrete(new ValueList<int>(1, 2, 3));
            concrete.Path.ShouldBe("tags/~1~2~3");

            (await nav.NavigateAsync("tags/~10~20~30")).ShouldBe(NavigationResult.Success);
            var vm = (TagsVm)nav.WiredViews[0].ViewModel;
            vm.GetParameter().AsSpan().ToArray().ShouldBe(new[] { 10, 20, 30 });

            nav.CurrentRoute.ToString().ShouldBe("tags/~10~20~30");
        });
    }

    [TestMethod]
    public void RouteValue_PathParameter_EmptyList_Throws()
    {
        // An empty list serializes to the empty string, which is not a valid path segment.
        var part = Route.Build<ValueList<int>>(p => $"tags/{p}").Root<TagsVm>();
        Should.Throw<FormatException>(() => part.ToConcrete(default(ValueList<int>)));
    }

    [TestMethod]
    public void RouteValue_AsQueryParameter_RoundTrips()
    {
        var original = new ValueList<int>(1, 2, 3);
        var query = new RouteQuery(("ids", original.ToString()));

        query.TryGetValue<ValueList<int>>("ids", out var parsed).ShouldBeTrue();
        parsed.ShouldBe(original);
    }

    [TestMethod]
    public void RouteValue_AsQueryParameter_StringsWithTildes_RoundTrips()
    {
        var original = new ValueList<string>("a~b", "c");
        var query = new RouteQuery(("tags", original.ToString()));

        query.TryGetValue<ValueList<string>>("tags", out var parsed).ShouldBeTrue();
        parsed.ShouldBe(original);
    }

    private static TestNavigator BuildNav() => new(b =>
    {
        b.MapRoutedView<TagsVm, TagsView>();
        b.AddRoute(Route.Build<ValueList<int>>(p => $"tags/{p}").Root<TagsVm>());
    });

    public class TagsVm : RecordedLifecycleViewModel, IRoutedViewModel<ValueList<int>>
    {
        public ValueList<int> GetParameter() => this.Parameter;
    }

    public class TagsView : FakeView
    {
    }

    #endregion
}
