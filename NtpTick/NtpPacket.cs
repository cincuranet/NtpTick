using System.Buffers.Binary;

namespace NtpTick;

public class NtpPacket
{
    public NtpLeapIndicator LeapIndicator { get; set; } = NtpLeapIndicator.NoWarning;
    
    public NtpVersion Version { get; set; } = NtpVersion.Version4;
    
    public NtpMode Mode { get; set; } = NtpMode.Client;

    public byte Stratum { get; set; }

    /// <summary>
    /// Maximum interval between successive messages, in log₂(seconds). Typical range is 6 to 10.
    /// </summary>
    public sbyte Poll { get; set; }

    /// <summary>
    /// Signed log₂(seconds) of system clock precision (e.g., –18 ≈ 1 microsecond).
    /// </summary>
    public sbyte Precision { get; set; }

    /// <summary>
    /// Total round-trip delay to the reference clock, in NTP short format.
    /// </summary>
    public uint RootDelay { get; set; }

    /// <summary>
    /// Total dispersion to the reference clock, in NTP short format.
    /// </summary>
    public uint RootDispersion { get; set; }

    public NtpReferenceId ReferenceId { get; set; }
    
    public ulong ReferenceTimestampRaw { get; set; }
    public DateTime ReferenceTimestamp
    {
        get => RawTimestampToDateTime(ReferenceTimestampRaw, DateTime.UtcNow); 
        set => ReferenceTimestampRaw = RawTimestampFromDateTime(value);
    }
    
    public ulong OriginTimestampRaw { get; set; }
    public DateTime OriginTimestamp
    {
        get => RawTimestampToDateTime(OriginTimestampRaw, DateTime.UtcNow); 
        set => OriginTimestampRaw = RawTimestampFromDateTime(value);
    }

    public ulong ReceiveTimestampRaw { get; set; }
    public DateTime ReceiveTimestamp
    {
        get => RawTimestampToDateTime(ReceiveTimestampRaw, DateTime.UtcNow); 
        set => ReceiveTimestampRaw = RawTimestampFromDateTime(value);
    }

    public ulong TransmitTimestampRaw { get; set; }
    public DateTime TransmitTimestamp
    {
        get => RawTimestampToDateTime(TransmitTimestampRaw, DateTime.UtcNow); 
        set => TransmitTimestampRaw = RawTimestampFromDateTime(value);
    }

    public void WriteTo(Span<byte> buffer)
    {
        AssertSize(buffer, nameof(buffer));

        buffer[0] = (byte)(
            (((byte)LeapIndicator & 0x3) << 6) |
            (((byte)Version & 0x7) << 3) |
            ((byte)Mode & 0x7)
        );

        buffer[1] = Stratum;
        buffer[2] = (byte)Poll;
        buffer[3] = (byte)Precision;

        BinaryPrimitives.WriteUInt32BigEndian(buffer[4..], RootDelay);
        BinaryPrimitives.WriteUInt32BigEndian(buffer[8..], RootDispersion);
        for (var i = 0; i < NtpReferenceId.Length; i++)
            buffer[12 + i] = ReferenceId[i];

        BinaryPrimitives.WriteUInt64BigEndian(buffer[16..], ReferenceTimestampRaw);
        BinaryPrimitives.WriteUInt64BigEndian(buffer[24..], OriginTimestampRaw);
        BinaryPrimitives.WriteUInt64BigEndian(buffer[32..], ReceiveTimestampRaw);
        BinaryPrimitives.WriteUInt64BigEndian(buffer[40..], TransmitTimestampRaw);
    }   

