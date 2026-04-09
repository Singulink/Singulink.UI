using System.Collections.Immutable;
using PrefixClassName.MsTest;
using Shouldly;

namespace Singulink.UI.Navigation.Tests;

[PrefixTestClass]
public class Base64BytesTests
{
    [TestMethod]
    public void Default_IsEmpty()
    {
        Base64Bytes b = default;
        b.Value.Length.ShouldBe(0);
        b.ToString().ShouldBe(string.Empty);
    }

    [TestMethod]
    public void Construct_FromSpan_CopiesData()
    {
        ReadOnlySpan<byte> data = [1, 2, 3];
        var b = new Base64Bytes(data);
        b.Value.ShouldBe(ImmutableArray.Create<byte>(1, 2, 3));
    }

    [TestMethod]
    public void Construct_FromImmutableArray()
    {
        var data = ImmutableArray.Create<byte>(1, 2, 3);
        var b = new Base64Bytes(data);
        b.Value.ShouldBe(data);
    }

    [TestMethod]
    public void ToString_ProducesBase64()
    {
        var b = new Base64Bytes("hello"u8);
        b.ToString().ShouldBe(Convert.ToBase64String("hello"u8));
    }

    [TestMethod]
    public void Parse_ValidBase64_RoundTrips()
    {
        var original = new Base64Bytes("data"u8);
        string encoded = original.ToString();
        var parsed = Base64Bytes.Parse(encoded);
        parsed.ShouldBe(original);
    }

    [TestMethod]
    public void Parse_InvalidBase64_Throws()
    {
        Should.Throw<FormatException>(() => Base64Bytes.Parse("!!!not-base64!!!"));
    }

    [TestMethod]
    public void TryParse_ValidBase64_ReturnsTrue()
    {
        Base64Bytes.TryParse("aGVsbG8=", out var result).ShouldBeTrue();
        result.AsSpan().ToArray().ShouldBe("hello"u8.ToArray());
    }

    [TestMethod]
    public void TryParse_InvalidBase64_ReturnsFalse()
    {
        Base64Bytes.TryParse("!!!", out var result).ShouldBeFalse();
        result.Value.Length.ShouldBe(0);
    }

    [TestMethod]
    public void TryParse_Null_ReturnsFalse()
    {
        Base64Bytes.TryParse(null, out _).ShouldBeFalse();
    }

    [TestMethod]
    public void Equality_SameBytes_Equal()
    {
        var a = new Base64Bytes("data"u8);
        var b = new Base64Bytes("data"u8);
        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [TestMethod]
    public void Equality_DifferentBytes_NotEqual()
    {
        var a = new Base64Bytes("a"u8);
        var b = new Base64Bytes("b"u8);
        a.ShouldNotBe(b);
        (a != b).ShouldBeTrue();
    }

    [TestMethod]
    public void IParsable_RoundTrip_LargePayload()
    {
        byte[] bytes = new byte[2048];
        Random.Shared.NextBytes(bytes);
        var original = new Base64Bytes(bytes);
        Base64Bytes.TryParse(original.ToString(), out var parsed).ShouldBeTrue();
        parsed.ShouldBe(original);
    }

    [TestMethod]
    public void ImplicitConversions_Work()
    {
        Base64Bytes b = ImmutableArray.Create<byte>(1, 2, 3);
        ImmutableArray<byte> arr = b;
        ReadOnlySpan<byte> span = b;

        arr.ShouldBe(ImmutableArray.Create<byte>(1, 2, 3));
        span.ToArray().ShouldBe(new byte[] { 1, 2, 3 });
    }
}
