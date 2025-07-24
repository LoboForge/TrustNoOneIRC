using TNO.mIRC.Commands;
using TNO.mIRC.Services;

namespace TNO.mIRC
{
    public static class Common
    {
        public static AppConfig Config { get; set; }
        public static IrcClientService ircClient;
        public static IrcCommandDispatcher Dispatcher = new IrcCommandDispatcher(new List<IIrcCommandHandler>
        {
            new PrivMsgHandler(),           // Handles PRIVMSG
            new JoinHandler(),              // Handles JOIN
            new PartHandler(),              // Handles PART
            new NickHandler(),              // Handles NICK changes
            new QuitHandler(),              // Handles QUIT
            new TopicHandler(),             // Handles RPL_TOPIC (332)
            new NameHandler(),             // Handles RPL_NAMREPLY (353)
            new ChannelListHandler(),       // Handles RPL_LIST (322)
            new EndOfListHandler(),         // Handles RPL_LISTEND (323)
            new WelcomeHandler(),           // Handles RPL_WELCOME (001)
            new NoticeHandler(),            // Handles NOTICE
            new PingHandler(),              // Handles PING
            new KickHandler(),
            new ModeHandler(),
            new NameReplyHandler(),
            new NoTopicHandler(),
            new ErrorHandler(),
        });
    }
}
