using System.Net;
using System.Net.Sockets;

namespace M9Studio.UdpLikeTcp.Test
{
    internal partial class Program
    {
        protected static void Test2()
        {
            Console.WriteLine("Test2:");
            Socket server = new Socket();
            Socket client1 = new Socket();
            Socket client2 = new Socket();

            IPEndPoint ipServer = new IPEndPoint(IPAddress.Loopback, server.Port);
            IPEndPoint ipClient1 = new IPEndPoint(IPAddress.Loopback, client1.Port);
            IPEndPoint ipClient2 = new IPEndPoint(IPAddress.Loopback, client2.Port);

            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine("Send: " + i);
                byte[] data1 = GenerateRandomBytes(12000);
                byte[] data2 = GenerateRandomBytes(12000);
                Console.WriteLine("client1: " + client1.SendTo(ipServer, data1));
                Console.WriteLine("client2: " + client2.SendTo(ipServer, data2));
                byte[]? buffer = null;
                if(server.ReceiveFrom(ipClient1, out buffer))
                {
                    Console.WriteLine("check1: " + Check(buffer, data1));
                }
                if (server.ReceiveFrom(ipClient2, out buffer))
                {
                    Console.WriteLine("check2: " + Check(buffer, data2));
                }

            }
        }
    }
}
