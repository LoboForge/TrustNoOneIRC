namespace TNO.mIRC.Services;

using System.Diagnostics;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using TNO.mIRC.Commands;
using TNO.mIRC.Models;

public static class TorSocks5Helper
{
    public static string TorExecutablePath { get; set; } = "tor/tor.exe";
    public static void StartTorIfNotRunning()
    {
        if (IsTorRunning())
        {
            Console.WriteLine("[TOR] Already running.");
            return;
        }

        if (!File.Exists(TorExecutablePath))
        {
            Console.WriteLine($"[TOR] Not found at: {TorExecutablePath}");
            return;
        }

        Console.WriteLine("[TOR] Starting tor...");
        var startInfo = new ProcessStartInfo
        {
            FileName = TorExecutablePath,
            Arguments = "",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process.Start(startInfo);
        Console.WriteLine("[TOR] Started.");
    }

    private static bool IsTorRunning()
    {
        return Process.GetProcessesByName("tor").Any();
    }
    public static SslStream ConnectThroughTorWithTls(string destinationHost, int destinationPort, out TcpClient tcpClient)
    {
        tcpClient = new TcpClient("127.0.0.1", 9150); // Tor SOCKS5
        NetworkStream stream = tcpClient.GetStream();

        // SOCKS5 handshake
        stream.Write(new byte[] { 0x05, 0x01, 0x00 });
        stream.Read(new byte[2], 0, 2);

        // SOCKS5 connect request
        byte[] hostBytes = Encoding.ASCII.GetBytes(destinationHost);
        byte[] request = new byte[7 + hostBytes.Length];
        request[0] = 0x05;
        request[1] = 0x01;
        request[2] = 0x00;
        request[3] = 0x03;
        request[4] = (byte)hostBytes.Length;
        Buffer.BlockCopy(hostBytes, 0, request, 5, hostBytes.Length);
        request[5 + hostBytes.Length] = (byte)(destinationPort >> 8);
        request[6 + hostBytes.Length] = (byte)(destinationPort & 0xFF);
        stream.Write(request, 0, request.Length);
        stream.Read(new byte[10], 0, 10);

        var sslStream = new SslStream(stream, false, (sender, cert, chain, errors) => true);
        sslStream.AuthenticateAsClient(destinationHost);
        return sslStream;
    }

    public static Stream ConnectThroughTorWithOptionalTls(string destinationHost, int destinationPort, bool useTls, out TcpClient tcpClient)
    {
        tcpClient = new TcpClient("127.0.0.1", 9150); // Tor SOCKS5
        NetworkStream stream = tcpClient.GetStream();

        // SOCKS5 handshake
        stream.Write(new byte[] { 0x05, 0x01, 0x00 });
        stream.Read(new byte[2], 0, 2);

        // SOCKS5 connect request
        byte[] hostBytes = Encoding.ASCII.GetBytes(destinationHost);
        byte[] request = new byte[7 + hostBytes.Length];
        request[0] = 0x05;
        request[1] = 0x01;
        request[2] = 0x00;
        request[3] = 0x03;
        request[4] = (byte)hostBytes.Length;
        Buffer.BlockCopy(hostBytes, 0, request, 5, hostBytes.Length);
        request[5 + hostBytes.Length] = (byte)(destinationPort >> 8);
        request[6 + hostBytes.Length] = (byte)(destinationPort & 0xFF);
        stream.Write(request, 0, request.Length);
        stream.Read(new byte[10], 0, 10);

        if (useTls)
        {
            var sslStream = new SslStream(stream, false, (sender, cert, chain, errors) => true);
            sslStream.AuthenticateAsClient(destinationHost);
            return sslStream;
        }

        return stream;
    }
    public static Stream ConnectThroughTorPlain(string destinationHost, int destinationPort, out TcpClient tcpClient)
    {
        tcpClient = new TcpClient("127.0.0.1", 9150); // Connect to Tor SOCKS5 proxy
        NetworkStream stream = tcpClient.GetStream();

        // SOCKS5 handshake: version 5, 1 auth method (no auth)
        stream.Write(new byte[] { 0x05, 0x01, 0x00 });
        stream.Read(new byte[2], 0, 2); // Version + Method

        // SOCKS5 connect request
        byte[] hostBytes = Encoding.ASCII.GetBytes(destinationHost);
        byte[] request = new byte[7 + hostBytes.Length];
        request[0] = 0x05; // SOCKS version
        request[1] = 0x01; // CONNECT
        request[2] = 0x00; // Reserved
        request[3] = 0x03; // Address type = domain name
        request[4] = (byte)hostBytes.Length;
        Buffer.BlockCopy(hostBytes, 0, request, 5, hostBytes.Length);
        request[5 + hostBytes.Length] = (byte)(destinationPort >> 8);
        request[6 + hostBytes.Length] = (byte)(destinationPort & 0xFF);
        stream.Write(request, 0, request.Length);
        stream.Read(new byte[10], 0, 10); // Reply

        // Return raw network stream — TLS can be wrapped later
        return stream;
    }

}
