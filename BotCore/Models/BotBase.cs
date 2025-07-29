using BotCore.Interfaces;
using LoboForge.TNOIRC.Shared.Models;
using System.Security.AccessControl;

namespace LoboForge.TNOIRC.BotCore.Models
{
    public abstract class BotBase : IBot
    {
        public abstract string Name { get; set; }

        public virtual void OnStart()
        {
            EventBus.Subscribe<SelfJoinedChannelEvent>(OnSelfJoin);
            EventBus.Subscribe<UserJoinedEvent>(OnJoin);
            EventBus.Subscribe<ChannelMessageReceivedEvent>(OnChannelMessage);
            EventBus.Subscribe<PrivateMessageReceivedEvent>(OnPM);
        }

        protected void SendPM(string nick, string message)
        {
            EventBus.Publish(new BotPrivateMessageEvent
            {
                Nick = nick,
                Message = message
            });
        }

        protected void SendToChannel(string channel, string message)
        {
            EventBus.Publish(new BotSendChannelMessageEvent
            {
                Channel = channel,
                Message = message
            });
        }

        public virtual void OnPM(PrivateMessageReceivedEvent evt) { }
        public virtual void OnJoin(UserJoinedEvent evt) { }
        public virtual void OnSelfJoin(SelfJoinedChannelEvent evt) { }
        public virtual void OnChannelMessage(ChannelMessageReceivedEvent evt) { }
        public virtual void OnTick() { }

        public virtual void OnStop()
        {
            EventBus.Unsubscribe<SelfJoinedChannelEvent>(OnSelfJoin);
            EventBus.Unsubscribe<UserJoinedEvent>(OnJoin);
            EventBus.Unsubscribe<ChannelMessageReceivedEvent>(OnChannelMessage);
        }

        public virtual void Log(string message)
        {
            var formatted = $"[BOT][{Name}][{DateTime.Now:HH:mm:ss}] {message}";
            Logs.Add(formatted);
            Console.WriteLine(formatted);
        }

        // IBot interface passthroughs
        void IBot.SendPM(string nick, string message) => SendPM(nick, message);
        void IBot.SendToChannel(string channel, string message) => SendToChannel(channel, message);

        public List<string> Logs { get; } = new();
    }
}
