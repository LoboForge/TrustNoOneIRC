using System.Net;
using System.Net.Sockets;

namespace LoboForge.TNOIRC.Services;

public static class TorHttpClientFactory
{
    public static HttpClient Create()
    {
        var port = TorSocks5Helper.SocksPort;
        var handler = new SocketsHttpHandler
        {
            UseProxy = true,
            Proxy = new WebProxy($"socks5h://127.0.0.1:{port}"),
            AutomaticDecompression = DecompressionMethods.All
        };

        return new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMinutes(3)
        };
    }

    public static async Task<bool> IsTorProxyAvailableAsync()
    {
        try
        {
            using var client = new TcpClient();
            var connect = client.ConnectAsync(System.Net.IPAddress.Loopback, TorSocks5Helper.SocksPort);
            var completed = await Task.WhenAny(connect, Task.Delay(500));
            return completed == connect && client.Connected;
        }
        catch
        {
            return false;
        }
    }
}
