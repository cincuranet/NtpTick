using System.Net;
using System.Net.Sockets;

namespace NtpTick;

public class SimpleNtpClient
{
    readonly IPEndPoint _remoteEP;

    public SimpleNtpClient(IPEndPoint remoteEP)
    {
        _remoteEP = remoteEP;
    }

    public async Task<NtpPacket> Send(NtpPacket packet, CancellationToken cancellationToken = default)
    {
        using (var socket = new Socket(_remoteEP.AddressFamily, SocketType.Dgram, ProtocolType.Udp))
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

    public async Task<DateTimeOffset> GetTime(CancellationToken cancellationToken = default)
    {
        var response = await Send(new NtpPacket { TransmitTimestamp = DateTimeOffset.UtcNow }, cancellationToken);
        return response.CalculateSynchronizedTime(DateTimeOffset.UtcNow);
    }
}
