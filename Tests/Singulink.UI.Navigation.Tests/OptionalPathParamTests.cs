using PrefixClassName.MsTest;
using Shouldly;

namespace Singulink.UI.Navigation.Tests;

[PrefixTestClass]
public class OptionalPathParamTests
{
    [TestMethod]
    public void None_HasNoValue()
    {
        var p = OptionalPathParam<int>.None;
        p.HasValue.ShouldBeFalse();
        p.ToString().ShouldBeNull();
    }

    [TestMethod]
    public void Default_HasNoValue()
    {
        OptionalPathParam<int> p = default;
        p.HasValue.ShouldBeFalse();
    }

    [TestMethod]
    public void Construct_WithValue_HasValue()
    {
        var p = new OptionalPathParam<int>(42);
        p.HasValue.ShouldBeTrue();
        p.Value.ShouldBe(42);
    }

    [TestMethod]
    public void Construct_WithReferenceTypeNull_HasNoValue()
    {
        var p = new OptionalPathParam<string>(null);
        p.HasValue.ShouldBeFalse();
    }

    [TestMethod]
    public void Value_WhenNoValue_Throws()
    {
        // Note: for value types, `T? _value` with `T : notnull` is unconstrained nullable so `_value ?? throw` is a no-op;
        // a reference-type `T` is required to exercise the exception path.
        var p = OptionalPathParam<string>.None;
        Should.Throw<InvalidOperationException>(() => _ = p.Value);
    }

    [TestMethod]
    public void GetValueOrDefault_NoValue_ReturnsDefault()
    {
        var p = OptionalPathParam<int>.None;
        p.GetValueOrDefault(99).ShouldBe(99);
    }

    [TestMethod]
    public void GetValueOrDefault_HasValue_ReturnsValue()
    {
        var p = new OptionalPathParam<int>(7);
        p.GetValueOrDefault(99).ShouldBe(7);
    }

    [TestMethod]
    public void ImplicitConversion_FromValue_HasValue()
    {
        OptionalPathParam<int> p = 5;
        p.HasValue.ShouldBeTrue();
        p.Value.ShouldBe(5);
    }

    [TestMethod]
    public void TryParse_ValidValue_Succeeds()
    {
        OptionalPathParam<int>.TryParse("42", out var p).ShouldBeTrue();
        p.HasValue.ShouldBeTrue();
        p.Value.ShouldBe(42);
    }

    [TestMethod]
    public void TryParse_Empty_Fails()
    {
        OptionalPathParam<int>.TryParse(string.Empty, out var p).ShouldBeFalse();
        p.HasValue.ShouldBeFalse();
    }

    [TestMethod]
    public void TryParse_Invalid_Fails()
    {
        OptionalPathParam<int>.TryParse("abc", out var p).ShouldBeFalse();
        p.HasValue.ShouldBeFalse();
    }

    [TestMethod]
    public void ToString_HasValue_FormatsValue()
    {
        var p = new OptionalPathParam<int>(42);
        p.ToString().ShouldBe("42");
    }

    [TestMethod]
    public void Equality_BothEmpty_Equal()
    {
        OptionalPathParam<int>.None.ShouldBe(OptionalPathParam<int>.None);
        (OptionalPathParam<int>.None == OptionalPathParam<int>.None).ShouldBeTrue();
    }

    [TestMethod]
    public void Equality_SameValue_Equal()
    {
        new OptionalPathParam<int>(5).ShouldBe(new OptionalPathParam<int>(5));
        new OptionalPathParam<int>(5).GetHashCode().ShouldBe(new OptionalPathParam<int>(5).GetHashCode());
    }

    [TestMethod]
    public void Equality_DifferentValues_NotEqual()
    {
        new OptionalPathParam<int>(5).ShouldNotBe(new OptionalPathParam<int>(6));
        (new OptionalPathParam<int>(5) != OptionalPathParam<int>.None).ShouldBeTrue();
    }

    [TestMethod]
    public void ToNullable_StructHasValue_ReturnsValue()
    {
        var p = new OptionalPathParam<int>(7);
        int? nullable = p.ToNullable();
        nullable.ShouldBe(7);
    }

    [TestMethod]
    public void ToNullable_StructNoValue_ReturnsNull()
    {
        var p = OptionalPathParam<int>.None;
        int? nullable = p.ToNullable();
        nullable.ShouldBeNull();
    }
}
