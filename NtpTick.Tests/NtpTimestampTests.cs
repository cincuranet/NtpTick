using NUnit.Framework;

namespace NtpTick.Tests;

[TestFixture]
public class NtpTimestampTests
{
    [Test]
    public void NtpEpoch_ReturnsZero()
    {
        var dto = new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var timestamp = new NtpTimestamp(dto);
        Assert.That((ulong)timestamp, Is.Zero);
    }

    [Test]
    public void OneSecondAfterEpoch_ReturnsCorrectValue()
    {
        var dto = new DateTimeOffset(1900, 1, 1, 0, 0, 1, TimeSpan.Zero);
        var timestamp = new NtpTimestamp(dto);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(timestamp >> 32, Is.EqualTo(1));
            Assert.That(timestamp & 0xFFFFFFFF, Is.Zero);
        }
    }

    [Test]
    public void WithFraction_EncodesCorrectly()
    {
        var dto = new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMilliseconds(500);
        var timestamp = new NtpTimestamp(dto);
        var seconds = timestamp >> 32;
        var fraction = timestamp & 0xFFFFFFFF;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(seconds, Is.Zero);
            Assert.That(fraction, Is.EqualTo(2147483647UL));
        }
    }

    [Test]
    public void KnownValue_January1_2000()
    {
        var dto = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var timestamp = new NtpTimestamp(dto);
        var seconds = timestamp >> 32;
        var expectedSeconds = (ulong)(dto - NtpTimestamp.NtpEpoch).TotalSeconds;
        Assert.That(seconds, Is.EqualTo(expectedSeconds));
    }

    [Test]
    public void KnownValue_February7_2036_6_28_16()
    {
        var dto = new DateTimeOffset(2036, 2, 7, 6, 28, 16, TimeSpan.Zero);
        var timestamp = new NtpTimestamp(dto);
        var seconds = timestamp >> 32;
        Assert.That(seconds, Is.Zero);
    }

    [Test]
    public void EraWrapAround_FirstSecondOfEra1()
    {
        var dto = new DateTimeOffset(2036, 2, 7, 6, 28, 16, TimeSpan.Zero);
        var timestamp = new NtpTimestamp(dto);
        var seconds = timestamp >> 32;
        Assert.That(seconds, Is.LessThan(uint.MaxValue));
    }

    [Test]
    public void RandomValue_2024_06_15()
    {
        var dto = new DateTimeOffset(2024, 6, 15, 14, 30, 45, 123, TimeSpan.Zero);
        var timestamp = new NtpTimestamp(dto);
        var expectedSeconds = (dto - NtpTimestamp.NtpEpoch).TotalSeconds;
        var wireSeconds = (ulong)expectedSeconds % (1UL << 32);
        Assert.That(timestamp >> 32, Is.EqualTo(wireSeconds));
    }

    [Test]
    public void RandomValue_1970_01_01()
    {
        var dto = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var timestamp = new NtpTimestamp(dto);
        Assert.That(timestamp >> 32, Is.EqualTo(2208988800UL));
    }

    [Test]
    public void BeforeEpoch_ThrowsException()
    {
        var dto = new DateTimeOffset(1899, 12, 31, 23, 59, 59, TimeSpan.Zero);
        Assert.Throws<ArgumentOutOfRangeException>(() => new NtpTimestamp(dto));
    }

    [Test]
    public void ToDateTimeOffset_Zero_WithEra0Reference_ReturnsNtpEpoch()
    {
        var result = new NtpTimestamp(0UL).ToDateTimeOffset(new DateTimeOffset(1950, 1, 1, 0, 0, 0, TimeSpan.Zero));
        Assert.That(result, Is.EqualTo(NtpTimestamp.NtpEpoch));
    }

    [Test]
    public void ToDateTimeOffset_OneSecond_WithEra0Reference_ReturnsCorrectDateTime()
    {
        var result = new NtpTimestamp(1UL << 32).ToDateTimeOffset(new DateTimeOffset(1950, 1, 1, 0, 0, 0, TimeSpan.Zero));
        Assert.That(result, Is.EqualTo(new DateTimeOffset(1900, 1, 1, 0, 0, 1, TimeSpan.Zero)));
    }

    [Test]
    public void ToDateTimeOffset_WithFraction_DecodesCorrectly()
    {
        var result = new NtpTimestamp(2147483648UL).ToDateTimeOffset(new DateTimeOffset(1950, 1, 1, 0, 0, 0, TimeSpan.Zero));
        Assert.That(result, Is.EqualTo(new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMilliseconds(500)));
    }

    [Test]
    public void ToDateTimeOffset_Era0Timestamp_WithEra1Reference_SelectsCorrectEra()
    {
        var result = new NtpTimestamp(100UL << 32).ToDateTimeOffset(new DateTimeOffset(2050, 1, 1, 0, 0, 0, TimeSpan.Zero));
        Assert.That(result.Year, Is.GreaterThanOrEqualTo(2036));
    }

    [Test]
    public void ToDateTimeOffset_Era1Timestamp_WithEra0Reference_SelectsCorrectEra()
    {
        var result = new NtpTimestamp(100UL << 32).ToDateTimeOffset(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero));
        Assert.That(result.Year, Is.GreaterThanOrEqualTo(2036));
    }

    [Test]
    public void ToDateTimeOffset_NearEraWrap_ClosestToReference()
    {
        var result = new NtpTimestamp(0UL << 32).ToDateTimeOffset(new DateTimeOffset(2036, 2, 7, 6, 28, 15, TimeSpan.Zero));
        Assert.That(result.Year, Is.EqualTo(2036));
    }

    [Test]
    public void MaxDateTimeOffset_DoesNotOverflow()
    {
        Assert.DoesNotThrow(() => new NtpTimestamp(DateTimeOffset.MaxValue.ToUniversalTime()));
    }

    [Test]
    public void MaxUInt64_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => new NtpTimestamp(ulong.MaxValue));
    }
}
