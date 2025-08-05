using SharpTox.Core;
using SharpTox.Av;
using LoboForge.TNOIRC.Models;
using System.Text;

namespace LoboForge.TNOIRC.Services
{
    public class ToxClientService
    {
        const int MaxMessages = 300;
        public List<string> SystemMessages { get; set; } = new();
        public List<IrcChannel> GroupChats { get; set; } = new();
        public List<IrcChannel> DirectMessages { get; set; } = new();

        private readonly Tox _tox;
        private readonly ToxOptions _options;
        private readonly IrcCommandDispatcher _dispatcher;

        private CancellationTokenSource _cts = new();

        public string Nick { get; private set; }
        public string ToxId => _tox.Id.ToString();

        public ToxClientService(
            string nickname,
            IrcCommandDispatcher dispatcher,
            bool useTor = true)
        {
            _dispatcher = dispatcher;
            Nick = nickname;

            _options = new ToxOptions(true, true);
            if (useTor)
            {
                _options.UdpEnabled = false; // Prevent IP leaks
                _options.Ipv6Enabled = false;
                _options.ProxyType = ToxProxyType.Socks5;
                _options.ProxyHost = "127.0.0.1";
                _options.ProxyPort = 9050;
            }

            _tox = new Tox(_options);
            _tox.Name = nickname;
        }

        public void StartService()
        {
            _ = Task.Run(() => ConnectAsync(_cts.Token));
        }

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            // Bootstrap (you’ll want onion nodes here)
            foreach (var node in ToxBootstrapNodes.GetDefaultNodes())
            {
                _tox.Bootstrap(node);
            }

            HookEvents();

            // Main loop
            while (!cancellationToken.IsCancellationRequested)
            {
                _tox.Do();
                await Task.Delay(50, cancellationToken);
            }
        }

        private void HookEvents()
        {
            _tox.OnFriendRequestReceived += (sender, e) =>
            {
                var msg = $"Friend request from {e.PublicKey}";
                EventBus.Publish(new ServerMessage(msg));
            };

            _tox.OnFriendMessageReceived += (sender, e) =>
            {
                var user = new IrcUser(e.FriendNumber.ToString(), "", "tox");
                var chatMsg = new ChatMessage(DateTime.UtcNow, user, e.FriendNumber.ToString(), e.Message, false);

                var direct = DirectMessages.FirstOrDefault(c => c.Name == user.Nick)
                          ?? new IrcChannel { Name = user.Nick, IsJoined = true };
                direct.Messages.Add(chatMsg);
                if (!DirectMessages.Contains(direct))
                    DirectMessages.Add(direct);

                EventBus.Publish(chatMsg);
            };

            _tox.OnGroupMessageReceived += (sender, e) =>
            {
                var group = GroupChats.FirstOrDefault(c => c.Name == e.GroupNumber.ToString())
                         ?? new IrcChannel { Name = $"Group-{e.GroupNumber}", IsJoined = true };
                
                var user = new IrcUser(e.PeerNumber.ToString(), "", "tox");
                var chatMsg = new ChatMessage(DateTime.UtcNow, user, group.Name, e.Message, false);
                
                group.Messages.Add(chatMsg);
                if (!GroupChats.Contains(group))
                    GroupChats.Add(group);

                EventBus.Publish(chatMsg);
            };
        }

        public void SendMessageToFriend(int friendNumber, string message)
        {
            _tox.Friends[friendNumber].SendMessage(message);
        }

        public void SendMessageToGroup(int groupNumber, string message)
        {
            _tox.Groups[groupNumber].SendMessage(message);
        }

        public void Stop()
        {
            _cts.Cancel();
            _tox.Dispose();
        }
    }
}
