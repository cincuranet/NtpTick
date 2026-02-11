using NUnit.Framework;

namespace NtpTick.Tests;

[TestFixture]
public class EndToEndTests
{
    SimpleNtpClient _client;

    [OneTimeSetUp]
    public void Setup()
    {
        _client = new SimpleNtpClient("time.cincura.net");
    }

    [Test]
    public async Task Send_ReturnsSensiblePacket()
    {
        var response = await _client.Send(new NtpPacket());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Version, Is.EqualTo(NtpVersion.Version4));
            Assert.That(response.Mode, Is.EqualTo(NtpMode.Server));
            Assert.That(response.Stratum, Is.GreaterThan(0).And.LessThanOrEqualTo(2));
            Assert.That(response.Poll, Is.GreaterThan(-30));
            Assert.That(response.Precision, Is.GreaterThan(-30));
            Assert.That(response.RootDelay, Is.GreaterThan(0));
            Assert.That(response.RootDispersion, Is.GreaterThan(0));
            Assert.That(response.ReferenceTimestamp, Is.Not.Default);
            Assert.That(response.ReceiveTimestamp, Is.Not.Default);
            Assert.That(response.TransmitTimestamp, Is.Not.Default);
        }
    }

    [Test]
    public async Task GetTime_ReturnsSensibleTime()
    {
        var time = await _client.GetTime();
        Assert.That(time, Is.EqualTo(DateTimeOffset.UtcNow).Within(TimeSpan.FromSeconds(5)));
    }
}
