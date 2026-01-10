namespace NtpTick;

public enum NtpMode : byte
{
    Reserved = 0,
    SymmetricActive = 1,
    SymmetricPassive = 2,
    Client = 3,
    Server = 4,
    Broadcast = 5,
    NtpControlMessage = 6,
    PrivateUse = 7
}
