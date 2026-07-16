using BotCore.Interfaces;
using BotScripts;
using LoboForge.TNOIRC.BotEngine;
using LoboForge.TNOIRC.Shared.Models;
using System.Threading;

namespace LoboForge.TNOIRC.Services
{
    public static class BotService
    {
        public static List<IBotMetadata> BotsMeta { get; set; } = new List<IBotMetadata>();
        public static List<IBot> Bots { get; set; } = new List<IBot>();
        private static Timer? _tickTimer;
        private static bool _eventsRegistered;

        public static void LoadBots()
        {
            if (Bots.Count == 0)
            {
                GhostRootBot ghostRootBot = new GhostRootBot();
                Bots.Add(ghostRootBot);
                ghostRootBot.OnStart();

                BotMetadata botMetadata = new BotMetadata() { Enabled = true, Instance = ghostRootBot, Name = ghostRootBot.Name };
                BotsMeta.Add(botMetadata);

                DuckHuntBot dhb = new DuckHuntBot();
                Bots.Add(dhb);
                dhb.OnStart();

                BotMetadata dhbmeta = new BotMetadata() { Enabled = true, Instance = dhb, Name = dhb.Name };
                BotsMeta.Add(dhbmeta);
            }

            RegisterBotEvents();

            _tickTimer ??= new Timer(_ => TickAllBots(), null, 1000, 1000);
        }

        private static void RegisterBotEvents()
        {
            if (_eventsRegistered)
                return;

            _eventsRegistered = true;
            EventBus.Subscribe<BotSendChannelMessageEvent>(BotSendChannelMessage);
            EventBus.Subscribe<BotPrivateMessageEvent>(BotSendPrivateMessage);
        }

        private static void TickAllBots()
        {
            foreach (var bot in Bots)
            {
                try
                {
                    bot.OnTick();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[BotService] Exception in OnTick for bot {bot.Name}: {ex}");
                }
            }
        }

        private static void BotSendChannelMessage(BotSendChannelMessageEvent @event)
        {
            if (!Common.IsConnected)
                return;

            _ = Common.ircClient.SendMessageAsync(@event.Channel, @event.Message);
        }

        private static void BotSendPrivateMessage(BotPrivateMessageEvent @event)
        {
            if (!Common.IsConnected)
                return;

            _ = Common.ircClient.SendMessageAsync(@event.Nick, @event.Message);
        }

        public static void UnloadBots()
        {
            _tickTimer?.Dispose();
            foreach (var bot in Bots)
                bot.OnStop();

            Bots.Clear();
            BotsMeta.Clear();
        }
    }
}
