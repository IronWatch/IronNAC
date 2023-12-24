
using System.Net.Sockets;
using System.Net;
using System.Text;
using Radius;
using Radius.RadiusAttributes;
using DNS.ResourceRecords;
using DNS;

namespace CaptivePortal
{
    public class DnsRedirectionServerBackgroundService : BackgroundService
    {
        private UdpClient udpClient;
        private IPAddress redirectAddress;

        public DnsRedirectionServerBackgroundService(IConfiguration configuration)
        {
            string listenAddress = configuration.GetValue<string>("DnsRedirector:ListenAddress")
                ?? throw new MissingFieldException("DnsRedirector:ListenAddress");

            string redirectAddressString = configuration.GetValue<string>("DnsRedirector:RedirectAddress")
                ?? throw new MissingFieldException("DnsRedirector:RedirectAddress");
            redirectAddress = IPAddress.Parse(redirectAddressString);

            udpClient = new(new IPEndPoint(IPAddress.Parse(listenAddress), 53));
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    UdpReceiveResult udpReceiveResult = await udpClient.ReceiveAsync(cancellationToken);
                    if (cancellationToken.IsCancellationRequested) break;

                    DnsPacket request = DnsPacket.FromBytes(udpReceiveResult.Buffer);

                    DnsPacket response = new DnsPacket();
                    response.TransactionId = request.TransactionId;
                    response.Flags.IsResponse = true;
                    response.Flags.IsRecursionDesired = request.Flags.IsRecursionDesired;
                    response.Questions = request.Questions;

                    if (request.NumQuestions != 1 ||
                        request.Questions[0].Type != DnsResourceRecordTypes.A)
                    {
                        response.Flags.ReplyCode = DnsPacketFlagsReplyCodes.REFUSED;
                    }
                    else
                    {
                        response.Flags.ReplyCode = DnsPacketFlagsReplyCodes.NO_ERROR;
                        response.Answers.Add(new ARecord(request.Questions[0].Name, redirectAddress, 60));
                    }

                    await udpClient.SendAsync(response.ToBytes(), udpReceiveResult.RemoteEndPoint, cancellationToken);
                }
                catch (SocketException) { }
            }
        }
    }
}
