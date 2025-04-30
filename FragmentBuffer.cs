using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M9Studio.UdpLikeTcp
{
    internal class FragmentBuffer
    {
        public int PacketId;
        public int TotalSize;
        public Dictionary<ushort, byte[]> Fragments = new();
        public int ReceivedBytes => Fragments.Values.Sum(x => x.Length);
        public bool IsComplete => ReceivedBytes >= TotalSize;

        public DateTime LastUpdated = DateTime.UtcNow;
        public int RetryCount = 0;
        public byte[] LastAckHeader = Array.Empty<byte>();

        public byte[] Assemble()
        {
            var result = new byte[TotalSize];
            int offset = 0;
            foreach (var part in Fragments.OrderBy(kvp => kvp.Key))
            {
                Array.Copy(part.Value, 0, result, offset, part.Value.Length);
                offset += part.Value.Length;
            }
            return result;
        }
    }
}
