namespace M9Studio.UdpLikeTcp
{
    //Буфер для сборки фрагментов
    internal class FragmentBuffer
    {
        public int PacketId;
        public int TotalSize;
        public Dictionary<ushort, byte[]> Fragments = new();
        public DateTime LastUpdated = DateTime.UtcNow;
        public byte[] LastAckHeader = Array.Empty<byte>();

        public bool IsComplete => Fragments.Values.Sum(x => x.Length) >= TotalSize;

        public byte[] Assemble()
        {
            byte[] result = new byte[TotalSize];
            int offset = 0;
            foreach (var pair in Fragments.OrderBy(x => x.Key))
            {
                Array.Copy(pair.Value, 0, result, offset, pair.Value.Length);
                offset += pair.Value.Length;
            }

            return result;
        }
    }
}
