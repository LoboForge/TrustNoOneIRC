namespace LoboForge.TNOIRC.Models
{
    public class IrcMessage
    {
        public string Raw { get; set; }
        public string Prefix { get; set; }      // e.g. nick!ident@host
        public string Command { get; set; }      // "PRIVMSG", "JOIN", etc.
        public List<string> Parameters { get; init; } = new();
        public string? Trailing { get; set; }

    }

}
