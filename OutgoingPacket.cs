using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M9Studio.UdpLikeTcp
{
    internal class OutgoingPacket
    {
        public byte[] Data;
        public TaskCompletionSource<bool> Completion;
    }
}
