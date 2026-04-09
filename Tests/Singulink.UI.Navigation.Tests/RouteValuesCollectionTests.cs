using PrefixClassName.MsTest;
using Shouldly;

namespace Singulink.UI.Navigation.Tests;

/// <summary>
/// Verifies the three-phase (Building/Consuming/Done) behavior of <see cref="RouteValuesCollection"/> and its reservation rules. These tests do
/// not exercise the source generator.
/// </summary>
[PrefixTestClass]
public class RouteValuesCollectionTests
{
    #region Reservation — ArgumentException for key conflicts

    [TestMethod]
    public void Add_DuplicateKey_Throws()
    {
        var c = new RouteValuesCollection();
        c.Add("a", 1);
        Should.Throw<ArgumentException>(() => c.Add("a", 2));
    }

    [TestMethod]
    public void Reserve_DuplicateKey_Throws()
    {
        var c = new RouteValuesCollection();
        c.Reserve("a");
        Should.Throw<ArgumentException>(() => c.Reserve("a"));
    }

    [TestMethod]
    public void Reserve_ThenAddSameKey_Throws()
    {
        var c = new RouteValuesCollection();
        c.Reserve("a");
        Should.Throw<ArgumentException>(() => c.Add("a", 1));
    }

    [TestMethod]
    public void AddQuery_KeyMatchingAdded_Throws()
    {
        var c = new RouteValuesCollection();
        c.Add("Id", 1);
        Should.Throw<ArgumentException>(() => c.AddQuery(new RouteQuery(("Id", "2"))));
    }

    [TestMethod]
    public void AddQuery_KeyMatchingReserved_Throws()
    {
        var c = new RouteValuesCollection();
        c.Reserve("Name");
        Should.Throw<ArgumentException>(() => c.AddQuery(new RouteQuery(("Name", "x"))));
    }

    [TestMethod]
    public void AddQuery_NoConflict_Succeeds()
    {
        var c = new RouteValuesCollection();
        c.Add("Id", 1);
        c.Reserve("Name");
        c.AddQuery(new RouteQuery(("Other", "z")));

        // After AddQuery the collection is in Consuming state; Count still accessible.
        c.Count.ShouldBe(2);
    }

    #endregion

    #region Building → Consuming transition (AddQuery or first consume call)

    [TestMethod]
    public void AddQuery_TransitionsToConsuming()
    {
        var c = new RouteValuesCollection();
        c.AddQuery(RouteQuery.Empty);

        Should.Throw<InvalidOperationException>(() => c.Add("a", 1));
        Should.Throw<InvalidOperationException>(() => c.Reserve("a"));
        Should.Throw<InvalidOperationException>(() => c.AddQuery(RouteQuery.Empty));
    }

    [TestMethod]
    public void TryConsume_TransitionsToConsuming()
    {
        var c = new RouteValuesCollection();
        c.Add("Id", 42);
        c.TryConsume<int>("Id", out int id).ShouldBeTrue();
        id.ShouldBe(42);
        Should.Throw<InvalidOperationException>(() => c.Add("b", 1));
    }

    [TestMethod]
    public void Add_AfterConsuming_Throws()
    {
        var c = new RouteValuesCollection();
        c.Add("a", 1);
        c.AddQuery(RouteQuery.Empty); // transitions to Consuming
        Should.Throw<InvalidOperationException>(() => c.Add("b", 2));
    }

    [TestMethod]
    public void Reserve_AfterConsuming_Throws()
    {
        var c = new RouteValuesCollection();
        c.AddQuery(RouteQuery.Empty);
        Should.Throw<InvalidOperationException>(() => c.Reserve("a"));
    }

    [TestMethod]
    public void AddQuery_AfterConsuming_Throws()
    {
        var c = new RouteValuesCollection();
        c.AddQuery(RouteQuery.Empty);
        Should.Throw<InvalidOperationException>(() => c.AddQuery(new RouteQuery(("a", "1"))));
    }

    #endregion

    #region Consuming → Done transition (ConsumeQuery)

    [TestMethod]
    public void ConsumeQuery_ReturnsRemainingEntries()
    {
        var c = new RouteValuesCollection();
        c.Add("x", "hello");
        c.AddQuery(new RouteQuery(("extra", "val")));

        c.TryConsume<string>("x", out _);
        var rest = c.ConsumeQuery();

        rest.Count.ShouldBe(1);
        rest.TryGetValue<string>("extra", out string? v).ShouldBeTrue();
        v.ShouldBe("val");
    }

    [TestMethod]
    public void ConsumeQuery_Twice_Throws()
    {
        var c = new RouteValuesCollection();
        c.AddQuery(RouteQuery.Empty);
        _ = c.ConsumeQuery();
        Should.Throw<InvalidOperationException>(() => c.ConsumeQuery());
    }

    [TestMethod]
    public void Count_AfterConsumeQuery_Throws()
    {
        var c = new RouteValuesCollection();
        c.AddQuery(RouteQuery.Empty);
        _ = c.ConsumeQuery();
        Should.Throw<InvalidOperationException>(() => _ = c.Count);
    }

    [TestMethod]
    public void TryConsume_AfterConsumeQuery_Throws()
    {
        var c = new RouteValuesCollection();
        c.AddQuery(RouteQuery.Empty);
        _ = c.ConsumeQuery();
        Should.Throw<InvalidOperationException>(() => c.TryConsume<int>("k", out _));
    }

    [TestMethod]
    public void Enumerate_AfterConsumeQuery_Throws()
    {
        var c = new RouteValuesCollection();
        c.AddQuery(RouteQuery.Empty);
        _ = c.ConsumeQuery();
        Should.Throw<InvalidOperationException>(() =>
        {
            foreach (var e in c) { }
        });
    }

    [TestMethod]
    public void Add_AfterConsumeQuery_Throws()
    {
        var c = new RouteValuesCollection();
        c.AddQuery(RouteQuery.Empty);
        _ = c.ConsumeQuery();
        Should.Throw<InvalidOperationException>(() => c.Add("a", 1));
    }

    #endregion
}
