﻿using System;
using System.Net;
using System.Net.Sockets;

namespace M9Studio.UdpLikeTcp.Test
{
    internal partial class Program
    {
        protected static void Test3Client()
        {
            Socket client = new Socket();
            Console.WriteLine("My Port: " + client.Port);

            client.OnReceived += (ip) =>
            {
                while (true)
                {
                    Console.WriteLine("Get packet: " + ip.ToString() + " " + BytesToHex(client.ReceiveFrom(ip)));
                }
            };
            Console.ReadLine();
        }
        protected static void Test3Server()
        {
            Socket server = new Socket();
            IPEndPoint ipEndPoint = Test3IPEndPoint(server);
            byte[] buffer = GenerateRandomBytes(64);
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
