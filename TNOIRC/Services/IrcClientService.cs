namespace LoboForge.TNOIRC.Services;

using System.Diagnostics;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using LoboForge.TNOIRC.Commands;
using LoboForge.TNOIRC.Models;

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
        _clientCertPath = options.ClientCertPath;
        _clientCertPassword = options.ClientCertPassword;
    }

    public void StartService()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await ConnectAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IRC] Connection failed: {ex.Message}");
                EventBus.Publish(new ServerMessage($"Connection failed: {ex.Message}"));
            }
        });
    }

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        if (_useTor)
            await TorSocks5Helper.EnsureTorReadyAsync(cancellationToken);

        var stream = await EstablishConnectionAsync(cancellationToken);
        _reader = new StreamReader(stream, Encoding.UTF8);
        _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

        if (_useSasl)
            await SaslAuthenticateAsync(cancellationToken);

        await SendNickUserAsync(cancellationToken);
        await ListenLoopAsync(cancellationToken);
    }

    private async Task<Stream> EstablishConnectionAsync(CancellationToken cancellationToken)
    {
        Stream baseStream;

        if (_useTor)
        {
            Console.WriteLine($"[TOR] Connecting to {Host}:{Port} via SOCKS5 127.0.0.1:{TorSocks5Helper.SocksPort}");
            baseStream = TorSocks5Helper.ConnectThroughTorPlain(Host, Port, out _client);
        }
        else
        {
            _client = new TcpClient();
            await _client.ConnectAsync(Host, Port, cancellationToken);
            baseStream = _client.GetStream();
        }

        if (!_useTls)
            return baseStream;

        var ssl = new SslStream(baseStream, false, ValidateServerCertificate);

        if (!string.IsNullOrEmpty(_clientCertPath))
        {
            var cert = ClientCertificateLoader.Load(_clientCertPath, _clientCertPassword);
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
                    cancellationToken);
            }
            catch (AuthenticationException ex)
            {
                Console.WriteLine($"[TLS] Client certificate authentication failed: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"[TLS] Inner: {ex.InnerException.Message}");
                throw;
            }
        }
        else
        {
            await ssl.AuthenticateAsClientAsync(
                new SslClientAuthenticationOptions
                {
                    TargetHost = Host,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck
                },
                cancellationToken);
        }

        Console.WriteLine("[TLS] Handshake complete.");
        return ssl;
    }

    private async Task SaslAuthenticateAsync(CancellationToken cancellationToken)
    {
        if (_writer == null || _reader == null)
            return;

        var useExternal = !string.IsNullOrEmpty(_clientCertPath);
        var usePlain = !useExternal && !string.IsNullOrEmpty(_saslUsername);

        if (!useExternal && !usePlain)
            throw new InvalidOperationException("SASL is enabled but no client certificate or username was provided.");

        await _writer.WriteLineAsync("CAP LS 302");

        var saslAcknowledged = false;
        while (!saslAcknowledged)
        {
            var line = await ReadServerLineAsync(cancellationToken);
            if (line == null)
                throw new InvalidOperationException("Connection closed during SASL capability negotiation.");

            if (IsCapMessage(line, "LS"))
            {
                await _writer.WriteLineAsync("CAP REQ :sasl");
                continue;
            }

            if (IsCapMessage(line, "NAK"))
                throw new InvalidOperationException("Server rejected SASL capability request.");

            if (IsCapMessage(line, "ACK") && line.Contains("sasl", StringComparison.OrdinalIgnoreCase))
                saslAcknowledged = true;
        }

        if (useExternal)
        {
            await _writer.WriteLineAsync("AUTHENTICATE EXTERNAL");
        }
        else
        {
            var credentials = $"\0{_saslUsername}\0{_saslPassword ?? string.Empty}";
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
            await _writer.WriteLineAsync($"AUTHENTICATE PLAIN {encoded}");
        }

        while (true)
        {
            var line = await ReadServerLineAsync(cancellationToken);
            if (line == null)
                throw new InvalidOperationException("Connection closed during SASL authentication.");

            if (line.StartsWith("AUTHENTICATE +", StringComparison.OrdinalIgnoreCase))
            {
                await _writer.WriteLineAsync("AUTHENTICATE +");
                continue;
            }

            if (line.Contains(" 903 ", StringComparison.Ordinal))
                break;

            if (line.Contains(" 904 ", StringComparison.Ordinal) || line.Contains(" 905 ", StringComparison.Ordinal))
                throw new InvalidOperationException("SASL authentication failed.");
        }

        await _writer.WriteLineAsync("CAP END");
        Console.WriteLine("[SASL] Authentication successful.");
    }

    private async Task<string?> ReadServerLineAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await _reader!.ReadLineAsync(cancellationToken);
            if (line == null)
                return null;

            Console.WriteLine($"[RAW] {line}");
            return line;
        }

        return null;
    }

    private static bool IsCapMessage(string line, string subcommand) =>
        line.Contains(" CAP ", StringComparison.OrdinalIgnoreCase) &&
        line.Contains($" {subcommand}", StringComparison.OrdinalIgnoreCase);

    private static bool ValidateServerCertificate(
        object sender,
        X509Certificate? certificate,
        X509Chain? chain,
        SslPolicyErrors errors)
    {
        if (errors != SslPolicyErrors.None)
            Console.WriteLine($"[TLS] Server certificate warning: {errors}");
        return true;
    }

    private async Task SendNickUserAsync(CancellationToken cancellationToken)
    {
        await _writer!.WriteLineAsync($"NICK {Nick}");
        await _writer.WriteLineAsync($"USER {User} 0 * :{User}");
    }

    private async Task ListenLoopAsync(CancellationToken cancellationToken)
    {
        var registrationComplete = false;

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await _reader!.ReadLineAsync(cancellationToken);
            if (line == null)
                break;

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

                var welcomeMessage = message.Trailing ?? "Welcome";
                EventBus.Publish(new ConnectionCompletedEvent(welcomeMessage));
                EventBus.Publish(new WelcomeEvent { Message = welcomeMessage });
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

            if (!registrationComplete && message.Command is "433" or "432")
            {
                Console.WriteLine("[IRC] Nickname rejected; retrying once with suffix.");
                Nick = $"{Nick}_";
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
