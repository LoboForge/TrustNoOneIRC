using LoboForge.TNOIRC.BotCore.Models;
using LoboForge.TNOIRC.Shared.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;

namespace BotScripts
{
    public class DuckHuntConfig
    {
        public int MinDuckSeconds { get; set; } = 50;
        public int MaxDuckSeconds { get; set; } = 300;
        public int PointsPerDuck { get; set; } = 1;
        public int PointsPerMiss { get; set; } = -1;
        public int MissChancePercent { get; set; } = 20; // 0-100
        public int ReloadSeconds { get; set; } = 5;
        public int DuckEscapeSeconds { get; set; } = 120; // 1 min to escape if not shot
    }

    public class DuckHuntBot : BotBase
    {
        public override string Name { get; set; } = "DuckHunt";
        private readonly DuckHuntConfig config;
        private readonly Random rand = new();
        private readonly ConcurrentDictionary<string, ChannelState> channels = new();

        public DuckHuntBot(DuckHuntConfig? customConfig = null)
        {
            config = customConfig ?? new DuckHuntConfig();
        }

        public override void OnStart()
        {
            base.OnStart();
            EventBus.Subscribe<PrivateMessageReceivedEvent>(OnPM);
            EventBus.Subscribe<ChannelMessageReceivedEvent>(OnChannelMessage);
            EventBus.Subscribe<UserJoinedEvent>(OnJoin);
            EventBus.Subscribe<ChannelLeftEvent>(OnChannelPart);
            _timer = new Timer(_ => OnTick(), null, 1000, 1000);
        }

        private Timer? _timer;

        public override void OnStop()
        {
            base.OnStop();
            _timer?.Dispose();
        }

        // Per-channel state
        private class ChannelState
        {
            public bool DuckActive { get; set; }
            public DateTime NextDuckTime { get; set; }
            public string? LastShooter { get; set; }
            public DateTime DuckSpawnedTime { get; set; }
            public ConcurrentDictionary<string, int> Scores { get; } = new();
            public ConcurrentDictionary<string, DateTime> Reloads { get; } = new();
        }

        public override void OnSelfJoin(SelfJoinedChannelEvent evt)
        {
            var state = new ChannelState
            {
                DuckActive = false,
                NextDuckTime = DateTime.UtcNow.AddSeconds(rand.Next(config.MinDuckSeconds, config.MaxDuckSeconds + 1))
            };
            var channelKey = NormalizeChannel(evt.Channel);
            channels.TryAdd(channelKey, state);
            SendToChannel(evt.Channel, "DuckHuntBot ready! Type @bang to shoot ducks. Type @score to see the scoreboard.");
            Log($"Duckhunt started in {evt.Channel}");
            //ScheduleNextDuck(evt.Channel, state);
        }

