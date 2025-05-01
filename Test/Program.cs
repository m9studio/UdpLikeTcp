using System.Net;

namespace M9Studio.UdpLikeTcp.Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Socket socket1 = new Socket();
            Socket socket2 = new Socket();
            IPEndPoint ip1 = new IPEndPoint(IPAddress.Loopback, socket1.Port);
            IPEndPoint ip2 = new IPEndPoint(IPAddress.Loopback, socket2.Port);
            for(int i = 0; i < 5; i++)
            {
                byte[] data = GenerateRandomBytes(1200000);
                if (socket2.SendTo(ip1, data))
                {
                    byte[] buffer;
                    while (!socket1.ReceiveFrom(ip2, out buffer))
                    {
                        Console.Write(0);
                    }
                    Console.WriteLine(Check(data, buffer));
                }
                else
                {
                    Console.WriteLine(1);
                }
            }
        }


        public static byte[] GenerateRandomBytes(int length = 64)
        {
            byte[] bytes = new byte[length];
            Random random = new Random();
            random.NextBytes(bytes);
            return bytes;
        }

        static bool Check(byte[] buffer1, byte[] buffer2)
        {
            if (buffer1.Length != buffer2.Length)
            {
                return false;
            }
            for (int i = 0; i < buffer1.Length; i++)
            {
                if (buffer1[i] != buffer2[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
