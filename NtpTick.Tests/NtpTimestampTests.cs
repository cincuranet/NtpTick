using NUnit.Framework;

namespace NtpTick.Tests;

[TestFixture]
public class NtpTimestampTests
{
    [Test]
    public void NtpEpoch_ReturnsZero()
    {
        var dt = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var timestamp = new NtpTimestamp(dt);
        Assert.That((ulong)timestamp, Is.Zero);
    }

    [Test]
    public void OneSecondAfterEpoch_ReturnsCorrectValue()
    {
        var dt = new DateTime(1900, 1, 1, 0, 0, 1, DateTimeKind.Utc);
        var timestamp = new NtpTimestamp(dt);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(timestamp >> 32, Is.EqualTo(1));
            Assert.That(timestamp & 0xFFFFFFFF, Is.Zero);
        }
    }

    [Test]
    public void WithFraction_EncodesCorrectly()
    {
        var dt = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(500);
        var timestamp = new NtpTimestamp(dt);
        var seconds = timestamp >> 32;
        var fraction = timestamp & 0xFFFFFFFF;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(seconds, Is.Zero);
            Assert.That(fraction, Is.GreaterThan(2147000000U).And.LessThan(2148000000U));
        }
    }

    [Test]
    public void KnownValue_January1_2000()
    {
        var dt = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var timestamp = new NtpTimestamp(dt);
        var seconds = timestamp >> 32;
        var expectedSeconds = (ulong)(dt - NtpTimestamp.NtpEpoch).TotalSeconds;
        Assert.That(seconds, Is.EqualTo(expectedSeconds));
    }

    [Test]
    public void KnownValue_February7_2036_6_28_16()
    {
        var dt = new DateTime(2036, 2, 7, 6, 28, 16, DateTimeKind.Utc);
        var timestamp = new NtpTimestamp(dt);
        var seconds = timestamp >> 32;
        Assert.That(seconds, Is.Zero);
    }

    [Test]
    public void EraWrapAround_FirstSecondOfEra1()
    {
        var dt = new DateTime(2036, 2, 7, 6, 28, 16, DateTimeKind.Utc);
        var timestamp = new NtpTimestamp(dt);
        var seconds = timestamp >> 32;
        Assert.That(seconds, Is.LessThan(uint.MaxValue));
    }

    [Test]
    public void RandomValue_2024_06_15()
    {
        var dt = new DateTime(2024, 6, 15, 14, 30, 45, 123, DateTimeKind.Utc);
        var timestamp = new NtpTimestamp(dt);
        var expectedSeconds = (dt - NtpTimestamp.NtpEpoch).TotalSeconds;
        var wireSeconds = (ulong)expectedSeconds % (1UL << 32);
        Assert.That(timestamp >> 32, Is.EqualTo(wireSeconds));
    }

    [Test]
    public void RandomValue_1970_01_01()
    {
        var dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var timestamp = new NtpTimestamp(dt);
        Assert.That(timestamp >> 32, Is.EqualTo(2208988800UL));
    }

    [Test]
    public void BeforeEpoch_ThrowsException()
    {
        var dt = new DateTime(1899, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        Assert.Throws<ArgumentOutOfRangeException>(() => new NtpTimestamp(dt));
    }

    [Test]
    public void NonUtcDateTime_ThrowsException()
    {
        var dt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local);
        Assert.Throws<ArgumentException>(() => new NtpTimestamp(dt));
    }

    [Test]
    public void UnspecifiedKind_ThrowsException()
    {
        var dt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
        Assert.Throws<ArgumentException>(() => new NtpTimestamp(dt));
    }

    [Test]
    public void ToDateTime_Zero_WithEra0Reference_ReturnsNtpEpoch()
    {
        var result = new NtpTimestamp(0UL).ToDateTime(new DateTime(1950, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        Assert.That(result, Is.EqualTo(NtpTimestamp.NtpEpoch));
    }

    [Test]
    public void ToDateTime_OneSecond_WithEra0Reference_ReturnsCorrectDateTime()
    {
        var raw = 1UL << 32;
        var result = new NtpTimestamp(raw).ToDateTime(new DateTime(1950, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        Assert.That(result, Is.EqualTo(new DateTime(1900, 1, 1, 0, 0, 1, DateTimeKind.Utc)));
    }

    [Test]
    public void ToDateTime_WithFraction_DecodesCorrectly()
    {
        var reference = new DateTime(1950, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var raw = 0UL | 2147483648U;
        var result = new NtpTimestamp(raw).ToDateTime(reference);
        Assert.That(result, Is.EqualTo(new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(0.5)).Within(TimeSpan.FromMilliseconds(1)));
    }

    [Test]
    public void ToDateTime_Era0Timestamp_WithEra1Reference_SelectsCorrectEra()
    {
        var raw = 100UL << 32;
        var result = new NtpTimestamp(raw).ToDateTime(new DateTime(2050, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        Assert.That(result.Year, Is.GreaterThanOrEqualTo(2036));
    }

    [Test]
    public void ToDateTime_Era1Timestamp_WithEra0Reference_SelectsCorrectEra()
    {
        var raw = 100UL << 32;
        var result = new NtpTimestamp(raw).ToDateTime(new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        Assert.That(result.Year, Is.GreaterThanOrEqualTo(2036));
    }

    [Test]
    public void ToDateTime_NearEraWrap_ClosestToReference()
    {
        var raw = 0UL << 32;
        var result = new NtpTimestamp(raw).ToDateTime(new DateTime(2036, 2, 7, 6, 28, 15, DateTimeKind.Utc));
        Assert.That(result.Year, Is.EqualTo(2036));
    }

    [Test]
    public void ToDateTime_NonUtcReference_ThrowsException()
    {
        var timestamp = new NtpTimestamp(0UL);
        Assert.Throws<ArgumentException>(() => timestamp.ToDateTime(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local)));
    }

    [Test]
    public void MaxDateTime_DoesNotOverflow()
    {
        Assert.DoesNotThrow(() => new NtpTimestamp(DateTime.MaxValue.ToUniversalTime()));
    }

    [Test]
    public void MaxValue_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => new NtpTimestamp(ulong.MaxValue));
    }
}
