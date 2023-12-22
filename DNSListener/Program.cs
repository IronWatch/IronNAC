using System.Net.Sockets;
using System.Net;
using DNSListener.DNS;
using DNSListener.DNS.ResourceRecords;

// On Windows if WSL2 or Hyper-V are running. ICS will lock port 53 on the Any address for itself. Must use Loopback instead for dev.
UdpClient udpClient = new(new IPEndPoint(IPAddress.Parse("10.100.0.100"), 53));
IPEndPoint remoteEndpoint = new(IPAddress.Any, 0);

try
{
    while(true)
    {
        byte[] bytes = udpClient.Receive(ref remoteEndpoint);

        DnsPacket request = DnsPacket.FromBytes(bytes);

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

        udpClient.Send(response.ToBytes(), remoteEndpoint);
    }
}
catch (SocketException sockEx)
{
    Console.WriteLine(sockEx.Message);
    Console.ReadLine();
}
finally
{
    udpClient.Close();
}