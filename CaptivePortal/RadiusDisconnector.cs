using Radius;
using System.Net.Sockets;
using System.Net;
using System.Text;
using CaptivePortal.Database;
using CaptivePortal.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Radius.RadiusAttributes;

namespace CaptivePortal
{
    public class RadiusDisconnector
    {
        private UdpClient udpClient = new();
        private byte[] secret = Encoding.ASCII.GetBytes("thesecret");
        private byte lastSentIdentifier = 0;

        private readonly CaptivePortalDbContext db;

        public RadiusDisconnector(
            CaptivePortalDbContext db)
        {
            this.db = db;
        }

        public async Task<bool> Disconnect(string mac, CancellationToken cancellationToken = default)
        {
            Device? device = await db.Devices
                .AsNoTracking()
                .Where(x => x.DeviceMac == mac)
                .FirstOrDefaultAsync(cancellationToken);

            if (device is null ||
                device.CallingStationId is null ||
                device.NasIpAddress is null ||
                device.NasIdentifier is null ||
                device.AccountingSessionId is null)
            {
                return false;
            }

            if (!IPAddress.TryParse(device.NasIpAddress, out IPAddress? nasIpAddress))
            {
                return false;
            }

            RadiusPacket disconnect = RadiusPacket.Create(
                RadiusCode.DISCONNECT_REQUEST,
                lastSentIdentifier++,
                null)
                .AddAttribute(new CallingStationIdAttribute(device.CallingStationId))
                .AddAttribute(new NasIpAddressAttribute(nasIpAddress))
                .AddAttribute(new NasIdentifierAttribute(device.NasIdentifier))
                .AddAttribute(new AccountingSessionIdAttribute(device.AccountingSessionId));

            disconnect.ReplaceAuthenticator(disconnect.CalculateAuthenticator(secret));

            await udpClient.SendAsync(
                disconnect.ToBytes(), 
                new IPEndPoint(nasIpAddress, 3799));

            return true;
        }
    }
}
