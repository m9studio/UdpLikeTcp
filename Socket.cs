using System.Net.Sockets;
using System.Net;
using OriginalSocket = System.Net.Sockets.Socket;


namespace M9Studio.UdpLikeTcp
{
    public class Socket
    {
        private int packetCounter = 0;
        private const int MaxUdpSize = 65507;
        private const int HeaderSize = 12;
        private const int MaxFragmentSize = MaxUdpSize - HeaderSize;

        private readonly OriginalSocket socket;

        private volatile byte[] lastReceived;
        public int Port => ((IPEndPoint)socket.LocalEndPoint).Port;

        private readonly Dictionary<EndPoint, FragmentBuffer> receiving = new();
        private readonly Dictionary<EndPoint, Queue<byte[]>> packetQueues = new();
        private readonly HashSet<(EndPoint, int packetId, ushort fragmentNumber)> receivedAcks = new();
        private readonly object syncLock = new();
        private readonly Dictionary<EndPoint, Queue<byte[]>> sendQueues = new();
        private readonly HashSet<EndPoint> isSending = new(); // флаг: уже отправляем?
        private readonly object sendLock = new();

        public Socket()
        {
            socket = new OriginalSocket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            EndPoint bindTo = new IPEndPoint(IPAddress.Any, 0);
            socket.Bind(bindTo);

            Task.Run(() =>
            {
                byte[] buffer = new byte[65507];
                EndPoint remote = new IPEndPoint(IPAddress.Any, 0);

                while (true)
                {
                    int received = socket.ReceiveFrom(buffer, ref remote);
                    if (received < 12)
                        continue;

                    int packetId = BitConverter.ToInt32(buffer, 0);
                    int totalSize = BitConverter.ToInt32(buffer, 4);
                    ushort fragmentNumber = BitConverter.ToUInt16(buffer, 8);
                    ushort fragmentSize = BitConverter.ToUInt16(buffer, 10);

                    bool isAck = received == 12;

                    lock (syncLock)
                    {
                        if (isAck)
                        {
                            receivedAcks.Add((remote, packetId, fragmentNumber));
                            continue;
                        }
                        if (fragmentSize > 65507 - 12)
                            continue;
                        if (received < 12 + fragmentSize)
                            continue;

                        byte[] payload = new byte[fragmentSize];
                        Array.Copy(buffer, 12, payload, 0, fragmentSize);

                        if (!receiving.TryGetValue(remote, out var buf) || buf.PacketId != packetId)
                        {
                            if (fragmentNumber != 0)
                                continue;

                            receiving[remote] = buf = new FragmentBuffer
                            {
                                PacketId = packetId,
                                TotalSize = totalSize
                            };
                        }

                        if (buf.Fragments.ContainsKey(fragmentNumber))
                            continue;

                        buf.Fragments[fragmentNumber] = payload;
                        buf.LastUpdated = DateTime.UtcNow;

                        byte[] ack = new byte[12];
                        Array.Copy(buffer, 0, ack, 0, 12);
                        socket.SendTo(ack, remote);

                        buf.LastAckHeader = ack;

                        if (buf.IsComplete)
                        {
                            byte[] fullData = buf.Assemble();
                            receiving.Remove(remote);

                            if (!packetQueues.TryGetValue(remote, out var queue))
                            {
                                queue = new Queue<byte[]>();
                                packetQueues[remote] = queue;
                            }

                            queue.Enqueue(fullData);
                        }
                    }
                }
            });
        }



        private bool IsAckReceived(EndPoint from, int packetId, ushort fragmentNumber)
        {
            lock (syncLock)
            {
                return receivedAcks.Remove((from, packetId, fragmentNumber));
            }
        }
        public bool ReceiveFrom(EndPoint remoteEP, out byte[] data)
        {
            lock (syncLock)
            {
                if (packetQueues.TryGetValue(remoteEP, out var queue) && queue.Count > 0)
                {
                    data = queue.Dequeue();
                    return true;
                }
            }
            data = null;
            return false;
        }

        public void SendTo(EndPoint remoteEP, byte[] data)
        {
            lock (sendLock)
            {
                if (!sendQueues.TryGetValue(remoteEP, out var queue))
                {
                    queue = new Queue<byte[]>();
                    sendQueues[remoteEP] = queue;
                }

                queue.Enqueue(data);

                if (!isSending.Contains(remoteEP))
                {
                    isSending.Add(remoteEP);
                    Task.Run(() => ProcessSendQueue(remoteEP));
                }
            }
        }
        private void ProcessSendQueue(EndPoint remoteEP)
        {
            while (true)
            {
                byte[] data;

                lock (sendLock)
                {
                    var queue = sendQueues[remoteEP];
                    if (queue.Count == 0)
                    {
                        isSending.Remove(remoteEP);
                        return;
                    }

                    data = queue.Dequeue();
                }

                SendReliable(remoteEP, data); // ← ✅ это твой метод с фрагментацией + ACK
            }
        }
        private void SendReliable(EndPoint remoteEP, byte[] data)
        {
            int packetId = Interlocked.Increment(ref packetCounter);
            int totalSize = data.Length;
            int totalFragments = (int)Math.Ceiling(totalSize / (double)MaxFragmentSize);

            for (ushort fragmentNumber = 0; fragmentNumber < totalFragments; fragmentNumber++)
            {
                int offset = fragmentNumber * MaxFragmentSize;
                int chunkSize = Math.Min(MaxFragmentSize, totalSize - offset);

                // Проверка на допустимый размер
                if (chunkSize > MaxFragmentSize)
                    throw new InvalidOperationException("Fragment size exceeds maximum UDP payload.");

                byte[] packet = new byte[HeaderSize + chunkSize];

                // Заголовок
                BitConverter.GetBytes(packetId).CopyTo(packet, 0);
                BitConverter.GetBytes(totalSize).CopyTo(packet, 4);
                BitConverter.GetBytes(fragmentNumber).CopyTo(packet, 8);
                BitConverter.GetBytes((ushort)chunkSize).CopyTo(packet, 10);

                // Данные
                Array.Copy(data, offset, packet, HeaderSize, chunkSize);

                int attempts = 0;
                const int maxAttempts = 5;

                while (attempts < maxAttempts)
                {
                    socket.SendTo(packet, remoteEP);
                    attempts++;

                    int waited = 0;
                    const int timeout = 1000; // 1 секунда
                    const int step = 200;

                    while (waited < timeout)
                    {
                        Thread.Sleep(step);
                        waited += step;

                        if (IsAckReceived(remoteEP, packetId, fragmentNumber))
                            goto NextFragment;
                    }
                }

                //Console.WriteLine($"❌ Не получен ACK от {remoteEP} за фрагмент {fragmentNumber} / пакет {packetId}");
                return;

            NextFragment:;
            }

            //Console.WriteLine($"✅ Отправлен полный пакет из {totalSize} байт получателю {remoteEP}");
        }
    }
}
