namespace NtpTick;

partial struct NtpTimestamp
{
    public static readonly DateTimeOffset NtpEpoch = new(1900, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public NtpTimestamp(DateTimeOffset dto)
        : this(FromDateTimeOffset(dto))
    { }

    public static implicit operator NtpTimestamp(DateTimeOffset dto)
        => new(dto);

    public DateTimeOffset ToDateTimeOffset(DateTimeOffset reference)
    {
        var totalSeconds = (reference - NtpEpoch).TotalSeconds;
        if (totalSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(reference), "The value must be on or after January 1, 1900 UTC.");

        var referenceSeconds = (ulong)totalSeconds;
        var seconds = (uint)(_value >> 32);
        var baseSeconds = (referenceSeconds & 0xFFFFFFFF00000000UL) | seconds;

        if (baseSeconds > referenceSeconds + HalfEraSeconds)
        {
            if (baseSeconds >= EraSeconds)
                baseSeconds -= EraSeconds;
        }
        else if (baseSeconds + HalfEraSeconds < referenceSeconds)
        {
            baseSeconds += EraSeconds;
        }

        var fraction = (uint)_value;
        var fractionSeconds = fraction / FractionScale;

        return NtpEpoch.AddSeconds(baseSeconds + fractionSeconds);
    }

    static ulong FromDateTimeOffset(DateTimeOffset dto)
    {
        var totalSeconds = (dto - NtpEpoch).TotalSeconds;
        if (totalSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(dto), "The value must be on or after January 1, 1900 UTC.");

        var fullSeconds = (ulong)totalSeconds;
        var seconds = (uint)fullSeconds % EraSeconds;
        var fraction = (uint)Math.Round((totalSeconds - fullSeconds) * FractionScale);
        return (seconds << 32) | fraction;
    }
}