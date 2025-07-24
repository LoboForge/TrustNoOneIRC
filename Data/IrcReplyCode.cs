namespace LoboForge.TNOIRC.Data
{
    public enum IrcReplyCode
    {
        UNKNOWN = 0,

        RPL_WELCOME = 001,
        RPL_YOURHOST = 002,
        RPL_CREATED = 003,
        RPL_MYINFO = 004,
        RPL_BOUNCE = 005,

        RPL_TOPIC = 332,
        RPL_NOTOPIC = 331,
        RPL_NAMREPLY = 353,
        RPL_ENDOFNAMES = 366,

        RPL_MOTDSTART = 375,
        RPL_MOTD = 372,
        RPL_ENDOFMOTD = 376,

        RPL_WHOISUSER = 311,
        RPL_WHOISSERVER = 312,
        RPL_WHOISOPERATOR = 313,
        RPL_ENDOFWHOIS = 318,

        ERR_NOSUCHNICK = 401,
        ERR_NOSUCHCHANNEL = 403,
        ERR_UNKNOWNCOMMAND = 421,
        ERR_CANNOTSENDTOCHAN = 404,
    }

}
