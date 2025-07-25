namespace LoboForge.TNOIRC.Commands
{
    public static class IrcCommands
    {
        public static string Join(string channel) => $"JOIN {channel}";
        public static string List() => $"LIST";

        public static string Part(string channel, string message = "") =>
            string.IsNullOrWhiteSpace(message) ? $"PART {channel}" : $"PART {channel} :{message}";

        public static string PrivMsg(string target, string message) => $"PRIVMSG {target} :{message}";

        public static string Pong(string token) => $"PONG :{token}";

        public static string Quit(string reason = "Leaving") => $"QUIT :{reason}";

        public static string Names(string channel) => $"NAMES {channel}";

        public static string Topic(string channel) => $"TOPIC {channel}";

        public static string Raw(string command) => command;
    }
}
