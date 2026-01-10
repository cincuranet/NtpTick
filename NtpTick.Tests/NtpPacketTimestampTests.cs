using NUnit.Framework;

namespace NtpTick.Tests;

[TestFixture]
public class NtpPacketTimestampTests
{
    [Test]
    public void RawTimestampFromDateTime_NtpEpoch_ReturnsZero()
    {
        var ntpEpoch = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var raw = NtpPacket.RawTimestampFromDateTime(ntpEpoch);
        Assert.That(raw, Is.Zero);
    }

    [Test]
    public void RawTimestampFromDateTime_OneSecondAfterEpoch_ReturnsCorrectValue()
    {
        var dt = new DateTime(1900, 1, 1, 0, 0, 1, DateTimeKind.Utc);
        var raw = NtpPacket.RawTimestampFromDateTime(dt);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(raw >> 32, Is.EqualTo(1));
            Assert.That(raw & 0xFFFFFFFF, Is.Zero);
        }
    }

    [Test]
    public void RawTimestampFromDateTime_WithFraction_EncodesCorrectly()
    {
        var dt = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(500);
        var raw = NtpPacket.RawTimestampFromDateTime(dt);
        var seconds = raw >> 32;
        var fraction = raw & 0xFFFFFFFF;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(seconds, Is.Zero);
            Assert.That(fraction, Is.GreaterThan(2147000000U).And.LessThan(2148000000U));
        }
    }

    [Test]
    public void RawTimestampFromDateTime_KnownValue_January1_2000()
    {
        var dt = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var raw = NtpPacket.RawTimestampFromDateTime(dt);
        var seconds = raw >> 32;
        var expectedSeconds = (ulong)(dt - new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        Assert.That(seconds, Is.EqualTo(expectedSeconds));
    }

    [Test]
    public void RawTimestampFromDateTime_KnownValue_February7_2036_6_28_16()
    {
        var dt = new DateTime(2036, 2, 7, 6, 28, 16, DateTimeKind.Utc);
        var raw = NtpPacket.RawTimestampFromDateTime(dt);
        var seconds = raw >> 32;
        Assert.That(seconds, Is.Zero);
    }

    [Test]
    public void RawTimestampFromDateTime_EraWrapAround_FirstSecondOfEra1()
    {
        var dt = new DateTime(2036, 2, 7, 6, 28, 16, DateTimeKind.Utc);
        var raw = NtpPacket.RawTimestampFromDateTime(dt);
        var seconds = raw >> 32;
        Assert.That(seconds, Is.LessThan(uint.MaxValue));
    }

    [Test]
    public void RawTimestampFromDateTime_RandomValue_2024_06_15()
    {
        var dt = new DateTime(2024, 6, 15, 14, 30, 45, 123, DateTimeKind.Utc);
        var raw = NtpPacket.RawTimestampFromDateTime(dt);
        var expectedSeconds = (dt - new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        var wireSeconds = (ulong)expectedSeconds % (1UL << 32);
        Assert.That(raw >> 32, Is.EqualTo(wireSeconds));
    }

    [Test]
    public void RawTimestampFromDateTime_RandomValue_1970_01_01()
    {
        var dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var raw = NtpPacket.RawTimestampFromDateTime(dt);
        Assert.That(raw >> 32, Is.EqualTo(2208988800UL));
    }

    [Test]
    public void RawTimestampFromDateTime_BeforeEpoch_ThrowsException()
    {
        var dt = new DateTime(1899, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        Assert.Throws<ArgumentOutOfRangeException>(() => NtpPacket.RawTimestampFromDateTime(dt));
    }

    [Test]
    public void RawTimestampFromDateTime_NonUtcDateTime_ThrowsException()
    {
        var dt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local);
        Assert.Throws<ArgumentException>(() => NtpPacket.RawTimestampFromDateTime(dt));
    }

    [Test]
    public void RawTimestampFromDateTime_UnspecifiedKind_ThrowsException()
    {
        var dt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
        Assert.Throws<ArgumentException>(() => NtpPacket.RawTimestampFromDateTime(dt));
    }

    [Test]
    public void RawTimestampToDateTime_Zero_WithEra0Reference_ReturnsNtpEpoch()
    {
        var reference = new DateTime(1950, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var result = NtpPacket.RawTimestampToDateTime(0UL, reference);
        Assert.That(result, Is.EqualTo(new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
    }

    [Test]
    public void RawTimestampToDateTime_OneSecond_WithEra0Reference_ReturnsCorrectDateTime()
    {
        var reference = new DateTime(1950, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var raw = 1UL << 32;
        var result = NtpPacket.RawTimestampToDateTime(raw, reference);
        Assert.That(result, Is.EqualTo(new DateTime(1900, 1, 1, 0, 0, 1, DateTimeKind.Utc)));
    }

    [Test]
    public void RawTimestampToDateTime_WithFraction_DecodesCorrectly()
    {
        var reference = new DateTime(1950, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var raw = 0UL | 2147483648U;
        var result = NtpPacket.RawTimestampToDateTime(raw, reference);
        var expected = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(0.5);
        Assert.That(result, Is.EqualTo(expected).Within(TimeSpan.FromMilliseconds(1)));
    }

    [Test]
    public void RawTimestampToDateTime_Era0Timestamp_WithEra1Reference_SelectsCorrectEra()
    {
        var reference = new DateTime(2050, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var raw = 100UL << 32;
        var result = NtpPacket.RawTimestampToDateTime(raw, reference);
        Assert.That(result.Year, Is.GreaterThanOrEqualTo(2036));
    }

    [Test]
    public void RawTimestampToDateTime_Era1Timestamp_WithEra0Reference_SelectsCorrectEra()
    {
        var reference = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var raw = 100UL << 32;
        var result = NtpPacket.RawTimestampToDateTime(raw, reference);
        Assert.That(result.Year, Is.GreaterThanOrEqualTo(2036));
    }

    [Test]
    public void RawTimestampToDateTime_NearEraWrap_ClosestToReference()
    {
        var reference = new DateTime(2036, 2, 7, 6, 28, 15, DateTimeKind.Utc);
        var raw = 0UL << 32;
        var result = NtpPacket.RawTimestampToDateTime(raw, reference);
        Assert.That(result.Year, Is.EqualTo(2036));
    }

    [Test]
    public void RawTimestampToDateTime_NonUtcReference_ThrowsException()
    {
        var reference = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local);
        Assert.Throws<ArgumentException>(() => NtpPacket.RawTimestampToDateTime(0UL, reference));
    }

    [Test]
    public void RoundTrip_NtpEpoch_PreservesValue()
    {
        var original = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var raw = NtpPacket.RawTimestampFromDateTime(original);
        var result = NtpPacket.RawTimestampToDateTime(raw, original);
        Assert.That(result, Is.EqualTo(original));
    }

    [Test]
    public void RoundTrip_RandomDateTime_PreservesValue()
    {
        var original = new DateTime(2024, 6, 15, 14, 30, 45, 123, DateTimeKind.Utc);
        var raw = NtpPacket.RawTimestampFromDateTime(original);
        var result = NtpPacket.RawTimestampToDateTime(raw, original);
        Assert.That(result, Is.EqualTo(original).Within(TimeSpan.FromMilliseconds(1)));
    }

    [Test]
    public void RoundTrip_Year1970_PreservesValue()
    {
        var original = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var raw = NtpPacket.RawTimestampFromDateTime(original);
        var result = NtpPacket.RawTimestampToDateTime(raw, original);
        Assert.That(result, Is.EqualTo(original));
    }

    [Test]
    public void RoundTrip_Year2000_PreservesValue()
    {
        var original = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var raw = NtpPacket.RawTimestampFromDateTime(original);
        var result = NtpPacket.RawTimestampToDateTime(raw, original);
        Assert.That(result, Is.EqualTo(original));
    }

    [Test]
    public void RoundTrip_NearEraWrap_PreservesValue()
    {
        var original = new DateTime(2036, 2, 7, 6, 28, 15, DateTimeKind.Utc);
        var raw = NtpPacket.RawTimestampFromDateTime(original);
        var result = NtpPacket.RawTimestampToDateTime(raw, original);
        Assert.That(result, Is.EqualTo(original).Within(TimeSpan.FromSeconds(1)));
    }

    [Test]
    public void RoundTrip_AfterEraWrap_PreservesValue()
    {
        var original = new DateTime(2036, 2, 7, 6, 28, 17, DateTimeKind.Utc);
        var raw = NtpPacket.RawTimestampFromDateTime(original);
        var result = NtpPacket.RawTimestampToDateTime(raw, original);
        Assert.That(result, Is.EqualTo(original).Within(TimeSpan.FromSeconds(1)));
    }

    [Test]
    public void RoundTrip_FarIntoEra1_PreservesValue()
    {
        var original = new DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var raw = NtpPacket.RawTimestampFromDateTime(original);
        var result = NtpPacket.RawTimestampToDateTime(raw, original);
        Assert.That(result, Is.EqualTo(original).Within(TimeSpan.FromSeconds(1)));
    }

    [Test]
    public void RawTimestampFromDateTime_MaxDateTime_DoesNotOverflow()
    {
        var dt = DateTime.MaxValue.ToUniversalTime();
        Assert.DoesNotThrow(() => NtpPacket.RawTimestampFromDateTime(dt));
    }

    [Test]
    public void RawTimestampToDateTime_MaxValue_DoesNotThrow()
    {
        var reference = DateTime.UtcNow;
        Assert.DoesNotThrow(() => NtpPacket.RawTimestampToDateTime(ulong.MaxValue, reference));
    }
}
