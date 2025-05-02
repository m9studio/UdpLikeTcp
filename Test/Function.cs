using System.Diagnostics;

namespace M9Studio.UdpLikeTcp.Test
{
    internal partial class Program
    {
        public static byte[] GenerateRandomBytes(int length = 64)
        {
            byte[] bytes = new byte[length];
            Random random = new Random();
            random.NextBytes(bytes);
            return bytes;
        }

        public static bool Check(byte[] buffer1, byte[] buffer2)
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

        public static string BytesToHex(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }


        public static void AddUdpRule(int port)
        {
            Run("netsh", $"advfirewall firewall add rule name=\"{RuleName}\" dir=in action=allow protocol=UDP localport={port}");
        }

        public static void RemoveUdpRule()
        {
            Run("netsh", $"advfirewall firewall delete rule name=\"{RuleName}\" protocol=UDP");
        }

        private static void Run(string file, string args)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = file,
                Arguments = args,
                Verb = "runas", // запустить от имени администратора
                CreateNoWindow = true,
                UseShellExecute = true
            };

            try
            {
                using var proc = Process.Start(psi);
                proc.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Не удалось изменить правило брандмауэра: {ex.Message}");
            }
        }
    }
}
