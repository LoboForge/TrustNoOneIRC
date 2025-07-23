namespace TNO.mIRC.Data
{
    public enum IrcCommandType
    {
        UNKNOWN = 0,
        PRIVMSG,
        NOTICE,
        JOIN,
        PART,
        NICK,
        QUIT,
        PING,
        PONG,
        MODE,
        TOPIC,
        WHO,
        WHOIS,
        USER,
        NAMES,
        LIST,
        MOTD,
        KICK
    }

}
