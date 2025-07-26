using BotCore.Interfaces;
using LoboForge.TNOIRC.BotCore.Models;
using LoboForge.TNOIRC.Models;
using System.Collections.Generic;

namespace BotScripts
{
    public class GhostRootBot : BotBase
    {
        public override string Name { get; set; } = "GhostRoot_MUD";

        private readonly Dictionary<string, PlayerProfile> activePlayers = new();
        private readonly PlayerService playerService = new();

        public override void OnStart()
        {
            base.OnStart();
            EventBus.Subscribe<PrivateMessageReceivedEvent>(OnPM);
            EventBus.Subscribe<ChannelMessageReceivedEvent>(OnChannelMessage);
        }

        public override void OnChannelMessage(ChannelMessageReceivedEvent evt)
        {
            string content = evt.Content.Trim();

            if (content.Equals("play!", StringComparison.OrdinalIgnoreCase))
            {
                SendPM(evt.Sender.Nick, "🧠 GhostRoot awakens...\nLet's get you jacked in. Reply with your desired handle (nickname).");

                if (!activePlayers.ContainsKey(evt.Sender.Nick))
                {
                    var profile = playerService.Load(evt.Sender.Nick) ?? new PlayerProfile { Nick = evt.Sender.Nick };
                    activePlayers[evt.Sender.Nick] = profile;
                }
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
            var nick = evt.Sender.Nick;
            var message = evt.Message.Content.Trim();

            var profile = EnsureProfile(evt.Sender);

            if (!profile.IsEnrolled)
            {
                profile.Handle = message;
                playerService.Save(profile);

                SendPM(nick, $"✅ Handle set to `{profile.Handle}`.");
                SendPM(nick, $"🌐 Initializing your connection...");
                SendToChannel("#ghostroot", $"🔌 Runner `{profile.Handle}` has jacked into GhostRoot.");
                return;
            }

            // Already enrolled – route to command handler
            HandleCommand(nick, profile, message, isPrivate: true);
        }

        private PlayerProfile EnsureProfile(IrcUser user)
        {
            if (!activePlayers.TryGetValue(user.Nick, out var profile))
            {
                profile = playerService.Load(user.Nick) ?? new PlayerProfile { Nick = user.Nick };
                activePlayers[user.Nick] = profile;
            }
            return profile;
        }

        private void HandleCommand(string nick, PlayerProfile profile, string input, bool isPrivate)
        {
            var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var cmd = parts[0].ToLowerInvariant();
            var args = parts.Length > 1 ? parts[1] : "";

            switch (cmd)
            {
                case "status":
                    var msg = $"🧠 {profile.Handle} | Level {profile.Level} | XP {profile.XP} | Node: {profile.CurrentNode}";
                    if (isPrivate) SendPM(nick, msg);
                    else SendToChannel("#ghostroot", msg);
                    break;

                case "look":
                    SendPM(nick, "👁️ You scan your surroundings... (room system coming soon)");
                    break;

                case "go":
                    SendPM(nick, $"🧭 You try to move: `{args}` — but the pathways aren't open yet.");
                    break;

                default:
                    SendPM(nick, $"❓ Unknown command: `{cmd}`.");
                    break;
            }
        }
    }
}
