namespace NtpTick;

public static class NtpConstants
{
    public const int PacketSize = 48;

    public const byte MinVersion = 3;
    public const byte MaxVersion = 4;

    public const byte MaxStratum = 15;

    public static readonly DateTime NtpEpoch = new(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public const ulong EraSeconds = 1UL << 32;
    public const double FractionScale = uint.MaxValue;
}
