using System.Runtime.CompilerServices;

namespace NtpTick;

[InlineArray(Length)]
public struct NtpReferenceId
{
    public const int Length = 4;

    public NtpReferenceId(ReadOnlySpan<byte> data)
    {
        if (data.Length != Length)
            throw new ArgumentException($"Data length must be exactly {Length}.", nameof(data));
        
        this[0] = data[0];
        this[1] = data[1];
        this[2] = data[2];
        this[3] = data[3];
    }

    byte _element0;
}
