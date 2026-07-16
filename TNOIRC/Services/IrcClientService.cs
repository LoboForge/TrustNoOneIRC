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
    const int MaxMessages = 500;
    public List<string> ServerMessages { get; set; } = new();
    public List<IrcChannel> JoinedChannels { get; set; } = new();
    public List<IrcChannel> AvailableChannels { get; set; } = new();
    public List<IrcChannel> DirectMessages { get; set; } = new();

    public string Host;
    public int Port;
    public string Nick;
    public string User;
    public bool IsConnected { get; private set; }
    public IrcConnectionState ConnectionState { get; private set; } = IrcConnectionState.Disconnected;

    private readonly IrcConnectionOptions _options;
    private readonly bool _useTor;
    private readonly bool _useTls;
    private readonly bool _useSasl;
    private readonly string? _saslUsername;
    private readonly string? _saslPassword;
    private readonly string? _clientCertPath;
    private readonly string? _clientCertPassword;
    private readonly string? _serverPassword;

    private TcpClient? _client;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private readonly IrcCommandDispatcher _dispatcher;
    private CancellationTokenSource? _connectionCts;
    private Task? _connectionTask;
    private bool _manualDisconnect;

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

        _options = options ?? new IrcConnectionOptions();
        _useTor = _options.UseTor;
        _useTls = _options.UseTls;
        _useSasl = _options.UseSasl;
        _saslUsername = _options.SaslUsername;
        _saslPassword = _options.SaslPassword;
        _clientCertPath = _options.ClientCertPath;
        _clientCertPassword = _options.ClientCertPassword;
        _serverPassword = _options.ServerPassword;
    }

    public void StartService()
    {
        _manualDisconnect = false;
        _connectionCts = new CancellationTokenSource();
        _connectionTask = Task.Run(() => RunConnectionLoopAsync(_connectionCts.Token));
    }

    private async Task RunConnectionLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                SetConnectionState(IrcConnectionState.Connecting, $"Connecting to {Host}:{Port}...");
                await ConnectAsync(cancellationToken);
                break;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested && !_manualDisconnect && _options.AutoReconnect)
            {
                IsConnected = false;
                SetConnectionState(IrcConnectionState.Reconnecting, ex.Message);
                EventBus.Publish(new DisconnectedEvent(ex.Message, WillReconnect: true));
                EventBus.Publish(new ServerMessage($"Connection failed: {ex.Message}. Retrying in {_options.ReconnectDelaySeconds}s..."));

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_options.ReconnectDelaySeconds), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
            catch (Exception ex)
            {
                IsConnected = false;
                SetConnectionState(IrcConnectionState.Disconnected, ex.Message);
                EventBus.Publish(new DisconnectedEvent(ex.Message, WillReconnect: false));
                EventBus.Publish(new ServerMessage($"Connection failed: {ex.Message}"));
                break;
            }
        }
    }

    public async Task DisconnectAsync(string reason = "Leaving", bool reconnect = false)
    {
        _manualDisconnect = !reconnect;
        _connectionCts?.Cancel();

        try
        {
            if (_writer != null && IsConnected)
                await _writer.WriteLineAsync(IrcCommands.Quit(reason));
        }
        catch { /* connection may already be dead */ }

        CleanupConnection();
        IsConnected = false;
        SetConnectionState(IrcConnectionState.Disconnected, reason);
        EventBus.Publish(new DisconnectedEvent(reason, WillReconnect: reconnect));
    }

    private void CleanupConnection()
    {
        try { _reader?.Dispose(); } catch { }
        try { _writer?.Dispose(); } catch { }
        try { _client?.Close(); } catch { }
        _reader = null;
        _writer = null;
        _client = null;
    }

    private void SetConnectionState(IrcConnectionState state, string? message = null)
    {
        ConnectionState = state;
        EventBus.Publish(new ConnectionStateChangedEvent(state, message));
    }

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        IsConnected = false;
        CleanupConnection();

        if (_useTor)
            await TorSocks5Helper.EnsureTorReadyAsync(cancellationToken);

        var stream = await EstablishConnectionAsync(cancellationToken);
        _reader = new StreamReader(stream, Encoding.UTF8);
        _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

        if (!string.IsNullOrWhiteSpace(_serverPassword))
            await _writer.WriteLineAsync(IrcCommands.Pass(_serverPassword));

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
        var usePassword = !useExternal && !string.IsNullOrEmpty(_saslUsername);

        if (!useExternal && !usePassword)
            throw new InvalidOperationException("SASL is enabled but no client certificate or username was provided.");

        await _writer.WriteLineAsync("CAP LS 302");

        var saslAcknowledged = false;
        var authLine = string.Empty;

        while (!saslAcknowledged)
        {
            var line = await ReadServerLineAsync(cancellationToken);
            if (line == null)
                throw new InvalidOperationException("Connection closed during SASL capability negotiation.");

            if (line.Contains("AUTH=", StringComparison.OrdinalIgnoreCase))
                authLine = line;

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
            await CompleteSaslExchangeAsync(cancellationToken);
        }
        else
        {
            var username = _saslUsername!;
            var supportsScram = authLine.Contains("SCRAM-SHA-256", StringComparison.OrdinalIgnoreCase);
            if (supportsScram && !string.IsNullOrEmpty(_saslPassword))
                await AuthenticateScramAsync(username, _saslPassword!, cancellationToken);
            else
                await AuthenticatePlainAsync(username, _saslPassword ?? string.Empty, cancellationToken);
        }

        await _writer.WriteLineAsync("CAP END");
        Console.WriteLine("[SASL] Authentication successful.");
    }

    private async Task AuthenticatePlainAsync(string username, string password, CancellationToken cancellationToken)
    {
        var credentials = $"\0{username}\0{password}";
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
        await _writer!.WriteLineAsync($"AUTHENTICATE PLAIN {encoded}");
        await CompleteSaslExchangeAsync(cancellationToken);
    }

    private async Task AuthenticateScramAsync(string username, string password, CancellationToken cancellationToken)
    {
        await _writer!.WriteLineAsync("AUTHENTICATE SCRAM-SHA-256");
        var session = ScramSha256Helper.Start(username);

        while (true)
        {
            var line = await ReadServerLineAsync(cancellationToken);
            if (line == null)
                throw new InvalidOperationException("Connection closed during SCRAM authentication.");

            if (line.StartsWith("AUTHENTICATE +", StringComparison.OrdinalIgnoreCase))
            {
                var first = Convert.ToBase64String(Encoding.UTF8.GetBytes(ScramSha256Helper.ClientFirstMessage(session)));
                await _writer.WriteLineAsync($"AUTHENTICATE {first}");
                continue;
            }

            if (line.StartsWith("AUTHENTICATE ", StringComparison.OrdinalIgnoreCase))
            {
                var payload = line["AUTHENTICATE ".Length..].Trim();
                if (payload == "+")
                    continue;

                var serverFirst = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                ScramSha256Helper.ParseServerFirst(session, serverFirst);
                var clientFinal = ScramSha256Helper.BuildClientFinal(session, password);
                var encodedFinal = Convert.ToBase64String(Encoding.UTF8.GetBytes(clientFinal));
                await _writer.WriteLineAsync($"AUTHENTICATE {encodedFinal}");
                continue;
            }

            if (line.Contains(" 903 ", StringComparison.Ordinal))
                return;

            if (line.Contains(" 904 ", StringComparison.Ordinal) || line.Contains(" 905 ", StringComparison.Ordinal))
                throw new InvalidOperationException("SCRAM authentication failed.");
        }
    }

    private async Task CompleteSaslExchangeAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var line = await ReadServerLineAsync(cancellationToken);
            if (line == null)
                throw new InvalidOperationException("Connection closed during SASL authentication.");

            if (line.StartsWith("AUTHENTICATE +", StringComparison.OrdinalIgnoreCase))
            {
                await _writer!.WriteLineAsync("AUTHENTICATE +");
                continue;
            }

            if (line.Contains(" 903 ", StringComparison.Ordinal))
                return;

            if (line.Contains(" 904 ", StringComparison.Ordinal) || line.Contains(" 905 ", StringComparison.Ordinal))
                throw new InvalidOperationException("SASL authentication failed.");
        }
    }

    private async Task<string?> ReadServerLineAsync(CancellationToken cancellationToken)
    {
        var line = await _reader!.ReadLineAsync(cancellationToken);
        if (line == null)
            return null;

        Console.WriteLine($"[RAW] {line}");
        return line;
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
        await _writer!.WriteLineAsync(IrcCommands.Nick(Nick));
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
                registrationComplete = true;
                IsConnected = true;
                SetConnectionState(IrcConnectionState.Connected, message.Trailing);

                var welcomeMessage = message.Trailing ?? "Welcome";
                EventBus.Publish(new ConnectionCompletedEvent(welcomeMessage));
                EventBus.Publish(new WelcomeEvent { Message = welcomeMessage });
            }

            if (message.Command.Equals("PING", StringComparison.OrdinalIgnoreCase))
            {
                var token = message.Trailing ?? message.Parameters.FirstOrDefault();
                if (token != null)
                    await _writer!.WriteLineAsync(IrcCommands.Pong(token));
            }

            if (!registrationComplete && message.Command is "433" or "432")
            {
                Nick = $"{Nick}_";
                await SendNickUserAsync(cancellationToken);
            }
        }

        IsConnected = false;
        CleanupConnection();

        if (!_manualDisconnect && _options.AutoReconnect && !cancellationToken.IsCancellationRequested)
        {
            SetConnectionState(IrcConnectionState.Reconnecting, "Connection lost");
            EventBus.Publish(new DisconnectedEvent("Connection lost", WillReconnect: true));
            throw new IOException("Connection lost");
        }

        SetConnectionState(IrcConnectionState.Disconnected, "Disconnected");
        EventBus.Publish(new DisconnectedEvent("Disconnected", WillReconnect: false));
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
        {
            var channel = JoinedChannels.FirstOrDefault(c => c.Name.Equals(target, StringComparison.OrdinalIgnoreCase));
            channel?.Messages.Add(chatMessage);
        }
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

        MessageLogService.AppendMessage(isChannel ? target : $"pm:{target}", chatMessage);
        return SendRawAsync(IrcCommands.PrivMsg(target, message));
    }

    public async Task SyncChannelAsync(string channel)
    {
        await RequestNamesAsync(channel);
        await RequestTopicAsync(channel);
    }

    public Task JoinChannelAsync(string channel, string? key = null) =>
        SendRawAsync(IrcCommands.Join(channel, key));

    public Task PartChannelAsync(string channel, string message = "") =>
        SendRawAsync(IrcCommands.Part(channel, message));

    public Task QuitAsync(string reason = "Leaving") => SendRawAsync(IrcCommands.Quit(reason));

    public Task RequestNamesAsync(string channel) => SendRawAsync(IrcCommands.Names(channel));

    public Task RequestTopicAsync(string channel) => SendRawAsync(IrcCommands.Topic(channel));

    public Task SetTopicAsync(string channel, string topic) =>
        SendRawAsync(IrcCommands.SetTopic(channel, topic));

    public Task ChangeNickAsync(string newNick) => SendRawAsync(IrcCommands.Nick(newNick));

    public Task WhoisAsync(string nick) => SendRawAsync(IrcCommands.Whois(nick));

    public Task KickAsync(string channel, string nick, string? reason = null) =>
        SendRawAsync(IrcCommands.Kick(channel, nick, reason));

    public Task RequestChannelListAsync(string? filter = null) =>
        SendRawAsync(IrcCommands.List(filter));

    public Task LeaveChannel(string target, string reason) =>
        SendRawAsync(IrcCommands.Part(target, reason));

    public Task SetModeAsync(string target, string mode) =>
        SendRawAsync(IrcCommands.Mode(target, mode));
}
