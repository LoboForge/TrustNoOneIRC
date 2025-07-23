namespace TNO.mIRC.Services;

using System.Diagnostics;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using TNO.mIRC.Commands;
using TNO.mIRC.Models;

public class IrcClientService
{
    const int MaxMessages = 300;
    public List<string> ServerMessages { get; set; } = new();
    public List<IrcChannel> JoinedChannels { get; set; } = new();
    public List<IrcChannel> AvailableChannels { get; set; } = new();
    public List<IrcChannel> DirectMessages { get; set; } = new();

    public string Host;
    public int Port;
    public string Nick;
    public string User;
    private readonly bool _useTor;
    private readonly bool _useTls;
    private readonly bool _useSasl;
    private readonly string? _saslUsername;
    private readonly string? _saslPassword;
    private readonly string? _clientCertPath;
    private readonly string? _clientCertPassword;


    private TcpClient? _client;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private readonly IrcCommandDispatcher _dispatcher;

    public IrcClientService(
        string host,
        int port,
        string nick,
        string user,
        IrcCommandDispatcher dispatcher,
        IrcConnectionOptions? options = null)
    {
        Host = host;
        Port = port;
        Nick = nick;
        User = user;
        _dispatcher = dispatcher;

        options ??= new IrcConnectionOptions();
        _useTor = options.UseTor;
        _useTls = options.UseTls;
        _useSasl = options.UseSasl;
        _saslUsername = options.SaslUsername;
        _saslPassword = options.SaslPassword;
        _clientCertPath = options?.ClientCertPath;
        _clientCertPassword = options?.ClientCertPassword;
    }

    public void StartService()
    {
        _ = Task.Run(() => ConnectAsync(CancellationToken.None));
    }

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        if (_useTor)
        {
            TorSocks5Helper.StartTorIfNotRunning();
        }
        var stream = await EstablishConnectionAsync();
        _reader = new StreamReader(stream, Encoding.UTF8);
        _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

        if (_useSasl)
            await SaslAuthenticateAsync(cancellationToken);

