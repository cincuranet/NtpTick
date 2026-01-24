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
        var seconds = (uint)(_value >> 32);
        var fraction = (uint)_value;

        var fractionSeconds = fraction / FractionScale;
        var referenceSeconds = (ulong)(reference - NtpEpoch).TotalSeconds;
        var referenceEra = referenceSeconds / EraSeconds;

        var best = NtpEpoch;
        var bestDiff = double.MaxValue;
        for (var era = referenceEra > 0 ? referenceEra - 1 : 0; era <= referenceEra + 1; era++)
        {
            var fullSeconds = era * EraSeconds + seconds;

            var candidate = NtpEpoch
                .AddSeconds(fullSeconds)
                .AddSeconds(fractionSeconds);

            var diff = Math.Abs((candidate - reference).TotalSeconds);

            if (diff < bestDiff)
            {
                bestDiff = diff;
                best = candidate;
            }
        }
        return best;
    }

    static ulong FromDateTimeOffset(DateTimeOffset dto)
    {
        var totalSeconds = (dto - NtpEpoch).TotalSeconds;
        if (totalSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(dto), "The DateTimeOffset must be on or after January 1, 1900 UTC.");

        var fullSeconds = (ulong)Math.Floor(totalSeconds);
        var seconds = (uint)fullSeconds % EraSeconds;
        var fraction = (uint)((totalSeconds - Math.Floor(totalSeconds)) * FractionScale);
        return (seconds << 32) | fraction;
    }
}
