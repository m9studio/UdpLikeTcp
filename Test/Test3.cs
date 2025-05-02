using System;
using System.Net;
using System.Net.Sockets;

namespace M9Studio.UdpLikeTcp.Test
{
    internal partial class Program
    {
        protected static void Test3Client()
        {
            Socket client = new Socket();
            IPEndPoint ipEndPoint = Test3IPEndPoint(client);
            byte[] buffer = null;
            Console.Write("Get?");
            Console.ReadLine();
            if (client.ReceiveFrom(ipEndPoint, out buffer))
            {
                Console.WriteLine(BytesToHex(buffer));
            }
            else
            {
                Console.WriteLine("Error");
            }
        }
        protected static void Test3Server()
        {
            Socket server = new Socket();
            IPEndPoint ipEndPoint = Test3IPEndPoint(server);
            byte[] buffer = GenerateRandomBytes(12000);
            Console.Write("Send?");
            Console.ReadLine();
            if(server.SendTo(ipEndPoint, buffer))
            {
                Console.WriteLine(BytesToHex(buffer));
            }
            else
            {
                Console.WriteLine("Error");
            }
        }

        private static IPEndPoint Test3IPEndPoint(Socket socket)
        {
            Console.WriteLine("My Port: " + socket.Port);

            Console.Write("IP: ");
            string ip = Console.ReadLine();
            Console.Write("Port: ");
            string port = Console.ReadLine();

            IPAddress ipAddr = IPAddress.Parse(ip);
            int portNumber = int.Parse(port);

            IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, portNumber);

            return ipEndPoint;
        }
    }
}
