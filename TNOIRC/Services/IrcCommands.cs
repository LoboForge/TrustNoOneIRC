namespace LoboForge.TNOIRC.Commands
{
    public static class IrcCommands
    {
        public static string Join(string channel, string? key = null) =>
            string.IsNullOrWhiteSpace(key) ? $"JOIN {channel}" : $"JOIN {channel} {key}";

        public static string List(string? filter = null) =>
            string.IsNullOrWhiteSpace(filter) ? "LIST" : $"LIST {filter}";

        public static string Part(string channel, string message = "") =>
            string.IsNullOrWhiteSpace(message) ? $"PART {channel}" : $"PART {channel} :{message}";

        public static string PrivMsg(string target, string message) => $"PRIVMSG {target} :{message}";

        public static string Notice(string target, string message) => $"NOTICE {target} :{message}";

        public static string Pong(string token) => $"PONG :{token}";

        public static string Quit(string reason = "Leaving") => $"QUIT :{reason}";

        public static string Names(string channel) => $"NAMES {channel}";

        public static string Topic(string channel) => $"TOPIC {channel}";

        public static string SetTopic(string channel, string topic) => $"TOPIC {channel} :{topic}";

        public static string Nick(string nick) => $"NICK {nick}";

        public static string Pass(string password) => $"PASS {password}";

        public static string Whois(string nick) => $"WHOIS {nick}";

        public static string Mode(string target, string modes, params string[] args) =>
            args.Length == 0 ? $"MODE {target} {modes}" : $"MODE {target} {modes} {string.Join(' ', args)}";

        public static string Kick(string channel, string nick, string? reason = null) =>
            string.IsNullOrWhiteSpace(reason) ? $"KICK {channel} {nick}" : $"KICK {channel} {nick} :{reason}";

        public static string Raw(string command) => command;
    }
}
