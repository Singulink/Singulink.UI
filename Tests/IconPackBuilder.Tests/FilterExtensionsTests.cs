using IconPackBuilder.ViewModels.Utilities;
using PrefixClassName.MsTest;
using Shouldly;

namespace IconPackBuilder.Tests;

[PrefixTestClass]
public class FilterExtensionsTests
{
    [TestMethod]
    public void MatchesFilter_EveryTermMustPrefixMatchAWord()
    {
        "Arrow Left Circle".MatchesFilter("arr").ShouldBeTrue();
        "Arrow Left Circle".MatchesFilter("left circ").ShouldBeTrue();
        "Arrow Left Circle".MatchesFilter("row").ShouldBeFalse();
        "Arrow Left Circle".MatchesFilter("arrow right").ShouldBeFalse();
        "Arrow Left Circle".MatchesFilter("  ").ShouldBeTrue();
        "Arrow Left Circle".MatchesFilter(FilterExtensions.SplitFilter("ARROW  left")).ShouldBeTrue();
    }

    [TestMethod]
    public void Filter_SelectsMatchingItems()
    {
        string[] items = ["Add Circle", "Alert", "Arrow Left"];

        items.Filter("a", s => s).ShouldBe(items);
        items.Filter("al", s => s).ShouldBe(["Alert"]);
        items.Filter("circle add", s => s).ShouldBe(["Add Circle"]);
        items.Filter("zzz", s => s).ShouldBeEmpty();
    }
}
