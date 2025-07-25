using LoboForge.TNOIRC.Models;

namespace LoboForge.TNOIRC.Services
{
    public static class IrcMessageParser
    {
        public static IrcMessage ParseLine(string line)
        {
            var message = new IrcMessage { Raw = line };
            var working = line;

            if (working.StartsWith(":"))
            {
                var split = working.IndexOf(' ');
                message.Prefix = working[1..split];
                working = working[(split + 1)..];
            }

            var trailingSplit = working.IndexOf(" :");
            if (trailingSplit >= 0)
            {
                message.Trailing = working[(trailingSplit + 2)..];
                working = working[..trailingSplit];
            }

            var parts = working.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            message.Command = parts.FirstOrDefault() ?? "UNKNOWN";
            message.Parameters.AddRange(parts.Skip(1));

            return message;
        }
    }

}
