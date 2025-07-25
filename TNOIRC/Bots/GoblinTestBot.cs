using BotCore.Interfaces;
using LoboForge.TNOIRC.BotCore.Models;
using LoboForge.TNOIRC.Models;

namespace BotScripts
{
    /// <summary>
    /// Example bot that listens for "!goblin" messages and logs responses.
    /// </summary>
    public class GoblinBot : BotBase
    {
        // Required: override the Name property
        public override string Name { get; set; } = "GoblinBot";

        /// <summary>
        /// Called when the bot starts up.
        /// </summary>
        public override void OnStart()
        {
            base.OnStart(); // Subscribes to basic events like OnJoin, OnSelfJoin, OnChannelMessage
            Log("GoblinBot is awake and listening...");
        }

        /// <summary>
        /// Responds to public channel messages.
        /// </summary>
        public override void OnChannelMessage(ChannelMessageReceivedEvent evt)
        {
            if (evt.Content.Trim() == "!goblin")
            {
                Log($"👺 {evt.Sender.Nick} disturbed a goblin!");
                // You could also respond via IRC here if needed.
            }
        }

        /// <summary>
        /// Called when the bot is stopped or disabled.
        /// </summary>
        public override void OnStop()
        {
            base.OnStop(); // Unsubscribes from events
            Log("GoblinBot has gone back to sleep.");
        }
    }
}
