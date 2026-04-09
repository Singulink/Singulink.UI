using PrefixClassName.MsTest;
using Shouldly;
using Singulink.UI.Navigation.Generators;

namespace Singulink.UI.Navigation.Generators.Tests;

[PrefixTestClass]
public partial class EquatableArrayTests
{
    [TestMethod]
    public void Equals_SameContent_DifferentArrays_ReturnsTrue()
    {
        var a = new EquatableArray<string>(["x", "y", "z"]);
        var b = new EquatableArray<string>(["x", "y", "z"]);

        a.Equals(b).ShouldBeTrue();
        (a == b).ShouldBeTrue();
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [TestMethod]
    public void Equals_DifferentContent_ReturnsFalse()
    {
        var a = new EquatableArray<string>(["x", "y"]);
        var b = new EquatableArray<string>(["x", "z"]);

        a.Equals(b).ShouldBeFalse();
    }

    [TestMethod]
    public void Equals_DifferentLengths_ReturnsFalse()
    {
        var a = new EquatableArray<int>([1, 2]);
        var b = new EquatableArray<int>([1, 2, 3]);

        a.Equals(b).ShouldBeFalse();
    }

    [TestMethod]
    public void Equals_DefaultAndEmpty_AreEqual()
    {
        var a = default(EquatableArray<int>);
        var b = EquatableArray<int>.Empty;

        a.Equals(b).ShouldBeTrue();
    }

    [TestMethod]
    public void RecordContainingEquatableArray_ProvidesStructuralEquality()
    {
        var a = new Sample(1, new EquatableArray<string>(["a", "b"]));
        var b = new Sample(1, new EquatableArray<string>(["a", "b"]));

        a.ShouldBe(b);
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    private sealed record Sample(int Id, EquatableArray<string> Values);
}
