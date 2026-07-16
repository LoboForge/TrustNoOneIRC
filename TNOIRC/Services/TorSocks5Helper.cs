namespace LoboForge.TNOIRC.Services;

using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

public static class TorSocks5Helper
{
    public static int SocksPort { get; set; } = 9050;
    public static string? TorExecutablePath { get; set; }

    private static readonly int[] DefaultSocksPorts = { 9050, 9150 };
    private static Process? _startedTorProcess;

    public static async Task EnsureTorReadyAsync(CancellationToken cancellationToken = default)
    {
        var existingPort = await DetectActiveSocksPortAsync(cancellationToken);
        if (existingPort.HasValue)
        {
            SocksPort = existingPort.Value;
            Console.WriteLine($"[TOR] Using existing SOCKS proxy on 127.0.0.1:{SocksPort}");
            return;
        }

        var torPath = ResolveTorExecutable();
        if (torPath == null)
        {
            throw new InvalidOperationException(
                "Tor is not running and no Tor binary was found. " +
                "Install Tor (e.g. `sudo apt install tor`) or start Tor Browser, then retry.");
        }

        StartTorProcess(torPath);
        await WaitForSocksPortAsync(SocksPort, cancellationToken);
        Console.WriteLine($"[TOR] Ready on 127.0.0.1:{SocksPort}");
    }

    public static Stream ConnectThroughTorPlain(string destinationHost, int destinationPort, out TcpClient tcpClient)
    {
        tcpClient = new TcpClient();
        tcpClient.Connect("127.0.0.1", SocksPort);

        var stream = tcpClient.GetStream();
        PerformSocks5Connect(stream, destinationHost, destinationPort);
        return stream;
    }

    public static SslStream ConnectThroughTorWithTls(string destinationHost, int destinationPort, out TcpClient tcpClient)
    {
        var baseStream = ConnectThroughTorPlain(destinationHost, destinationPort, out tcpClient);
        var sslStream = new SslStream(baseStream, false, ValidateServerCertificate);
        sslStream.AuthenticateAsClient(destinationHost);
        return sslStream;
    }

    public static Stream ConnectThroughTorWithOptionalTls(string destinationHost, int destinationPort, bool useTls, out TcpClient tcpClient)
    {
        if (!useTls)
            return ConnectThroughTorPlain(destinationHost, destinationPort, out tcpClient);

        return ConnectThroughTorWithTls(destinationHost, destinationPort, out tcpClient);
    }

    private static void StartTorProcess(string torPath)
    {
        if (IsTorRunning())
        {
            Console.WriteLine("[TOR] Tor process already running.");
            return;
        }

        var torDirectory = Path.Combine(AppContext.BaseDirectory, "tor");
        var torrcPath = Path.Combine(torDirectory, "torrc");
        var arguments = File.Exists(torrcPath) ? $"-f \"{torrcPath}\"" : string.Empty;

        Console.WriteLine($"[TOR] Starting {torPath} {arguments}".Trim());

        var startInfo = new ProcessStartInfo
        {
            FileName = torPath,
            Arguments = arguments,
            WorkingDirectory = torDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        _startedTorProcess = Process.Start(startInfo);
        if (_startedTorProcess == null)
            throw new InvalidOperationException("Failed to start Tor process.");
    }

    private static string? ResolveTorExecutable()
    {
        if (!string.IsNullOrWhiteSpace(TorExecutablePath) && File.Exists(TorExecutablePath))
            return TorExecutablePath;

        var bundledCandidates = OperatingSystem.IsWindows()
            ? new[] { Path.Combine(AppContext.BaseDirectory, "tor", "tor.exe") }
            : new[]
            {
                Path.Combine(AppContext.BaseDirectory, "tor", "tor"),
                Path.Combine(AppContext.BaseDirectory, "tor", "tor.exe")
            };

        foreach (var candidate in bundledCandidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        var pathTor = FindOnPath("tor");
        if (pathTor != null)
            return pathTor;

        return null;
    }

    private static string? FindOnPath(string executable)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathEnv))
            return null;

