using System.Net;
using System.Net.Sockets;

namespace NtpTick;

/// <summary>
/// Provides a simple client for communicating with a NTP server
/// to retrieve time synchronization data.
/// </summary>
public class SimpleNtpClient
{
    public const int DefaultPort = 123;

    readonly EndPoint _remoteEP;

    public SimpleNtpClient(EndPoint remoteEP)
    {
        _remoteEP = remoteEP;
    }

    public SimpleNtpClient(string host, int port = DefaultPort)
        : this(new DnsEndPoint(host, port))
    { }

    /// <summary>
    /// Sends an NTP packet and waits for the response. The returned packet contains the timestamps from the response,
    /// but the caller is responsible for calculating the synchronized time using the timestamps and local clock.
    /// </summary>
    public async Task<NtpPacket> Send(NtpPacket packet, CancellationToken cancellationToken = default)
    {
        using (var socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
        {
            await socket.ConnectAsync(_remoteEP, cancellationToken);

            var buffer = new byte[NtpConstants.PacketSize];

            packet.WriteTo(buffer);
            var sent = await socket.SendAsync(buffer, cancellationToken);
            if (sent < buffer.Length)
                throw new IOException("Failed to send NTP packet.");

            var received = await socket.ReceiveAsync(buffer, cancellationToken);
            if (received < buffer.Length)
                throw new IOException("Failed to receive NTP packet.");
            return NtpPacket.ReadFrom(buffer);
        }
    }


    /// <summary>
    /// Sends an NTP packet and from response calculates what the local time should be at the moment the response was received.
    /// </summary>
    public async Task<DateTimeOffset> GetTime(CancellationToken cancellationToken = default)
    {
        var response = await Send(new NtpPacket { TransmitTimestamp = DateTimeOffset.UtcNow }, cancellationToken);
        return response.CalculateSynchronizedTime(DateTimeOffset.UtcNow);
    }
}
