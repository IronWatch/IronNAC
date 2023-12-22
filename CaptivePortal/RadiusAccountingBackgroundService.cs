
using System.Net.Sockets;
using System.Net;
using System.Text;
using Radius;
using Radius.RadiusAttributes;
using CaptivePortal.Data;
using System.Collections.Concurrent;

namespace CaptivePortal
{
    public class RadiusAccountingBackgroundService : BackgroundService
    {
        private UdpClient udpClient = new(new IPEndPoint(IPAddress.Any, 1813));
        private byte[] secret = Encoding.ASCII.GetBytes("thesecret");
        private byte lastSeenIdentifier = 0;

        private readonly RadiusAttributeParser parser;
        private readonly DatabaseService databaseService;

        public RadiusAccountingBackgroundService(
            RadiusAttributeParserService parserService, 
            DatabaseService databaseService)
        {
            parser = parserService.Parser;
            this.databaseService = databaseService;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                UdpReceiveResult udpReceiveResult = await udpClient.ReceiveAsync(cancellationToken);
                if (cancellationToken.IsCancellationRequested) break;

                RadiusPacket? incoming = null;
                try
                {
                    incoming = RadiusPacket.FromBytes(udpReceiveResult.Buffer, parser);
                }
                catch (RadiusException radEx)
                {
                    Console.WriteLine(radEx.Message);
                    continue;
                }

                lastSeenIdentifier = incoming.Identifier;

                switch (incoming.Code)
                {
                    case RadiusCode.ACCOUNTING_REQUEST:

                        // We got an accounting message, acknowledge it
                        await udpClient.SendAsync(RadiusPacket
                            .Create(RadiusCode.ACCOUNTING_RESPONSE, incoming.Identifier)
                            .AddMessageAuthenticator(secret)
                            .AddResponseAuthenticator(secret, incoming.Authenticator)
                            .ToBytes(),
                            udpReceiveResult.RemoteEndPoint,
                            cancellationToken);

                        string? mac = incoming.GetAttribute<UserNameAttribute>()?.Value;
                        if (mac is null) break;

                        AccountingStatusTypeAttribute.StatusTypes? statusType = incoming.GetAttribute<AccountingStatusTypeAttribute>()?.StatusType;
                        if (statusType is null) break;

                        if (statusType == AccountingStatusTypeAttribute.StatusTypes.STOP)
                        {
                            databaseService.Sessions.TryRemove(mac, out _);
                            break;
                        }

                        if (statusType == AccountingStatusTypeAttribute.StatusTypes.START)
                        {
                            databaseService.Sessions.TryAdd(mac, new Session()
                            {
                                FramedIpAddress = incoming.GetAttribute<FramedIpAddressAttribute>(), // usually not here
                                NasIpAddress = incoming.GetAttribute<NasIpAddressAttribute>(),
                                NasIdentifier = incoming.GetAttribute<NasIdentifierAttribute>(),
                                CallingStationId = incoming.GetAttribute<CallingStationIdAttribute>(),
                                AccountingSessionId = incoming.GetAttribute<AccountingSessionIdAttribute>()
                            });
                        }

                        if (statusType == AccountingStatusTypeAttribute.StatusTypes.INTERIM_UPDATE &&
                            databaseService.Sessions.ContainsKey(mac))
                        {
                            databaseService.Sessions[mac].FramedIpAddress = incoming.GetAttribute<FramedIpAddressAttribute>();
                            databaseService.Sessions[mac].NasIpAddress = incoming.GetAttribute<NasIpAddressAttribute>();
                            databaseService.Sessions[mac].NasIdentifier = incoming.GetAttribute<NasIdentifierAttribute>();
                            databaseService.Sessions[mac].CallingStationId = incoming.GetAttribute<CallingStationIdAttribute>();
                            databaseService.Sessions[mac].AccountingSessionId = incoming.GetAttribute<AccountingSessionIdAttribute>();
                        }

                        break;

                    default:
                        break;
                }
            }
        }
    }
}
