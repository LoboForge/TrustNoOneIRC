using BotCore.Interfaces;
using LoboForge.TNOIRC.BotCore.Models;
using LoboForge.TNOIRC.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BotScripts
{
    public class GhostRootBot : BotBase
    {
        public override string Name { get; set; } = "GhostRoot_MUD";

        private readonly Dictionary<string, PlayerProfile> activePlayers = new();
        private readonly PlayerService playerService = new();
        private readonly Dictionary<string, Action<PlayerProfile, Node, string, bool>> CommandHandlers;
        private readonly NetworkMap networkMap; // <-- Don't forget to provide this!

        public GhostRootBot()
        {
            // Initialize your node map (loads /Data/Nodes.json by default)
            networkMap = new NetworkMap();

            CommandHandlers = new Dictionary<string, Action<PlayerProfile, Node, string, bool>>(StringComparer.OrdinalIgnoreCase)
            {
                { "status", HandleStatus },
                { "look", HandleLook },
                { "go", HandleGo },
                // Add more commands here!
            };
        }

        public override void OnStart()
        {
            base.OnStart();
            EventBus.Subscribe<PrivateMessageReceivedEvent>(OnPM);
            EventBus.Subscribe<ChannelMessageReceivedEvent>(OnChannelMessage);
            EventBus.Subscribe<UserJoinedEvent>(OnJoin);
            Log("Startup Complete!");
        }

        public override void OnStop()
        {
            base.OnStop();
            EventBus.Unsubscribe<PrivateMessageReceivedEvent>(OnPM);
            EventBus.Unsubscribe<ChannelMessageReceivedEvent>(OnChannelMessage);
            EventBus.Unsubscribe<UserJoinedEvent>(OnJoin);
            Log("Successfully Stopped!");
        }

        public override void OnJoin(UserJoinedEvent evt)
        {
            base.OnJoin(evt);
            SendToChannel(evt.Channel, $"Welcome to GhostRoot {evt.User.Nick}! Type 'play!' to be jacked in - or send me a PM.");
            Log($"User joined: {evt.User.Nick}");
        }

        public override void OnChannelMessage(ChannelMessageReceivedEvent evt)
        {
            if (!evt.Target.Equals("#GhostRoot", StringComparison.OrdinalIgnoreCase))
                return;

            Log($"Received channel message from {evt.Sender.Nick}: {evt.Content}");
            var content = evt.Content.Trim();

            if (content.Equals("play!", StringComparison.OrdinalIgnoreCase))
            {
                Log($"{evt.Sender.Nick} triggered play!");
                HandlePlayRequest(evt.Sender);
                return;
            }

            if (content.StartsWith("!"))
            {
                var profile = EnsureProfile(evt.Sender);

                if (!profile.IsEnrolled)
                {
                    SendPM(evt.Sender.Nick, "👋 You're not enrolled yet. Type `play!` in the channel or PM me to begin.");
                    return;
                }

                var command = content.Substring(1).Trim();
                HandleCommand(evt.Sender.Nick, profile, command, isPrivate: false);
            }
        }

        public override void OnPM(PrivateMessageReceivedEvent evt)
        {
            Log($"PM received from {evt.Sender.Nick}: {evt.Message.Content}");

            var nick = evt.Sender.Nick;
            var message = evt.Message.Content.Trim();
            var profile = EnsureProfile(evt.Sender);

            if (!profile.IsEnrolled)
            {
                profile.Handle = message;
                profile.IsEnrolled = true;
                playerService.Save(profile);

                Log($"{nick} enrolled successfully as {profile.Handle}");

                SendPM(nick, $"✅ Handle set to `{profile.Handle}`.");
                SendPM(nick, $"🌐 Initializing your connection...");
                SendToChannel("#ghostroot", $"🔌 Runner `{profile.Handle}` has jacked into GhostRoot.");
                // Auto-look at their node
                var node = networkMap.GetNode(profile.CurrentNode) ?? networkMap.GetNode("root");
                HandleLook(profile, node, "", true);
                return;
            }

            HandleCommand(nick, profile, message, isPrivate: true);
        }

        private void HandlePlayRequest(IrcUser user)
        {
            var profile = EnsureProfile(user);

            if (profile.IsEnrolled && !string.IsNullOrWhiteSpace(profile.Handle))
            {
                SendPM(user.Nick, $"👋 You're already enrolled as `{profile.Handle}`. Use commands like `!status` or `!look` to continue.");
                SendToChannel("#ghostroot", $"🧠 {user.Nick}, you're already jacked in. Check your PM for details.");
                var node = networkMap.GetNode(profile.CurrentNode) ?? networkMap.GetNode("root");
                HandleLook(profile, node, "", true);
                Log($"Reminded already enrolled user {user.Nick} of their profile.");
            }
            else if (!string.IsNullOrWhiteSpace(profile.Handle))
            {
                profile.IsEnrolled = true;
                playerService.Save(profile);

                SendPM(user.Nick, $"✅ Welcome back `{profile.Handle}`. Your session has been reactivated.");
                SendToChannel("#ghostroot", $"🔌 Runner `{profile.Handle}` has reconnected to GhostRoot.");
                var node = networkMap.GetNode(profile.CurrentNode) ?? networkMap.GetNode("root");
                HandleLook(profile, node, "", true);
                Log($"Reactivated previous enrollment for {user.Nick}");
            }
            else
            {
                SendPM(user.Nick, "🧠 GhostRoot awakens...");
                SendPM(user.Nick, "Let's get you jacked in. Reply with your desired handle (nickname)");
                SendToChannel("#ghostroot", $"📩 {user.Nick}, a PM has been sent to you to start your game session.");

                Log($"Prompted new player {user.Nick} to complete enrollment.");

                if (!activePlayers.ContainsKey(user.Nick))
                    activePlayers[user.Nick] = profile;
            }
        }

        private PlayerProfile EnsureProfile(IrcUser user)
        {
            if (!activePlayers.TryGetValue(user.Nick, out var profile))
            {
                profile = playerService.Load(user.Nick) ?? new PlayerProfile { Nick = user.Nick };
                activePlayers[user.Nick] = profile;
                Log($"Loaded or created new profile for {user.Nick}");
            }
            return profile;
        }

        private void HandleCommand(string nick, PlayerProfile profile, string input, bool isPrivate)
        {
            var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var cmd = parts[0].ToLowerInvariant();
            var args = parts.Length > 1 ? parts[1] : "";

            var node = networkMap.GetNode(profile.CurrentNode) ?? networkMap.GetNode("root");
            if (CommandHandlers.TryGetValue(cmd, out var handler))
            {
                handler(profile, node, args, isPrivate);
            }
            else
            {
                // Unknown command: suggest valid actions/exits for this node
                var options = new List<string>(node.Actions.Select(a => $"!{a}"));
                options.AddRange(node.Exits.Keys.Select(e => $"!go {e}"));
                SendPM(profile.Nick, $"Unknown command. Options here: {string.Join(", ", options)}");
            }
        }

        // === HANDLER FUNCTIONS BELOW ===

        private void HandleStatus(PlayerProfile profile, Node node, string args, bool isPrivate)
        {
            var msg = $"🧠 {profile.Handle} | Level {profile.Level} | XP {profile.XP} | Node: {profile.CurrentNode}";
            if (isPrivate) SendPM(profile.Nick, msg);
            else SendToChannel("#ghostroot", msg);
        }

        private void HandleLook(PlayerProfile profile, Node node, string args, bool isPrivate)
        {
            var msg = $"{node.Name}\n{node.Description}\nExits: {string.Join(", ", node.Exits.Keys)}";
            SendPM(profile.Nick, msg);
            // Optionally, list available actions at this node
            if (node.Actions.Any())
                SendPM(profile.Nick, $"Available actions: {string.Join(", ", node.Actions.Select(a => $"!{a}"))}");
        }

        private void HandleGo(PlayerProfile profile, Node node, string args, bool isPrivate)
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                SendPM(profile.Nick, "Specify a direction or exit. Example: !go scan");
                return;
            }
            if (!node.Exits.TryGetValue(args.ToLowerInvariant(), out var nextNodeId))
            {
                SendPM(profile.Nick, $"Invalid path. Options: {string.Join(", ", node.Exits.Keys)}");
                return;
            }
            var nextNode = networkMap.GetNode(nextNodeId);
            if (nextNode == null)
            {
                SendPM(profile.Nick, "Path leads to nowhere. Contact an admin.");
                return;
            }
            profile.CurrentNode = nextNode.Id;
            playerService.Save(profile);
            HandleLook(profile, nextNode, "", true); // Auto-look after move!
        }
    }
}
