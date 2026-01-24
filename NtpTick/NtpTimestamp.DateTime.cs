namespace NtpTick;

partial struct NtpTimestamp
{
    public static readonly DateTime NtpEpoch = new(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public NtpTimestamp(DateTime dt)
    {
        AssertDateTimeKind(dt, nameof(dt));

        var totalSeconds = (dt - NtpEpoch).TotalSeconds;
        if (totalSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(dt), "The DateTime must be on or after January 1, 1900 UTC.");

        var fullSeconds = (ulong)Math.Floor(totalSeconds);
        var wireSeconds = fullSeconds % EraSeconds;

        var frac = (uint)((totalSeconds - Math.Floor(totalSeconds)) * FractionScale);

        _value = (wireSeconds << 32) | frac;
    }

    public readonly DateTime ToDateTime(DateTime reference)
    {
        AssertDateTimeKind(reference, nameof(reference));

        var seconds = (uint)(_value >> 32);
        var fraction = (uint)_value;

        var fracSeconds = fraction / FractionScale;

        var refSeconds = (ulong)(reference - NtpEpoch).TotalSeconds;

        var refEra = refSeconds / EraSeconds;

        var best = NtpEpoch;
        var bestDiff = double.MaxValue;
        for (var era = refEra > 0 ? refEra - 1 : 0; era <= refEra + 1; era++)
        {
            var fullSeconds = era * EraSeconds + seconds;

            var candidate = NtpEpoch
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

    internal static void AssertDateTimeKind(DateTime dt, string paramName)
    {
        if (dt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("The DateTime must be in UTC.", paramName);
    }
}
