using System.Net.Sockets;
using System.Net;
using OriginalSocket = System.Net.Sockets.Socket;

namespace M9Studio.UdpLikeTcp
{
    public delegate void PacketReceivedHandler(IPEndPoint sender, byte[] data);
    public class Socket
    {
        //Константы протокола
        private const int MaxUdpSize = 65507;
        private const int HeaderSize = 12;
        private const int MaxFragmentSize = MaxUdpSize - HeaderSize;
        private const int MaxAckWaitTimeMs = 1000;
        private const int AckPollIntervalMs = 200;
        private const int MaxSendAttempts = 5;

        //Сокет и состояние
        private readonly OriginalSocket socket;
        private int packetCounter = 0;

        public int Port => ((IPEndPoint)socket.LocalEndPoint).Port;

        private readonly Dictionary<EndPoint, FragmentBuffer> receiving = new();
        private readonly Dictionary<EndPoint, Queue<byte[]>> packetQueues = new();
        private readonly List<(DateTime timestamp, EndPoint from, int packetId, ushort fragmentNumber)> receivedAcks = new();
        private readonly object syncLock = new();

        private readonly Dictionary<EndPoint, Queue<OutgoingPacket>> sendQueues = new();
        private readonly HashSet<EndPoint> isSending = new();
        private readonly object sendLock = new();

        public event PacketReceivedHandler? OnPacketReceived;


