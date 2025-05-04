# M9Studio.UdpLikeTcp

Reliable UDP communication over unreliable networks with TCP-like delivery guarantees.

[![NuGet](https://img.shields.io/nuget/v/M9Studio.UdpLikeTcp.svg)](https://www.nuget.org/packages/M9Studio.UdpLikeTcp)
[![License: Apache-2.0](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](https://www.apache.org/licenses/LICENSE-2.0)

## Features

- Reliable message delivery over UDP
- Packet fragmentation and reassembly
- ACK-based retransmission system
- Send and receive queues per endpoint
- NAT traversal ready (via STUN, optional)
- Simple API using IPEndPoint

## Installation

```bash
dotnet add package M9Studio.UdpLikeTcp
```

## Usage

### Sending data

```csharp
var socket = new M9Studio.UdpLikeTcp.Socket();
var target = new IPEndPoint(IPAddress.Loopback, 12345);
byte[] message = Encoding.UTF8.GetBytes("Hello over UDP!");
socket.SendTo(target, message);
```

### Receiving data (blocking)

```csharp
byte[] received = socket.ReceiveFrom(remoteEP);
Console.WriteLine("Received: " + Encoding.UTF8.GetString(received));
```

### Using the event system

```csharp
socket.OnPacketReceived += (sender, data) =>
{
    Console.WriteLine($"Received {data.Length} bytes from {sender}");
};
```

## Notes

- Supports .NET 8.0+
- Cross-platform: Windows, Linux, macOS
- STUN and public IP detection supported externally
