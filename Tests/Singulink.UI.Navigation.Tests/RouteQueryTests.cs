using PrefixClassName.MsTest;
using Shouldly;

namespace Singulink.UI.Navigation.Tests;

[PrefixTestClass]
public class RouteQueryTests
{
    [TestMethod]
    public void Default_IsEmpty()
    {
        RouteQuery q = default;
        q.Count.ShouldBe(0);
        q.ShouldBe(RouteQuery.Empty);
        q.GetHashCode().ShouldBe(RouteQuery.Empty.GetHashCode());
        q.ToString().ShouldBe(string.Empty);
    }

    [TestMethod]
    public void Construct_WithEntries_PreservesInsertionOrder()
    {
        var q = new RouteQuery(("b", "2"), ("a", "1"), ("c", "3"));
        q.Select(e => e.Key).ShouldBe(["b", "a", "c"]);
        q.Select(e => e.Value).ShouldBe(["2", "1", "3"]);
    }

    [TestMethod]
    public void Construct_WithDuplicateKey_LastWins()
    {
        var q = new RouteQuery(("a", "1"), ("a", "2"));
        q.Count.ShouldBe(1);
        q.TryGetValue<string>("a", out string? value).ShouldBeTrue();
        value.ShouldBe("2");
    }

    [TestMethod]
    public void Construct_WithEmptyKey_Throws()
    {
        Should.Throw<ArgumentException>(() => new RouteQuery((string.Empty, "x")));
    }

    [TestMethod]
    public void Equality_KeyOrderInsensitive()
    {
        var q1 = new RouteQuery(("a", "1"), ("b", "2"));
        var q2 = new RouteQuery(("b", "2"), ("a", "1"));
        q1.Equals(q2).ShouldBeTrue();
        q1.GetHashCode().ShouldBe(q2.GetHashCode());
        (q1 == q2).ShouldBeTrue();
        (q1 != q2).ShouldBeFalse();
    }

    [TestMethod]
    public void Equality_DifferentValue_NotEqual()
    {
        var q1 = new RouteQuery(("a", "1"));
        var q2 = new RouteQuery(("a", "2"));
        q1.Equals(q2).ShouldBeFalse();
        (q1 != q2).ShouldBeTrue();
    }

    [TestMethod]
    public void TryGetValue_String_ReturnsValue()
    {
        var q = new RouteQuery(("name", "alice"));
        q.TryGetValue<string>("name", out string? value).ShouldBeTrue();
        value.ShouldBe("alice");
    }

    [TestMethod]
    public void TryGetValue_Int_Parses()
    {
        var q = new RouteQuery(("id", "42"));
        q.TryGetValue<int>("id", out int value).ShouldBeTrue();
        value.ShouldBe(42);
    }

    [TestMethod]
    public void TryGetValue_Guid_Parses()
    {
        var guid = Guid.NewGuid();
        var q = new RouteQuery(("g", guid.ToString()));
        q.TryGetValue<Guid>("g", out var value).ShouldBeTrue();
        value.ShouldBe(guid);
    }