        public override void OnChannelMessage(ChannelMessageReceivedEvent evt)
        {
            var channelKey = NormalizeChannel(evt.Target);
            if (!channels.TryGetValue(channelKey, out var state)) return;
            var msg = evt.Content.Trim();
            var nick = evt.Sender.Nick;

            // !bang: attempt to shoot duck
            if (msg.Equals("@bang", StringComparison.OrdinalIgnoreCase))
            {
                // Check for reload
                if (state.Reloads.TryGetValue(nick, out var nextFire) && DateTime.UtcNow < nextFire)
                {
                    var wait = (int)Math.Ceiling((nextFire - DateTime.UtcNow).TotalSeconds);
                    SendToChannel(channelKey, $"🔫 {nick}, you are reloading! Wait {wait}s.");
                    return;
                }

                if (state.DuckActive)
                {
                    // Roll to hit
                    bool missed = rand.Next(100) < config.MissChancePercent;
                    state.Reloads[nick] = DateTime.UtcNow.AddSeconds(config.ReloadSeconds);

                    if (missed)
                    {
                        int newScore = state.Scores.AddOrUpdate(nick, config.PointsPerMiss, (_, v) => v + config.PointsPerMiss);
                        SendToChannel(channelKey, $"💨 {nick} missed the 🦆 duck! ({config.PointsPerMiss} point) [Score: {newScore}] Reloading...");
                        return;
                    }

                    // Hit
                    state.DuckActive = false;
                    state.LastShooter = nick;
                    int newScoreHit = state.Scores.AddOrUpdate(nick, config.PointsPerDuck, (_, v) => v + config.PointsPerDuck);
                    SendToChannel(channelKey, $"🦆 {nick} shot the 🦆 duck! (+{config.PointsPerDuck} point) [Score: {newScoreHit}]");
                    ScheduleNextDuck(channelKey, state);
                }
                else
                {
                    state.Reloads[nick] = DateTime.UtcNow.AddSeconds(config.ReloadSeconds);
                    int newScore = state.Scores.AddOrUpdate(nick, config.PointsPerMiss, (_, v) => v + config.PointsPerMiss);
                    SendToChannel(channelKey, $"🔫 {nick} fired, but there was no duck! ({config.PointsPerMiss} point) [Score: {newScore}] Reloading...");
                }
                return;
            }

            // !score or !scoreboard: show scores
            if (msg.Equals("@score", StringComparison.OrdinalIgnoreCase) || msg.Equals("!scoreboard", StringComparison.OrdinalIgnoreCase))
            {
                ShowScoreboard(channelKey, state);
                return;
            }
        }
        public void OnChannelPart(ChannelLeftEvent evt) // Or whatever event your IRC lib uses
        {
            var channelKey = NormalizeChannel(evt.Channel);
            if (channels.TryRemove(channelKey, out var _))
            {
                Log($"Removed state for {channelKey}");
            }
        }
        private static string NormalizeChannel(string channel)
        {
            return channel?.Trim().ToLowerInvariant() ?? string.Empty;
        }
        // Called every second
        public override void OnTick()
        {
            foreach (var kvp in channels)
            {

                var channel = kvp.Key;
                var state = kvp.Value;

                // Duck escapes if active too long
                if (state.DuckActive)
                {
                    // Only do this if DuckSpawnedTime is actually initialized
                    var secondsAlive = (DateTime.UtcNow - state.DuckSpawnedTime).TotalSeconds;
                    if (secondsAlive > 0 && secondsAlive > config.DuckEscapeSeconds)
                    {
                        state.DuckActive = false;
                        SendToChannel(channel, "💨 The 🦆 duck escaped! Nobody was fast enough...");
                        ScheduleNextDuck(channel, state);
                        continue;
                    }
                }

                // Spawn a duck if timer elapsed
                if (!state.DuckActive && DateTime.UtcNow >= state.NextDuckTime)
                {
                    state.DuckActive = true;
                    state.LastShooter = null;
                    state.DuckSpawnedTime = DateTime.UtcNow; // <--- ONLY set here!
                    SendToChannel(channel, "🦆🦆 A wild 🦆 DUCK appears! Type @bang to shoot it! 🦆🦆");
                }
            }
        }


        private void ScheduleNextDuck(string channel, ChannelState state)
        {
            int nextIn = rand.Next(config.MinDuckSeconds, config.MaxDuckSeconds + 1);
            state.NextDuckTime = DateTime.UtcNow.AddSeconds(nextIn);
            Log($"Next 🦆 duck in {nextIn} seconds on {channel}");
        }

        private void ShowScoreboard(string channel, ChannelState state)
        {
            if (state.Scores.IsEmpty)
            {
                SendToChannel(channel, "No one has scored any points yet!");
                return;
            }

            var top = state.Scores.OrderByDescending(kvp => kvp.Value)
                .Take(10)
                .Select((kvp, i) => $"{i + 1}. {kvp.Key}: {kvp.Value} pt{(kvp.Value == 1 ? "" : "s")}")
                .ToList();

            SendToChannel(channel, "🦆 Duck Hunt Scoreboard: " + string.Join(" | ", top));
        }
    }
}