        foreach (var directory in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(directory.Trim(), executable);
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    private static bool IsTorRunning()
    {
        if (_startedTorProcess is { HasExited: false })
            return true;

        return Process.GetProcessesByName("tor").Any(p =>
        {
            try { return !p.HasExited; }
            catch { return false; }
        });
    }

    private static async Task<int?> DetectActiveSocksPortAsync(CancellationToken cancellationToken)
    {
        foreach (var port in DefaultSocksPorts)
        {
            if (await IsSocksPortOpenAsync(port, cancellationToken))
                return port;
        }

        return null;
    }

    private static async Task WaitForSocksPortAsync(int port, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 60; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await IsSocksPortOpenAsync(port, cancellationToken))
                return;

            if (_startedTorProcess is { HasExited: true })
            {
                throw new InvalidOperationException(
                    $"Tor exited unexpectedly with code {_startedTorProcess.ExitCode}. Check tor/torrc and tor/data permissions.");
            }

            await Task.Delay(500, cancellationToken);
        }

        throw new TimeoutException(
            $"Timed out waiting for Tor SOCKS proxy on 127.0.0.1:{port}. " +
            "Ensure Tor is installed and not blocked by a firewall.");
    }

    private static async Task<bool> IsSocksPortOpenAsync(int port, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(IPAddress.Loopback, port);
            var completed = await Task.WhenAny(connectTask, Task.Delay(500, cancellationToken));
            return completed == connectTask && client.Connected;
        }
        catch
        {
            return false;
        }
    }

    private static void PerformSocks5Connect(NetworkStream stream, string destinationHost, int destinationPort)
    {
        stream.Write(new byte[] { 0x05, 0x01, 0x00 }, 0, 3);

        var methodResponse = ReadExact(stream, 2);
        if (methodResponse[0] != 0x05 || methodResponse[1] != 0x00)
            throw new InvalidOperationException("SOCKS5 proxy rejected the no-auth handshake.");

        var hostBytes = Encoding.ASCII.GetBytes(destinationHost);
        if (hostBytes.Length > byte.MaxValue)
            throw new ArgumentException("Destination host name is too long for SOCKS5.", nameof(destinationHost));

        var request = new byte[7 + hostBytes.Length];
        request[0] = 0x05;
        request[1] = 0x01;
        request[2] = 0x00;
        request[3] = 0x03;
        request[4] = (byte)hostBytes.Length;
        Buffer.BlockCopy(hostBytes, 0, request, 5, hostBytes.Length);
        request[5 + hostBytes.Length] = (byte)(destinationPort >> 8);
        request[6 + hostBytes.Length] = (byte)(destinationPort & 0xFF);
        stream.Write(request, 0, request.Length);

        var replyHeader = ReadExact(stream, 4);
        if (replyHeader[1] != 0x00)
            throw new InvalidOperationException($"SOCKS5 CONNECT failed with code {replyHeader[1]} via 127.0.0.1:{SocksPort}.");

        switch (replyHeader[3])
        {
            case 0x01:
                ReadExact(stream, 4 + 2);
                break;
            case 0x03:
                var domainLength = ReadExact(stream, 1)[0];
                ReadExact(stream, domainLength + 2);
                break;
            case 0x04:
                ReadExact(stream, 16 + 2);
                break;
            default:
                throw new InvalidOperationException($"Unexpected SOCKS5 address type {replyHeader[3]}.");
        }
    }

    private static byte[] ReadExact(NetworkStream stream, int length)
    {
        var buffer = new byte[length];
        var offset = 0;

        while (offset < length)
        {
            var read = stream.Read(buffer, offset, length - offset);
            if (read == 0)
                throw new IOException("SOCKS5 proxy closed the connection unexpectedly.");
            offset += read;
        }

        return buffer;
    }

    private static bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors errors)
    {
        if (errors == SslPolicyErrors.None)
            return true;

        Console.WriteLine($"[TLS] Server certificate warning: {errors}");
        return true;
    }
}
