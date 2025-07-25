using BotCore.Interfaces;

namespace LoboForge.TNOIRC.BotCore.Models
{
    public abstract class BotBase: IBot
    {
        public abstract string Name { get; set; }

        public virtual void OnStart()
        {
            EventBus.Subscribe<SelfJoinedChannelEvent>(OnSelfJoin);
            EventBus.Subscribe<UserJoinedEvent>(OnJoin);
            EventBus.Subscribe<ChannelMessageReceivedEvent>(OnChannelMessage);
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

            // Optional: push to shared log service or console
            Console.WriteLine(formatted);
        }
        public List<string> Logs = new();
    }
}
