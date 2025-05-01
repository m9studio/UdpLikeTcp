using System.Net;

namespace M9Studio.UdpLikeTcp.Test
{
    internal partial class Program
    {
        protected static void Test1()
        {
            Console.WriteLine("Test1:");

            Socket socket1 = new Socket();
            Socket socket2 = new Socket();
            IPEndPoint ip1 = new IPEndPoint(IPAddress.Loopback, socket1.Port);
            IPEndPoint ip2 = new IPEndPoint(IPAddress.Loopback, socket2.Port);

            byte[]? data1 = GenerateRandomBytes(64);
            byte[]? data2 = GenerateRandomBytes(65507 - 12);
            byte[]? data3 = GenerateRandomBytes(65507);
            byte[]? data4 = GenerateRandomBytes(24000);
            byte[]? data5 = GenerateRandomBytes(1200000);
            byte[]? buffer = null;
            Console.WriteLine("Send:");

            if (!socket2.SendTo(ip1, data1))
            {
                data1 = null;
            }
            Console.WriteLine("1: " + (data1 != null));

            if (!socket2.SendTo(ip1, data2))
            {
                data2 = null;
            }
            Console.WriteLine("2: " + (data2 != null));

            if (!socket2.SendTo(ip1, data3))
            {
                data3 = null;
            }
            Console.WriteLine("3: " + (data3 != null));

            if (!socket2.SendTo(ip1, data4))
            {
                data4 = null;
            }
            Console.WriteLine("4: " + (data4 != null));

            if (!socket2.SendTo(ip1, data5))
            {
                data5 = null;
            }
            Console.WriteLine("5: " + (data5 != null));

            Console.WriteLine("Check:");
            if (data1 != null && socket1.ReceiveFrom(ip2, out buffer))
            {
                Console.WriteLine("1: " + Check(buffer, data1));
            }
            if (data2 != null && socket1.ReceiveFrom(ip2, out buffer))
            {
                Console.WriteLine("2: " + Check(buffer, data2));
            }
            if (data3 != null && socket1.ReceiveFrom(ip2, out buffer))
            {
                Console.WriteLine("3: " + Check(buffer, data3));
            }
            if (data4 != null && socket1.ReceiveFrom(ip2, out buffer))
            {
                Console.WriteLine("4: " + Check(buffer, data4));
            }
            if (data5 != null && socket1.ReceiveFrom(ip2, out buffer))
            {
                Console.WriteLine("5: " + Check(buffer, data5));
            }
        }
    }
}