        public Socket()
        {
            socket = new OriginalSocket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Any, 0));
            Task.Run(CleanupRoutine);
            Task.Run(() =>
            {
                byte[] buffer = new byte[MaxUdpSize];
                EndPoint remote = new IPEndPoint(IPAddress.Any, 0);

                while (true)
                {
                    int received = socket.ReceiveFrom(buffer, ref remote);
                    if (received < HeaderSize)
                        continue;

                    int packetId = BitConverter.ToInt32(buffer, 0);
                    int totalSize = BitConverter.ToInt32(buffer, 4);
                    ushort fragmentNumber = BitConverter.ToUInt16(buffer, 8);
                    ushort fragmentSize = BitConverter.ToUInt16(buffer, 10);

                    bool isAck = received == HeaderSize;

                    lock (syncLock)
                    {
                        if (isAck)
                        {
                            receivedAcks.Add((DateTime.UtcNow, remote, packetId, fragmentNumber));
                            continue;
                        }

                        if (fragmentSize > MaxFragmentSize || received < HeaderSize + fragmentSize)
                            continue;

                        byte[] payload = new byte[fragmentSize];
                        Array.Copy(buffer, HeaderSize, payload, 0, fragmentSize);

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

                        byte[] ack = new byte[HeaderSize];
                        Array.Copy(buffer, 0, ack, 0, HeaderSize);
                        socket.SendTo(ack, remote);

                        buf.LastAckHeader = ack;

                        if (buf.IsComplete)
                        {
                            byte[] fullData = buf.Assemble();
                            receiving.Remove(remote);

                            if (!packetQueues.TryGetValue(remote, out var queue))
                                packetQueues[remote] = queue = new Queue<byte[]>();

                            queue.Enqueue(fullData);
                            OnPacketReceived?.Invoke(remote as IPEndPoint, fullData);
                        }
                    }
                }
            });
        }

        public bool SendTo(EndPoint remoteEP, byte[] buffer)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var packet = new OutgoingPacket { Data = buffer, Completion = tcs };

            lock (sendLock)
            {
                if (!sendQueues.TryGetValue(remoteEP, out var queue))
                    sendQueues[remoteEP] = queue = new Queue<OutgoingPacket>();

                queue.Enqueue(packet);

                if (!isSending.Contains(remoteEP))
                {
                    isSending.Add(remoteEP);
                    ProcessSendQueue(remoteEP); // теперь просто обычный вызов
                }
            }

            // Блокирует вызов, пока отправка не завершится
            return tcs.Task.GetAwaiter().GetResult();
        }

        private void ProcessSendQueue(EndPoint remoteEP)
        {
            while (true)
            {
                OutgoingPacket packet;

                lock (sendLock)
                {
                    var queue = sendQueues[remoteEP];
                    if (queue.Count == 0)
                    {
                        isSending.Remove(remoteEP);
                        return;
                    }

                    packet = queue.Dequeue();
                }

                bool success = SendReliable(remoteEP, packet.Data);
                packet.Completion.TrySetResult(success);
            }
        }

        private bool SendReliable(EndPoint remoteEP, byte[] buffer)
        {
            int packetId = Interlocked.Increment(ref packetCounter);
            int totalSize = buffer.Length;
            int totalFragments = (int)Math.Ceiling(totalSize / (double)MaxFragmentSize);

            for (ushort fragmentNumber = 0; fragmentNumber < totalFragments; fragmentNumber++)
            {
                int offset = fragmentNumber * MaxFragmentSize;
                int chunkSize = Math.Min(MaxFragmentSize, totalSize - offset);

                byte[] packet = new byte[HeaderSize + chunkSize];
                BitConverter.GetBytes(packetId).CopyTo(packet, 0);
                BitConverter.GetBytes(totalSize).CopyTo(packet, 4);
                BitConverter.GetBytes(fragmentNumber).CopyTo(packet, 8);
                BitConverter.GetBytes((ushort)chunkSize).CopyTo(packet, 10);
                Array.Copy(buffer, offset, packet, HeaderSize, chunkSize);

                int attempts = 0;
                while (attempts < MaxSendAttempts)
                {
                    socket.SendTo(packet, remoteEP);
                    attempts++;

                    int waited = 0;
                    while (waited < MaxAckWaitTimeMs)
                    {
                        Thread.Sleep(AckPollIntervalMs);
                        waited += AckPollIntervalMs;

                        if (IsAckReceived(remoteEP, packetId, fragmentNumber))
                            goto NextFragment;
                    }
                }

                return false; //Не получил ACK

            NextFragment:;
            }

            return true;
        }


        public bool ReceiveFrom(EndPoint remoteEP, out byte[] buffer)
        {
            lock (syncLock)
            {
                if (packetQueues.TryGetValue(remoteEP, out var queue) && queue.Count > 0)
                {
                    buffer = queue.Dequeue();
                    return true;
                }
            }

            buffer = null;
            return false;
        }

        public byte[] ReceiveFrom(EndPoint remoteEP)
        {
            byte[]? buffer = null;
            if(ReceiveFrom(remoteEP, out buffer))
            {
                return buffer;
            }

            using var waitHandle = new AutoResetEvent(false);

            PacketReceivedHandler handler = null!;
            handler = (sender, data) =>
            {
                if (sender.Equals(remoteEP))
                {
                    buffer = data;
                    OnPacketReceived -= handler;
                    waitHandle.Set();
                }
            };

            OnPacketReceived += handler;

            waitHandle.WaitOne(); //Блокируем, пока не получим

            return buffer!;
        }

        private bool IsAckReceived(EndPoint from, int packetId, ushort fragmentNumber)
        {
            lock (syncLock)
            {
                var index = receivedAcks.FindIndex(x =>
                    x.from.Equals(from) &&
                    x.packetId == packetId &&
                    x.fragmentNumber == fragmentNumber);

                if (index != -1)
                {
                    receivedAcks.RemoveAt(index);
                    return true;
                }

                return false;
            }
        }
        private async Task CleanupRoutine()
        {
            const int cleanupIntervalMs = 5000;
            const int ackTtlSeconds = 10;
            const int bufferTtlSeconds = 15;

            while (true)
            {
                await Task.Delay(cleanupIntervalMs);

                lock (syncLock)
                {
                    // Чистим ACK-и
                    receivedAcks.RemoveAll(entry =>
                        (DateTime.UtcNow - entry.timestamp).TotalSeconds > ackTtlSeconds);

                    // Чистим сборки пакетов, которые "зависли"
                    var toRemove = receiving
                        .Where(pair => (DateTime.UtcNow - pair.Value.LastUpdated).TotalSeconds > bufferTtlSeconds)
                        .Select(pair => pair.Key)
                        .ToList();

                    foreach (var ep in toRemove)
                    {
                        receiving.Remove(ep);
                    }
                }
            }
        }


    }


}
