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
    
    public NtpTimestamp ReferenceTimestamp { get; set; }
    public NtpTimestamp OriginTimestamp { get; set; }
    public NtpTimestamp ReceiveTimestamp { get; set; }
    public NtpTimestamp TransmitTimestamp { get; set; }

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
        ReferenceId.CopyTo(buffer[12..]);

        BinaryPrimitives.WriteUInt64BigEndian(buffer[16..], ReferenceTimestamp);
        BinaryPrimitives.WriteUInt64BigEndian(buffer[24..], OriginTimestamp);
        BinaryPrimitives.WriteUInt64BigEndian(buffer[32..], ReceiveTimestamp);
        BinaryPrimitives.WriteUInt64BigEndian(buffer[40..], TransmitTimestamp);
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
            ReferenceId = new NtpReferenceId(data[12..]),

            ReferenceTimestamp = BinaryPrimitives.ReadUInt64BigEndian(data[16..]),
            OriginTimestamp = BinaryPrimitives.ReadUInt64BigEndian(data[24..]),
            ReceiveTimestamp = BinaryPrimitives.ReadUInt64BigEndian(data[32..]),
            TransmitTimestamp = BinaryPrimitives.ReadUInt64BigEndian(data[40..])
        };
    }

    /// <summary>
    /// Calculates the estimated time at the destination adjusted for network delays.
    /// </summary>
    public DateTimeOffset CalculateSynchronizedTime(DateTimeOffset destinationTimestamp, DateTimeOffset? referenceTimestamp = null)
    {
        referenceTimestamp ??= DateTimeOffset.UtcNow;
        var t1 = OriginTimestamp.ToDateTimeOffset((DateTimeOffset)referenceTimestamp);
        var t2 = ReceiveTimestamp.ToDateTimeOffset((DateTimeOffset)referenceTimestamp);
        var t3 = TransmitTimestamp.ToDateTimeOffset((DateTimeOffset)referenceTimestamp);
        var t4 = destinationTimestamp;

        var offset = ((t2 - t1) + (t3 - t4)) / 2;

        return destinationTimestamp.Add(offset);
    }

    /// <summary>
    /// Calculates the estimated round-trip network delay.
    /// </summary>
    public TimeSpan CalculateRoundTripDelay(DateTimeOffset destinationTimestamp, DateTimeOffset? referenceTimestamp = null)
    {
        referenceTimestamp ??= DateTimeOffset.UtcNow;
        var t1 = OriginTimestamp.ToDateTimeOffset((DateTimeOffset)referenceTimestamp);
        var t2 = ReceiveTimestamp.ToDateTimeOffset((DateTimeOffset)referenceTimestamp);
        var t3 = TransmitTimestamp.ToDateTimeOffset((DateTimeOffset)referenceTimestamp);
        var t4 = destinationTimestamp;

        return (t4 - t1) - (t3 - t2);
    }

    static void AssertSize(ReadOnlySpan<byte> data, string paramName)
    {
        if (data.Length < NtpConstants.PacketSize)
            throw new ArgumentException("Invalid NTP packet length.", paramName);
    }
}
