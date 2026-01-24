using System.Diagnostics.CodeAnalysis;

namespace NtpTick;

public readonly partial struct NtpTimestamp : IEquatable<NtpTimestamp>, IComparable<NtpTimestamp>
{
    public const ulong EraSeconds = 1UL << 32;
    public const double FractionScale = uint.MaxValue;

    readonly ulong _value;

    public NtpTimestamp(ulong value)
    {
        _value = value;
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is NtpTimestamp other && Equals(other);

    public override int GetHashCode()
        => _value.GetHashCode();

    public bool Equals(NtpTimestamp other)
        => _value == other._value;

    public int CompareTo(NtpTimestamp other)
        => _value.CompareTo(other._value);

    public static implicit operator ulong(NtpTimestamp timestamp)
        => timestamp._value;

    public static implicit operator NtpTimestamp(ulong value)
        => new(value);

    public static bool operator ==(NtpTimestamp lhs, NtpTimestamp rhs) 
        => lhs.Equals(rhs);

    public static bool operator !=(NtpTimestamp lhs, NtpTimestamp rhs)
        => !lhs.Equals(rhs);

    public static bool operator <(NtpTimestamp lhs, NtpTimestamp rhs)
        => lhs.CompareTo(rhs) < 0;

    public static bool operator <=(NtpTimestamp lhs, NtpTimestamp rhs) 
        => lhs.CompareTo(rhs) <= 0;

    public static bool operator >(NtpTimestamp lhs, NtpTimestamp rhs) 
        => lhs.CompareTo(rhs) > 0;

    public static bool operator >=(NtpTimestamp lhs, NtpTimestamp rhs) 
        => lhs.CompareTo(rhs) >= 0;
}