    public static NtpPacket ReadFrom(ReadOnlySpan<byte> data)
    {
        AssertSize(data, nameof(data));

        return new NtpPacket
        {
            LeapIndicator = (NtpLeapIndicator)((data[0] >> 6) & 0x3),
            Version = (NtpVersion)((data[0] >> 3) & 0x7),
            Mode = (NtpMode)(data[0] & 0x7),

            Stratum = data[1],
            Poll = (sbyte)data[2],
            Precision = (sbyte)data[3],

            RootDelay = BinaryPrimitives.ReadUInt32BigEndian(data[4..]),
            RootDispersion = BinaryPrimitives.ReadUInt32BigEndian(data[8..]),
            ReferenceId = new NtpReferenceId(data[12..16]),

            ReferenceTimestampRaw = BinaryPrimitives.ReadUInt64BigEndian(data[16..]),
            OriginTimestampRaw = BinaryPrimitives.ReadUInt64BigEndian(data[24..]),
            ReceiveTimestampRaw = BinaryPrimitives.ReadUInt64BigEndian(data[32..]),
            TransmitTimestampRaw = BinaryPrimitives.ReadUInt64BigEndian(data[40..])
        };
    }

    public DateTime CalculateSynchronizedTime(DateTime destinationTimestamp)
    {
        AssertDateTimeKind(destinationTimestamp, nameof(destinationTimestamp));

        var t1 = OriginTimestamp;
        var t2 = ReceiveTimestamp;
        var t3 = TransmitTimestamp;
        var t4 = destinationTimestamp;

        var offset = TimeSpan.FromTicks(
            ((t2 - t1).Ticks + (t3 - t4).Ticks) / 2
        );

        return destinationTimestamp.Add(offset);
    }

    public TimeSpan CalculateRoundTripDelay(DateTime destinationTimestamp)
    {
        AssertDateTimeKind(destinationTimestamp, nameof(destinationTimestamp));

        var t1 = OriginTimestamp;
        var t2 = ReceiveTimestamp;
        var t3 = TransmitTimestamp;
        var t4 = destinationTimestamp;

        return TimeSpan.FromTicks((t4 - t1).Ticks - (t3 - t2).Ticks);
    }

    public static ulong RawTimestampFromDateTime(DateTime dt)
    {
        AssertDateTimeKind(dt, nameof(dt));

        var totalSeconds = (dt - NtpConstants.NtpEpoch).TotalSeconds;
        if (totalSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(dt), "The DateTime must be on or after January 1, 1900 UTC.");

        var fullSeconds = (ulong)Math.Floor(totalSeconds);
        var wireSeconds = fullSeconds % NtpConstants.EraSeconds;

        var frac = (uint)((totalSeconds - Math.Floor(totalSeconds)) * NtpConstants.FractionScale);

        return (wireSeconds << 32) | frac;
    }

    public static DateTime RawTimestampToDateTime(ulong timestamp, DateTime reference)
    {
        AssertDateTimeKind(reference, nameof(reference));

        var seconds = (uint)(timestamp >> 32);
        var fraction = (uint)timestamp;

        var fracSeconds = fraction / NtpConstants.FractionScale;

        var refSeconds = (ulong)(reference - NtpConstants.NtpEpoch).TotalSeconds;

        var refEra = refSeconds / NtpConstants.EraSeconds;

        var best = NtpConstants.NtpEpoch;
        var bestDiff = double.MaxValue;
        for (var era = refEra > 0 ? refEra - 1 : 0; era <= refEra + 1; era++)
        {
            var fullSeconds = era * NtpConstants.EraSeconds + seconds;

            var candidate = NtpConstants.NtpEpoch
                .AddSeconds(fullSeconds)
                .AddSeconds(fracSeconds);

            var diff = Math.Abs((candidate - reference).TotalSeconds);

            if (diff < bestDiff)
            {
                bestDiff = diff;
                best = candidate;
            }
        }

        return best;
    }

    static void AssertSize(ReadOnlySpan<byte> data, string paramName)
    {
        if (data.Length < NtpConstants.PacketSize)
            throw new ArgumentException("Invalid NTP packet length.", paramName);
    }

    static void AssertDateTimeKind(DateTime dt, string paramName)
    {
        if (dt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("The DateTime must be in UTC.", paramName);
    }
}
