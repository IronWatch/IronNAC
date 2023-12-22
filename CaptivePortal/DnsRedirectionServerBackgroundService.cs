
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
        // On Windows if WSL2 or Hyper-V are running. ICS will lock port 53 on the Any address for itself. Must use Loopback instead for dev.
        private UdpClient udpClient = new(new IPEndPoint(IPAddress.Parse("10.100.0.100"), 53));

        public DnsRedirectionServerBackgroundService() {}

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
                        response.Answers.Add(new ARecord(request.Questions[0].Name, IPAddress.Parse("10.100.0.100"), 60));
                    }

                    await udpClient.SendAsync(response.ToBytes(), udpReceiveResult.RemoteEndPoint, cancellationToken);
                }
                catch (SocketException) { }
            }
        }
    }
}
