using PrefixClassName.MsTest;
using Shouldly;

namespace Singulink.UI.Navigation.Tests;

[PrefixTestClass]
public class RouteQueryBuilderTests
{
    [TestMethod]
    public void Add_NewKey_Succeeds()
    {
        var b = new RouteQueryBuilder();
        b.Add("id", 1).Add("name", "alice");
        b.Count.ShouldBe(2);

        var q = b.ToQuery();
        q.TryGetValue<int>("id", out int id).ShouldBeTrue();
        id.ShouldBe(1);
        q.TryGetValue<string>("name", out string? name).ShouldBeTrue();
        name.ShouldBe("alice");
    }

    [TestMethod]
    public void Add_DuplicateKey_Throws()
    {
        var b = new RouteQueryBuilder();
        b.Add("a", 1);
        Should.Throw<ArgumentException>(() => b.Add("a", 2));
    }

    [TestMethod]
    public void Set_NewKey_Adds()
    {
        var b = new RouteQueryBuilder();
        b.Set("a", 1);
        b.Count.ShouldBe(1);
    }

    [TestMethod]
    public void Set_ExistingKey_Replaces()
    {
        var b = new RouteQueryBuilder();
        b.Add("a", 1).Set("a", 2);
        b.Count.ShouldBe(1);
        b.TryGetValue<int>("a", out int v).ShouldBeTrue();
        v.ShouldBe(2);
    }

    [TestMethod]
    public void Remove_ExistingKey_Removes()
    {
        var b = new RouteQueryBuilder();
        b.Add("a", 1).Add("b", 2);
        b.Remove("a");
        b.Count.ShouldBe(1);
        b.ContainsKey("a").ShouldBeFalse();
        b.ContainsKey("b").ShouldBeTrue();
    }

    [TestMethod]
    public void Remove_MissingKey_NoOp()
    {
        var b = new RouteQueryBuilder();
        b.Add("a", 1);
        b.Remove("missing");
        b.Count.ShouldBe(1);
    }

    [TestMethod]
    public void Remove_MultipleKeys()
    {
        var b = new RouteQueryBuilder();
        b.Add("a", 1).Add("b", 2).Add("c", 3);
        b.Remove("a", "c");
        b.Count.ShouldBe(1);
        b.ContainsKey("b").ShouldBeTrue();
    }

    [TestMethod]
    public void TryGetValue_BadFormat_Throws()
    {
        var b = new RouteQueryBuilder();
        b.Add("id", "abc");
        Should.Throw<FormatException>(() => b.TryGetValue<int>("id", out _));
    }

    [TestMethod]
    public void ToQuery_Empty_ReturnsEmpty()
    {
        new RouteQueryBuilder().ToQuery().ShouldBe(RouteQuery.Empty);
    }

    [TestMethod]
    public void RoundTrip_FromQueryToBuilderToQuery()
    {
        var q = new RouteQuery(("a", "1"), ("b", "2"));
        var b = q.ToBuilder();
        b.Count.ShouldBe(2);
        b.ToQuery().Equals(q).ShouldBeTrue();
    }

    [TestMethod]
    public void Capacity_DoesNotChangeBehavior()
    {
        var b = new RouteQueryBuilder(capacity: 16);
        b.Add("a", 1);
        b.Count.ShouldBe(1);
    }
}
