using NUnit.Framework;
using NUnit.Framework.Internal;

namespace NtpTick.Tests;

[TestFixture]
public class NtpTimestampTests
{
    static IEnumerable<TestCaseData> FromULongSource()
        => TimestampsSource(includeImpreciseDTOs: true).Select(x =>
            new TestCaseData(x.valueULong, x.valueDTO, x.reference)
                .SetArgDisplayNames(x.name));
    [TestCaseSource(nameof(FromULongSource))]
    public void FromULongTests(ulong value, DateTimeOffset expected, DateTimeOffset reference)
    {
        var timestamp = new NtpTimestamp(value);
        var dto = timestamp.ToDateTimeOffset(reference);
        Assert.That(dto, Is.EqualTo(expected));
    }

    static IEnumerable<TestCaseData> FromDateTimeOffsetSource()
        => TimestampsSource(includeImpreciseDTOs: false).Select(x =>
            new TestCaseData(x.valueDTO, x.valueULong)
                .SetArgDisplayNames(x.name));
    [TestCaseSource(nameof(FromDateTimeOffsetSource))]
    public void FromDateTimeOffsetTests(DateTimeOffset value, ulong expected)
    {
        var timestamp = new NtpTimestamp(value);
        Assert.That(timestamp.Value, Is.EqualTo(expected));
    }

    static IEnumerable<(ulong valueULong, DateTimeOffset valueDTO, DateTimeOffset reference, string name)> TimestampsSource(bool includeImpreciseDTOs)
    {
        yield return (0x0000000000000000, NtpTimestamp.NtpEpoch, NtpTimestamp.NtpEpoch, "NTP epoch");
        yield return (0x0000000100000000, DTO(1900, 1, 1, 0, 0, second: 1), NtpTimestamp.NtpEpoch, "NTP epoch +1s");
        yield return (0x0000003C00000000, DTO(1900, 1, 1, 0, minute: 1, 0), NtpTimestamp.NtpEpoch, "NTP epoch +1m");
        yield return (0x00000E1000000000, DTO(1900, 1, 1, hour: 1, 0, 0), NtpTimestamp.NtpEpoch, "NTP epoch +1h");
        yield return (0x0001518000000000, DTO(1900, 1, day: 2, 0, 0, 0), NtpTimestamp.NtpEpoch, "NTP epoch +1d");
        yield return (0x0000000080000000, DTO(1900, 1, 1, 0, 0, 0, ms: 500), NtpTimestamp.NtpEpoch, "NTP epoch +500ms");
        yield return (0x0000000040000000, DTO(1900, 1, 1, 0, 0, 0, ms: 250), NtpTimestamp.NtpEpoch, "NTP epoch +250ms");
        yield return (0x000000001999999A, DTO(1900, 1, 1, 0, 0, 0, ms: 100), NtpTimestamp.NtpEpoch, "NTP epoch +100ms");

        // 2136 => <2068, 2204>
        // 2272 => <2204, 2340>
        yield return (0xBC189B3F00000000, DTO(2000, 1, 1, 15, 26, 55), DTO(1990, 1, 1, 0, 0, 0), "2000-01-01 15:26:55 (1990)");
        yield return (0xBC189B3F00000000, DTO(2000, 1, 1, 15, 26, 55), DTO(2000, 1, 1, 15, 26, 55), "2000-01-01 15:26:55 (2000 [exact])");
        yield return (0xBC189B3F00000000, DTO(2000, 1, 1, 15, 26, 55), DTO(2010, 1, 1, 0, 0, 0), "2000-01-01 15:26:55 (2010)");
        yield return (0xBC189B3F00000000, NE(DTO(2000, 1, 1, 15, 26, 55)), DTO(2070, 1, 1, 0, 0, 0), "2000-01-01 15:26:55 +E1 (2070)");
        yield return (0xBC189B3F00000000, NE(DTO(2000, 1, 1, 15, 26, 55)), DTO(2200, 1, 1, 0, 0, 0), "2000-01-01 15:26:55 +E1 (2200)");
        yield return (0xBC189B3F00000000, NE(NE(DTO(2000, 1, 1, 15, 26, 55))), DTO(2250, 1, 1, 0, 0, 0), "2000-01-01 15:26:55 +E2 (2250)");
        // below -68 for era 0
        yield return (0xBC189B3F00000000, DTO(2000, 1, 1, 15, 26, 55), DTO(1910, 1, 1, 0, 0, 0), "2000-01-01 15:26:55 (1910)");

        if (includeImpreciseDTOs)
        {
            yield return (0x0000000000418937, DTO(1900, 1, 1, 0, 0, 0).AddTicks(9999), NtpTimestamp.NtpEpoch, "fractions: +9999 ticks");
            yield return (0x00000000000001AD, DTO(1900, 1, 1, 0, 0, 0).AddTicks(0), NtpTimestamp.NtpEpoch, "fractions: <0 ticks [01AD]");
            yield return (0x00000000000000D6, DTO(1900, 1, 1, 0, 0, 0).AddTicks(0), NtpTimestamp.NtpEpoch, "fractions: <0 ticks [00D6]");
            yield return (0x0000000000000863, DTO(1900, 1, 1, 0, 0, 0).AddTicks(4), NtpTimestamp.NtpEpoch, "fractions: +4 ticks");
            yield return (0x00000000FFFFFCD2, DTO(1900, 1, 1, 0, 0, 0, ms: 999, us: 999).AddTicks(8), NtpTimestamp.NtpEpoch, "fractions: DTO rounding [FCD2]");
            yield return (0x00000000FFFFFCD3, DTO(1900, 1, 1, 0, 0, 0, ms: 999, us: 999).AddTicks(8), NtpTimestamp.NtpEpoch, "fractions: DTO rounding [FCD3]");
        }
    }

    static DateTimeOffset DTO(int year, int month, int day, int hour, int minute, int second, int ms = 0, int us = 0)
        => new(year, month, day, hour, minute, second, ms, us, TimeSpan.Zero);

    static DateTimeOffset NE(DateTimeOffset dto)
        => dto.AddSeconds((ulong)NtpTimestamp.EraSeconds);
}
