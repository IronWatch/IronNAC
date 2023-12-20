using System.Net.Sockets;
using System.Net;
using DNSListener.DNS;

// On Windows if WSL2 or Hyper-V are running. ICS will lock port 53 on the Any address for itself. Must use Loopback instead for dev.
UdpClient udpClient = new(new IPEndPoint(IPAddress.Loopback, 53));
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
        response.Flags.ReplyCode = DnsPacketFlagsReplyCodes.NAME_ERROR;
        response.NumQuestions = request.NumQuestions;
        response.Questions = request.Questions;

        udpClient.Send(response.ToBytes(), remoteEndpoint);

        Console.WriteLine();
    }
}
catch (SocketException sockEx)
{
    Console.WriteLine(sockEx.Message);
}
finally
{
    udpClient.Close();
}