    [TestMethod]
    public void TryGetValue_DateTime_RoundTripsViaO()
    {
        var dt = new DateTime(2026, 4, 26, 10, 30, 15, DateTimeKind.Utc);
        var q = new RouteQuery(("d", dt.ToString("O", System.Globalization.CultureInfo.InvariantCulture)));
        q.TryGetValue<DateTime>("d", out var value).ShouldBeTrue();
        value.ShouldBe(dt);
        value.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [TestMethod]
    public void TryGetValue_MissingKey_ReturnsFalse()
    {
        var q = new RouteQuery(("a", "1"));
        q.TryGetValue<int>("missing", out int value).ShouldBeFalse();
        value.ShouldBe(0);
    }

    [TestMethod]
    public void TryGetValue_BadFormat_ReturnsFalse()
    {
        var q = new RouteQuery(("id", "abc"));
        q.TryGetValue<int>("id", out int value).ShouldBeFalse();
        value.ShouldBe(0);
    }

    [TestMethod]
    public void TryGetValue_BadFormat_ThrowParseError_Throws()
    {
        var q = new RouteQuery(("id", "abc"));
        Should.Throw<FormatException>(() => q.TryGetValue<int>("id", throwOnParseError: true, out _));
    }

    [TestMethod]
    public void TryGetValue_BadFormat_NoThrowParseError_ReturnsFalse()
    {
        var q = new RouteQuery(("id", "abc"));
        q.TryGetValue<int>("id", throwOnParseError: false, out int value).ShouldBeFalse();
        value.ShouldBe(0);
    }

    [TestMethod]
    public void TryGetValue_FoundKey_BadFormat_ReturnsFalseWithFoundKeyTrue()
    {
        var q = new RouteQuery(("id", "abc"));
        q.TryGetValue<int>("id", out bool foundKey, out int value).ShouldBeFalse();
        foundKey.ShouldBeTrue();
        value.ShouldBe(0);
    }

    [TestMethod]
    public void TryGetValue_FoundKey_MissingKey_ReturnsFalseWithFoundKeyFalse()
    {
        var q = new RouteQuery(("a", "1"));
        q.TryGetValue<int>("missing", out bool foundKey, out int value).ShouldBeFalse();
        foundKey.ShouldBeFalse();
        value.ShouldBe(0);
    }

    [TestMethod]
    public void TryGetValue_FoundKey_GoodValue_ReturnsTrue()
    {
        var q = new RouteQuery(("id", "42"));
        q.TryGetValue<int>("id", out bool foundKey, out int value).ShouldBeTrue();
        foundKey.ShouldBeTrue();
        value.ShouldBe(42);
    }

    [TestMethod]
    public void GetValue_GoodValue_ReturnsValue()
    {
        var q = new RouteQuery(("id", "42"));
        q.GetValue<int>("id").ShouldBe(42);
    }

    [TestMethod]
    public void GetValue_MissingKey_Throws()
    {
        var q = new RouteQuery(("a", "1"));
        Should.Throw<KeyNotFoundException>(() => q.GetValue<int>("missing"));
    }

    [TestMethod]
    public void GetValue_BadFormat_Throws()
    {
        var q = new RouteQuery(("id", "abc"));
        Should.Throw<FormatException>(() => q.GetValue<int>("id"));
    }

    [TestMethod]
    public void ContainsKey_FindsExisting()
    {
        var q = new RouteQuery(("a", "1"));
        q.ContainsKey("a").ShouldBeTrue();
        q.ContainsKey("missing").ShouldBeFalse();
    }

    [TestMethod]
    public void ToString_FormatsAsQueryString()
    {
        var q = new RouteQuery(("a", "1"), ("b", "2"));
        q.ToString().ShouldBe("a=1&b=2");
    }

    [TestMethod]
    public void ToString_EscapesSpecialCharacters()
    {
        var q = new RouteQuery(("k&y", "v=l"));
        q.ToString().ShouldContain("k%26y");
        q.ToString().ShouldContain("v%3Dl");
    }

    [TestMethod]
    public void ToString_Empty_ReturnsEmpty()
    {
        RouteQuery.Empty.ToString().ShouldBe(string.Empty);
        ((RouteQuery)default).ToString().ShouldBe(string.Empty);
    }

    [TestMethod]
    public void Enumerate_DefaultInstance_YieldsNothing()
    {
        RouteQuery q = default;
        q.ToList().Count.ShouldBe(0);
    }

    [TestMethod]
    public void ToBuilder_RoundTripsToOriginal()
    {
        var q = new RouteQuery(("a", "1"), ("b", "2"));
        var rebuilt = q.ToBuilder().ToQuery();
        rebuilt.Equals(q).ShouldBeTrue();
    }
}
