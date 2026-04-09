using System.Globalization;
using PrefixClassName.MsTest;
using Shouldly;
using Singulink.UI.Navigation.Utilities;

namespace Singulink.UI.Navigation.Tests;

/// <summary>
/// Verifies the internal route value formatter/parser used for path parameters and query string values.
/// </summary>
[PrefixTestClass]
public class RouteValueConverterTests
{
    [TestMethod]
    public void Format_String_RoundTrips()
    {
        RouteValueConverter.Format("hello").ShouldBe("hello");
    }

    [TestMethod]
    public void Format_Int_UsesInvariant()
    {
        var prev = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            RouteValueConverter.Format(1234).ShouldBe("1234");
        }
        finally
        {
            CultureInfo.CurrentCulture = prev;
        }
    }

    [TestMethod]
    public void Format_Double_UsesG17()
    {
        // 0.1 is famously not exactly 0.1 in binary; G17 preserves full precision so it round-trips.
        string formatted = RouteValueConverter.Format(0.1);
        RouteValueConverter.TryParse<double>(formatted, out double parsed).ShouldBeTrue();
        parsed.ShouldBe(0.1);
    }

    [TestMethod]
    public void Format_Float_UsesG9()
    {
        string formatted = RouteValueConverter.Format(0.1f);
        RouteValueConverter.TryParse<float>(formatted, out float parsed).ShouldBeTrue();
        parsed.ShouldBe(0.1f);
    }

    [TestMethod]
    public void Format_DateTime_UsesORoundTripFormat()
    {
        var dt = new DateTime(2026, 4, 26, 10, 30, 15, DateTimeKind.Utc);
        string formatted = RouteValueConverter.Format(dt);
        formatted.ShouldBe(dt.ToString("O", CultureInfo.InvariantCulture));

        RouteValueConverter.TryParse<DateTime>(formatted, out var parsed).ShouldBeTrue();
        parsed.ShouldBe(dt);
        parsed.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [TestMethod]
    public void Format_DateTimeOffset_UsesO()
    {
        var dto = new DateTimeOffset(2026, 4, 26, 10, 30, 15, TimeSpan.FromHours(-5));
        string formatted = RouteValueConverter.Format(dto);
        RouteValueConverter.TryParse<DateTimeOffset>(formatted, out var parsed).ShouldBeTrue();
        parsed.ShouldBe(dto);
        parsed.Offset.ShouldBe(TimeSpan.FromHours(-5));
    }

    [TestMethod]
    public void Format_DateOnly_UsesO()
    {
        var d = new DateOnly(2026, 4, 26);
        string formatted = RouteValueConverter.Format(d);
        RouteValueConverter.TryParse<DateOnly>(formatted, out var parsed).ShouldBeTrue();
        parsed.ShouldBe(d);
    }

    [TestMethod]
    public void Format_TimeOnly_UsesO()
    {
        var t = new TimeOnly(10, 30, 15);
        string formatted = RouteValueConverter.Format(t);
        RouteValueConverter.TryParse<TimeOnly>(formatted, out var parsed).ShouldBeTrue();
        parsed.ShouldBe(t);
    }

    [TestMethod]
    public void TryParse_Guid_RoundTrips()
    {
        var guid = Guid.NewGuid();
        string formatted = RouteValueConverter.Format(guid);
        RouteValueConverter.TryParse<Guid>(formatted, out var parsed).ShouldBeTrue();
        parsed.ShouldBe(guid);
    }

    [TestMethod]
    public void TryParse_Int_Invalid_Fails()
    {
        RouteValueConverter.TryParse<int>("not-a-number", out _).ShouldBeFalse();
    }

    [TestMethod]
    public void TryParse_DateTime_NonRoundTripFormat_Fails()
    {
        // Only the "O" format is accepted on parse.
        RouteValueConverter.TryParse<DateTime>("2026-04-26", out _).ShouldBeFalse();
    }

    [TestMethod]
    public void TryParse_RespectsInvariantCulture()
    {
        var prev = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            // German uses comma decimal — "1,5" would parse as 1.5 in German but not invariant.
            RouteValueConverter.TryParse<double>("1.5", out double parsed).ShouldBeTrue();
            parsed.ShouldBe(1.5);
        }
        finally
        {
            CultureInfo.CurrentCulture = prev;
        }
    }

    [TestMethod]
    public void TryParse_CustomIParsable_Works()
    {
        RouteValueConverter.TryParse<MyIparsable>("hi:5", out var parsed).ShouldBeTrue();
        parsed.Prefix.ShouldBe("hi");
        parsed.Number.ShouldBe(5);
    }

    public readonly record struct MyIparsable(string Prefix, int Number) : IParsable<MyIparsable>
    {
        public static MyIparsable Parse(string s, IFormatProvider? provider)
        {
            if (!TryParse(s, provider, out var result))
                throw new FormatException();
            return result;
        }

        public static bool TryParse(string? s, IFormatProvider? provider, out MyIparsable result)
        {
            if (s is null)
            {
                result = default;
                return false;
            }

            int colon = s.IndexOf(':');
            if (colon < 0 || !int.TryParse(s.AsSpan()[(colon + 1)..], provider, out int n))
            {
                result = default;
                return false;
            }

            result = new MyIparsable(s[..colon], n);
            return true;
        }
    }
}