        await SendNickUserAsync(cancellationToken);
        await ListenLoopAsync(cancellationToken);
    }

    private async Task<Stream> EstablishConnectionAsync()
    {
        Stream baseStream;

        if (_useTor)
        {
            Console.WriteLine("[TOR] Connecting via Tor SOCKS5");
            baseStream = TorSocks5Helper.ConnectThroughTorPlain(Host, Port, out _client);
        }
        else
        {
            _client = new TcpClient();
            await _client.ConnectAsync(Host, Port);
            baseStream = _client.GetStream();
        }


        if (_useTls)
        {
            var ssl = new SslStream(baseStream, false, (sender, cert, chain, err) => true);

            if (!string.IsNullOrEmpty(_clientCertPath))
            {
                if (!File.Exists(_clientCertPath))
                    throw new FileNotFoundException("Client cert not found", _clientCertPath);

                var cert = new X509Certificate2(_clientCertPath, _clientCertPassword, X509KeyStorageFlags.Exportable);
                try
                {
                    await ssl.AuthenticateAsClientAsync(
                    new SslClientAuthenticationOptions
                    {
                        TargetHost = Host,
                        ClientCertificates = new X509CertificateCollection { cert },
                        EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                        CertificateRevocationCheckMode = X509RevocationMode.NoCheck
                    },
                    CancellationToken.None
                );
                }
                catch (AuthenticationException ex)
                {
                    Console.WriteLine($"[TLS] Authentication failed: {ex.Message}");
                    if (ex.InnerException != null)
                        Console.WriteLine($"[TLS] Inner: {ex.InnerException.Message}");
                    throw;
                }
            }
            else
            {
                await ssl.AuthenticateAsClientAsync(Host);
            }

            return ssl;
        }

        return baseStream;
    }

    private async Task SaslAuthenticateAsync(CancellationToken cancellationToken)
    {
        if (_writer == null || _reader == null) return;

        await _writer.WriteLineAsync("CAP LS");
        await _writer.WriteLineAsync("CAP REQ :sasl");
        await _writer.WriteLineAsync("AUTHENTICATE EXTERNAL");

        string? line;
        while ((line = await _reader.ReadLineAsync()) != null)
        {
            Console.WriteLine($"[RAW] {line}");

            if (line.StartsWith("AUTHENTICATE +"))
            {
                // EXTERNAL doesn't require any payload, just send an empty line
                await _writer.WriteLineAsync("AUTHENTICATE +");
            }

            if (line.Contains("903")) break; // SASL success
            if (line.Contains("904") || line.Contains("905"))
                throw new Exception("SASL authentication failed.");
        }

        await _writer.WriteLineAsync("CAP END");
    }


    private async Task SendNickUserAsync(CancellationToken cancellationToken)
    {
        //await Task.Delay(5000, cancellationToken);
        await _writer!.WriteLineAsync($"NICK {Nick}");
        await _writer.WriteLineAsync($"USER {User} 0 * :{User}");
    }

    private async Task ListenLoopAsync(CancellationToken cancellationToken)
    {
        bool registrationComplete = false;

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await _reader!.ReadLineAsync();
            if (line == null) break;

            Console.WriteLine($"[RAW] {line}");
            var rawmessage = $"SERVER: {line}".Trim();
            if (ServerMessages.Count > MaxMessages)
                ServerMessages.RemoveRange(0, ServerMessages.Count - MaxMessages);

            ServerMessages.Add(rawmessage);
            EventBus.Publish(new ServerMessage(rawmessage));

            var message = IrcMessageParser.ParseLine(line);
            _dispatcher.Dispatch(message);

            if (message.Command == "001")
            {
                Console.WriteLine("[IRC] Registration complete.");
                registrationComplete = true;
            }

            if (message.Command.Equals("PING", StringComparison.OrdinalIgnoreCase))
            {
                var token = message.Trailing ?? message.Parameters.FirstOrDefault();
                if (token != null)
                {
                    Console.WriteLine($"[IRC] PONG :{token}");
                    await _writer!.WriteLineAsync(IrcCommands.Pong(token));
                }
            }

            if (!registrationComplete)
            {
                await Task.Delay(1000, cancellationToken);
                await SendNickUserAsync(cancellationToken);
            }
        }
    }

    public Task SendRawAsync(string rawLine)
    {
        Console.WriteLine($"[RAW] {rawLine}");
        var rawmessage = $"CLIENT: {rawLine}".Trim();
        ServerMessages.Add(rawmessage);
        EventBus.Publish(new ServerMessage(rawmessage));
        return _writer?.WriteLineAsync(rawLine) ?? Task.CompletedTask;
    }

    public Task SendMessageAsync(string target, string message)
    {
        var isChannel = target.StartsWith("#") || target.StartsWith("&");
        var myUser = new IrcUser(Nick, User, Host);

        var isAction = message.StartsWith("\x01ACTION") && message.EndsWith("\x01");
        var content = isAction ? message[8..^1].Trim() : message;

        var chatMessage = new ChatMessage(DateTime.UtcNow, myUser, target, content, isAction);

        if (isChannel)
            JoinedChannels.FirstOrDefault(c => c.Name.Equals(target, StringComparison.OrdinalIgnoreCase))?.Messages.Add(chatMessage);
        else
        {
            var direct = DirectMessages.FirstOrDefault(c => c.Name.Equals(target, StringComparison.OrdinalIgnoreCase));
            if (direct == null)
            {
                direct = new IrcChannel { Name = target, IsJoined = true };
                DirectMessages.Add(direct);
            }
            direct.Messages.Add(chatMessage);
        }

        return SendRawAsync(IrcCommands.PrivMsg(target, message));
    }

    public Task JoinChannelAsync(string channel) => SendRawAsync(IrcCommands.Join(channel));
    public Task PartChannelAsync(string channel, string message = "") => SendRawAsync(IrcCommands.Part(channel, message));
    public Task QuitAsync(string reason = "Leaving") => SendRawAsync(IrcCommands.Quit(reason));
    public Task RequestNamesAsync(string channel) => SendRawAsync(IrcCommands.Names(channel));
    public Task RequestTopicAsync(string channel) => SendRawAsync(IrcCommands.Topic(channel));
    public Task RequestChannelListAsync(string? filter = null) => SendRawAsync(IrcCommands.List());
    public Task LeaveChannel(string target, string reason) => SendRawAsync($"PART {target} {reason}");
    public Task SetModeAsync(string target, string mode) => SendRawAsync($"MODE {target} {mode}");
}
