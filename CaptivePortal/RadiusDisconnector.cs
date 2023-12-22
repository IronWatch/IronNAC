using Radius;
using System.Net.Sockets;
using System.Net;
using System.Text;
using CaptivePortal.Data;

namespace CaptivePortal
{
    public class RadiusDisconnector
    {
        private UdpClient udpClient = new();
        private byte[] secret = Encoding.ASCII.GetBytes("thesecret");
        private byte lastSentIdentifier = 0;

        private readonly DatabaseService databaseService;

        public RadiusDisconnector(
            DatabaseService databaseService)
        {
            this.databaseService = databaseService;
        }

        public async Task<bool> Disconnect(string mac)
        {
            if (!databaseService.Sessions.TryGetValue(mac, out Session? session) ||
                session is null ||
                session.CallingStationId is null ||
                session.NasIpAddress is null ||
                session.NasIdentifier is null ||
                session.AccountingSessionId is null)
            {
                return false;
            }

            RadiusPacket disconnect = RadiusPacket.Create(
                RadiusCode.DISCONNECT_REQUEST,
                lastSentIdentifier++,
                null)
                .AddAttribute(session.CallingStationId)
                .AddAttribute(session.NasIpAddress)
                .AddAttribute(session.NasIdentifier)
                .AddAttribute(session.AccountingSessionId);
            disconnect.ReplaceAuthenticator(disconnect.CalculateAuthenticator(secret));

            await udpClient.SendAsync(
                disconnect.ToBytes(), 
                new IPEndPoint(session.NasIpAddress.Address, 3799));

            return true;
        }
    }
}
