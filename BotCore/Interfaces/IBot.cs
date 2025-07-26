using LoboForge.TNOIRC.Shared.Models;

namespace BotCore.Interfaces
{
    public interface IBot
    {
        string Name { get; set; }

        void OnStart();
        void OnStop();

        void OnPM(PrivateMessageReceivedEvent evt);
        void OnJoin(UserJoinedEvent evt);
        void OnSelfJoin(SelfJoinedChannelEvent evt);
        void OnChannelMessage(ChannelMessageReceivedEvent evt);
        void OnTick();

        void SendPM(string nick, string message);
        void SendToChannel(string channel, string message);
    }
}
