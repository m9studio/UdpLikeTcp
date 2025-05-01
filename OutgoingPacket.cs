namespace M9Studio.UdpLikeTcp
{
    internal class OutgoingPacket
    {
        public byte[] Data;
        public TaskCompletionSource<bool> Completion;
    }
}
