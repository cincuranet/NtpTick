namespace NtpTick;

public enum NtpLeapIndicator : byte
{
    NoWarning = 0,
    LastMinute61Seconds = 1,
    LastMinute59Seconds = 2,
    AlarmCondition = 3
}
