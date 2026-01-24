using System.Runtime.CompilerServices;

namespace NtpTick;

[InlineArray(Length)]
public struct NtpReferenceId
{
    public const int Length = 4;

    public NtpReferenceId(ReadOnlySpan<byte> data)
    {
        if (data.Length < Length)
            throw new ArgumentException($"Data length must be at least {Length}.", nameof(data));

        this[0] = data[0];
        this[1] = data[1];
        this[2] = data[2];
        this[3] = data[3];
    }

    public readonly void CopyTo(Span<byte> destination)
    {
        if (destination.Length < Length)
            throw new ArgumentException($"Destination length must be at least {Length}.", nameof(destination));

        destination[0] = this[0];
        destination[1] = this[1];
        destination[2] = this[2];
        destination[3] = this[3];
    }

    byte _element0;
}
